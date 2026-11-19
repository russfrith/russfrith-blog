namespace Shop.Onion.Application.Products;

public interface ICreateProductHandler
{
    Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default);
}
