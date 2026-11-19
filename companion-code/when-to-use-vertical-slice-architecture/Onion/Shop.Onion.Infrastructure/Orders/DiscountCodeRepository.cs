using Microsoft.EntityFrameworkCore;
using Shop.Onion.Application.Orders;
using Shop.Onion.Domain;

namespace Shop.Onion.Infrastructure.Orders;

public class DiscountCodeRepository : IDiscountCodeRepository
{
    private readonly AppDbContext _context;

    public DiscountCodeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<DiscountCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _context.DiscountCodes.FirstOrDefaultAsync(d => d.Code == normalized, cancellationToken);
    }
}
