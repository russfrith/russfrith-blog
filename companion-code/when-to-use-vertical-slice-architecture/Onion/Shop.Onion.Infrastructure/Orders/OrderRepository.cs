using Microsoft.EntityFrameworkCore;
using Shop.Onion.Application.Orders;
using Shop.Onion.Domain;

namespace Shop.Onion.Infrastructure.Orders;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _context.Orders
            .Include(o => o.Lines)
            .Include(o => o.AppliedDiscounts)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public void Add(Order order) => _context.Orders.Add(order);
}
