using Microsoft.EntityFrameworkCore;
using Shop.VerticalSlice.Data;

namespace Shop.VerticalSlice.Features.Products.ListProducts;

public class Handler
{
    private readonly AppDbContext _context;

    public Handler(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<ProductResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponse(p.Id, p.Sku, p.Name, p.Price))
            .ToListAsync(cancellationToken);
    }
}
