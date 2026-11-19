using Microsoft.EntityFrameworkCore;
using Shop.VerticalSlice.Common;
using Shop.VerticalSlice.Data;

namespace Shop.VerticalSlice.Features.Products.CreateProduct;

public class Handler
{
    private readonly AppDbContext _context;

    public Handler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> HandleAsync(Request request, CancellationToken cancellationToken = default)
    {
        if (await _context.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken))
        {
            throw new DomainException($"A product with SKU {request.Sku} already exists.");
        }

        var product = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Sku = request.Sku,
            Name = request.Name,
            Price = request.Price
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
