using Dapper;
using OrderLightCqrs.Application.Dtos;
using OrderLightCqrs.Application.Queries;

namespace OrderLightCqrs.Infrastructure.Reads;

/// <summary>
/// The entire read side: SQL in, DTO out. No repositories, no aggregates, no Order or
/// OrderLine in sight. This is the only place in Infrastructure allowed to reference
/// Dapper for reads (see ArchitectureTests).
/// </summary>
public class OrderQueries : IOrderQueries
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderQueries(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<OrderListItemDto>> GetOrderListAsync(OrderListFilter filter, CancellationToken cancellationToken = default)
    {
        const string countSql = """
            select count(*)
            from orders o
            where (@Status is null or o.status = @Status)
              and (@FromUtc is null or o.created_at_utc >= @FromUtc)
              and (@ToUtc is null or o.created_at_utc < @ToUtc)
            """;

        // Paginate first, then join: picking the 50 order ids for this page before joining
        // to order_lines keeps the GROUP BY small. The first version of this query grouped
        // every matching order (potentially thousands) before applying OFFSET/LIMIT, which
        // measurably lost to EF's own query shaping - EF defers the Lines join the same way
        // when it paginates a collection Include. See the post for the actual numbers.
        const string pageSql = """
            with paged_orders as (
                select o.id, o.customer_id, o.created_at_utc, o.status
                from orders o
                where (@Status is null or o.status = @Status)
                  and (@FromUtc is null or o.created_at_utc >= @FromUtc)
                  and (@ToUtc is null or o.created_at_utc < @ToUtc)
                order by o.created_at_utc desc
                offset @Offset limit @PageSize
            )
            select
                po.id as OrderId,
                c.name as CustomerName,
                po.created_at_utc as CreatedAtUtc,
                po.status as Status,
                count(ol.id)::int as LineCount,
                coalesce(sum(ol.quantity * ol.unit_price), 0) as Total
            from paged_orders po
            join customers c on c.id = po.customer_id
            left join order_lines ol on ol.order_id = po.id
            group by po.id, c.name, po.created_at_utc, po.status
            order by po.created_at_utc desc
            """;

        var parameters = new
        {
            filter.Status,
            filter.FromUtc,
            filter.ToUtc,
            Offset = (filter.Page - 1) * filter.PageSize,
            filter.PageSize
        };

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        var items = await connection.QueryAsync<OrderListItemDto>(new CommandDefinition(pageSql, parameters, cancellationToken: cancellationToken));

        return new PagedResult<OrderListItemDto>(items.ToList(), totalCount, filter.Page, filter.PageSize);
    }

    public async Task<OrderDetailDto?> GetOrderDetailAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        const string headerSql = """
            select o.id as OrderId, o.customer_id as CustomerId, c.name as CustomerName,
                   o.status as Status, o.created_at_utc as CreatedAtUtc, o.submitted_at_utc as SubmittedAtUtc
            from orders o
            join customers c on c.id = o.customer_id
            where o.id = @OrderId
            """;

        const string linesSql = """
            select ol.product_id as ProductId, p.name as ProductName, ol.quantity as Quantity,
                   ol.unit_price as UnitPrice, (ol.quantity * ol.unit_price) as LineTotal
            from order_lines ol
            join products p on p.id = ol.product_id
            where ol.order_id = @OrderId
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            $"{headerSql};{linesSql}", new { OrderId = orderId }, cancellationToken: cancellationToken));

        var header = await multi.ReadSingleOrDefaultAsync<OrderHeaderRow>();
        if (header is null)
        {
            return null;
        }

        var lines = (await multi.ReadAsync<OrderDetailLineDto>()).ToList();

        return new OrderDetailDto(
            header.OrderId,
            header.CustomerId,
            header.CustomerName,
            header.Status,
            header.CreatedAtUtc,
            header.SubmittedAtUtc,
            lines.Sum(l => l.LineTotal),
            lines);
    }

    public async Task<IReadOnlyList<SalesReportRowDto>> GetSalesReportAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                p.category as Category,
                extract(year from o.created_at_utc)::int as Year,
                extract(month from o.created_at_utc)::int as Month,
                sum(ol.quantity * ol.unit_price) as Revenue,
                count(distinct o.id)::int as OrderCount
            from order_lines ol
            join orders o on o.id = ol.order_id
            join products p on p.id = ol.product_id
            where o.created_at_utc >= @FromUtc and o.created_at_utc < @ToUtc
              and o.status <> 'Cancelled'
            group by p.category, extract(year from o.created_at_utc), extract(month from o.created_at_utc)
            order by Year, Month, Category
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<SalesReportRowDto>(new CommandDefinition(
            sql, new { FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<CustomerOrderHistoryRowDto>> GetCustomerOrderHistoryAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select
                o.id as OrderId,
                o.created_at_utc as CreatedAtUtc,
                o.status as Status,
                ol.product_id as ProductId,
                p.name as ProductName,
                ol.quantity as Quantity,
                ol.unit_price as UnitPrice,
                (ol.quantity * ol.unit_price) as LineTotal
            from orders o
            join order_lines ol on ol.order_id = o.id
            join products p on p.id = ol.product_id
            where o.customer_id = @CustomerId
            order by o.created_at_utc desc, ol.id
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = await connection.QueryAsync<CustomerOrderHistoryRowDto>(new CommandDefinition(
            sql, new { CustomerId = customerId }, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    private record OrderHeaderRow(
        Guid OrderId,
        Guid CustomerId,
        string CustomerName,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? SubmittedAtUtc);
}
