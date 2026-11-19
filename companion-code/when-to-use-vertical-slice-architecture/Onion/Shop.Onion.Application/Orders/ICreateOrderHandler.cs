namespace Shop.Onion.Application.Orders;

public interface ICreateOrderHandler
{
    Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default);
}
