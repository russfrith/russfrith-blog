using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using OrderLightCqrs.Application.Dtos;
using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure.Reads;
using OrderLightCqrs.Infrastructure.Write;

namespace OrderLightCqrs.Benchmarks;

/// <summary>
/// Scenario 1 - order list page: paged, filtered by status and date range, sorted by date,
/// showing customer name, line count, and total. The bread-and-butter list screen - a real
/// order-management app runs this query on every page load of the orders list.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class OrderListBenchmarks
{
    private const string Status = "Submitted";
    private DateTimeOffset _fromUtc;
    private DateTimeOffset _toUtc;
    private const int Page = 2;
    private const int PageSize = 50;

    private static readonly Func<AppDbContext, OrderStatus, DateTimeOffset, DateTimeOffset, int, int, Task<List<OrderListRow>>> CompiledPageQuery =
        EF.CompileAsyncQuery((AppDbContext ctx, OrderStatus status, DateTimeOffset fromUtc, DateTimeOffset toUtc, int skip, int take) =>
            ctx.Orders
                .Where(o => o.Status == status && o.CreatedAtUtc >= fromUtc && o.CreatedAtUtc < toUtc)
                .OrderByDescending(o => o.CreatedAtUtc)
                .Skip(skip)
                .Take(take)
                .Select(o => new OrderListRow(
                    o.Id,
                    ctx.Customers.Where(c => c.Id == o.CustomerId).Select(c => c.Name).First(),
                    o.CreatedAtUtc,
                    o.Status.ToString(),
                    o.Lines.Count(),
                    o.Lines.Sum(l => l.Quantity * l.UnitPrice)))
                .ToList());

    private static readonly Func<AppDbContext, OrderStatus, DateTimeOffset, DateTimeOffset, Task<int>> CompiledCountQuery =
        EF.CompileAsyncQuery((AppDbContext ctx, OrderStatus status, DateTimeOffset fromUtc, DateTimeOffset toUtc) =>
            ctx.Orders.Count(o => o.Status == status && o.CreatedAtUtc >= fromUtc && o.CreatedAtUtc < toUtc));

    [GlobalSetup]
    public void Setup()
    {
        _toUtc = DateTimeOffset.UtcNow;
        _fromUtc = _toUtc.AddDays(-90);
    }

    [Benchmark(Baseline = true, Description = "EF tracked entities (Include + second query)")]
    public async Task<PagedResult<OrderListItemDto>> EfTracked()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var baseQuery = context.Orders
            .Where(o => o.Status == OrderStatus.Submitted && o.CreatedAtUtc >= _fromUtc && o.CreatedAtUtc < _toUtc);

        var total = await baseQuery.CountAsync();

        var orders = await baseQuery
            .Include(o => o.Lines)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
        var customerNames = await context.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var items = orders
            .Select(o => new OrderListItemDto(o.Id, customerNames[o.CustomerId], o.CreatedAtUtc.UtcDateTime, o.Status.ToString(), o.Lines.Count, o.Lines.Sum(l => l.LineTotal)))
            .ToList();

        return new PagedResult<OrderListItemDto>(items, total, Page, PageSize);
    }

    [Benchmark(Description = "EF AsNoTracking entities (Include + second query)")]
    public async Task<PagedResult<OrderListItemDto>> EfAsNoTracking()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var baseQuery = context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Submitted && o.CreatedAtUtc >= _fromUtc && o.CreatedAtUtc < _toUtc);

        var total = await baseQuery.CountAsync();

        var orders = await baseQuery
            .Include(o => o.Lines)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
        var customerNames = await context.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var items = orders
            .Select(o => new OrderListItemDto(o.Id, customerNames[o.CustomerId], o.CreatedAtUtc.UtcDateTime, o.Status.ToString(), o.Lines.Count, o.Lines.Sum(l => l.LineTotal)))
            .ToList();

        return new PagedResult<OrderListItemDto>(items, total, Page, PageSize);
    }

    [Benchmark(Description = "EF AsNoTracking + Select projection (single query)")]
    public async Task<PagedResult<OrderListItemDto>> EfProjection()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var baseQuery = context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Submitted && o.CreatedAtUtc >= _fromUtc && o.CreatedAtUtc < _toUtc);

        var total = await baseQuery.CountAsync();

        var rows = await baseQuery
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(o => new OrderListRow(
                o.Id,
                context.Customers.Where(c => c.Id == o.CustomerId).Select(c => c.Name).First(),
                o.CreatedAtUtc,
                o.Status.ToString(),
                o.Lines.Count(),
                o.Lines.Sum(l => l.Quantity * l.UnitPrice)))
            .ToListAsync();

        return new PagedResult<OrderListItemDto>(rows.Select(r => r.ToDto()).ToList(), total, Page, PageSize);
    }

    [Benchmark(Description = "EF compiled query + projection (single query)")]
    public async Task<PagedResult<OrderListItemDto>> EfCompiledProjection()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var status = Enum.Parse<OrderStatus>(Status);
        var total = await CompiledCountQuery(context, status, _fromUtc, _toUtc);
        var rows = await CompiledPageQuery(context, status, _fromUtc, _toUtc, (Page - 1) * PageSize, PageSize);

        return new PagedResult<OrderListItemDto>(rows.Select(r => r.ToDto()).ToList(), total, Page, PageSize);
    }

    [Benchmark(Description = "Dapper (single query + count query)")]
    public async Task<PagedResult<OrderListItemDto>> Dapper()
    {
        var queries = new OrderQueries(BenchmarkEnvironment.CreateConnectionFactory());
        return await queries.GetOrderListAsync(new OrderListFilter(Status, _fromUtc, _toUtc, Page, PageSize));
    }

    // EF Core's Npgsql provider can't translate DateTimeOffset.UtcDateTime inside a
    // projection that also contains a correlated scalar subquery (the customer-name
    // lookup) - it throws a coercion error in the shaper. Projecting DateTimeOffset as-is
    // and converting to DateTime after materialization sidesteps that translation gap.
    private record OrderListRow(Guid OrderId, string CustomerName, DateTimeOffset CreatedAtUtc, string Status, int LineCount, decimal Total)
    {
        public OrderListItemDto ToDto() => new(OrderId, CustomerName, CreatedAtUtc.UtcDateTime, Status, LineCount, Total);
    }
}
