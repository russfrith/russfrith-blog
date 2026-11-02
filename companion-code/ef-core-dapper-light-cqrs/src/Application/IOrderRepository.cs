using OrderLightCqrs.Domain;

namespace OrderLightCqrs.Application;

/// <summary>
/// Write-side repository. Aggregate-shaped on purpose: it never returns a DTO
/// or an IQueryable, and the read side never calls it.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    void Add(Order order);
}
