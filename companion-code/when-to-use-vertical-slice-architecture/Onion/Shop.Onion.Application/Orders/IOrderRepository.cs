using Shop.Onion.Domain;

namespace Shop.Onion.Application.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    void Add(Order order);
}
