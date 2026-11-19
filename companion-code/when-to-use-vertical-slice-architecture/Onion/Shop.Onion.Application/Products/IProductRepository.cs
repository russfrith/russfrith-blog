using Shop.Onion.Domain;

namespace Shop.Onion.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default);
    void Add(Product product);
    void Remove(Product product);
}
