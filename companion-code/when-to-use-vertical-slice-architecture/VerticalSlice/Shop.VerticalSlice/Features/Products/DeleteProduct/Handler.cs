using Shop.VerticalSlice.Common;
using Shop.VerticalSlice.Data;

namespace Shop.VerticalSlice.Features.Products.DeleteProduct;

public class Handler
{
    private readonly AppDbContext _context;

    public Handler(AppDbContext context)
    {
        _context = context;
    }

    public async Task HandleAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync([productId], cancellationToken)
            ?? throw new DomainException($"Product {productId} was not found.");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
