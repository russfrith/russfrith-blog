namespace Shop.Onion.Application.Products;

public interface IDeleteProductHandler
{
    Task HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken = default);
}
