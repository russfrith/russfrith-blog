namespace OrderLightCqrs.Application.Commands;

public interface ISubmitOrderHandler
{
    Task HandleAsync(SubmitOrderCommand command, CancellationToken cancellationToken = default);
}
