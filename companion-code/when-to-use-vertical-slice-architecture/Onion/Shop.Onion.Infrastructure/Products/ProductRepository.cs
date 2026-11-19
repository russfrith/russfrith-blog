using Microsoft.EntityFrameworkCore;
using Shop.Onion.Application.Products;
using Shop.Onion.Domain;

namespace Shop.Onion.Infrastructure.Products;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _context.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        return _context.Products.AnyAsync(p => p.Sku == sku, cancellationToken);
    }

    public void Add(Product product) => _context.Products.Add(product);

    public void Remove(Product product) => _context.Products.Remove(product);
}
