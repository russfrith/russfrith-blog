namespace OrderLightCqrs.Application.Commands;

public interface ICreateOrderHandler
{
    Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default);
}
