using OrderLightCqrs.Application.Dtos;

namespace OrderLightCqrs.Application.Queries;

/// <summary>
/// Read-side query service. No repositories, no aggregates, no domain types: everything
/// here goes straight from SQL to a DTO. Implemented with Dapper in Infrastructure/Reads.
/// </summary>
public interface IOrderQueries
{
    Task<PagedResult<OrderListItemDto>> GetOrderListAsync(OrderListFilter filter, CancellationToken cancellationToken = default);

    Task<OrderDetailDto?> GetOrderDetailAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesReportRowDto>> GetSalesReportAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerOrderHistoryRowDto>> GetCustomerOrderHistoryAsync(Guid customerId, CancellationToken cancellationToken = default);
}
