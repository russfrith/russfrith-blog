namespace Shop.Onion.Application.Products;

public interface IUpdateProductHandler
{
    Task HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default);
}
