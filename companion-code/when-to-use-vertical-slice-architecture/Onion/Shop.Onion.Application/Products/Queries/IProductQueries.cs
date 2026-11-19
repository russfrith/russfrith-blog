using Shop.Onion.Application.Products.Dtos;

namespace Shop.Onion.Application.Products.Queries;

public interface IProductQueries
{
    Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken = default);
}
