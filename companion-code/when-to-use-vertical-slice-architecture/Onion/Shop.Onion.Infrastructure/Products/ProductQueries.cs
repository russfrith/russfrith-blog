using Microsoft.EntityFrameworkCore;
using Shop.Onion.Application.Products.Dtos;
using Shop.Onion.Application.Products.Queries;

namespace Shop.Onion.Infrastructure.Products;

public class ProductQueries : IProductQueries
{
    private readonly AppDbContext _context;

    public ProductQueries(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Price))
            .ToListAsync(cancellationToken);
    }
}
