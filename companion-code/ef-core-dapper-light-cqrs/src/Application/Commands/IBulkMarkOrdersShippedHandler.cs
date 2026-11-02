namespace OrderLightCqrs.Application.Commands;

public interface IBulkMarkOrdersShippedHandler
{
    Task<int> HandleAsync(BulkMarkOrdersShippedCommand command, CancellationToken cancellationToken = default);
}
