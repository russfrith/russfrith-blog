using Microsoft.EntityFrameworkCore;
using OrderLightCqrs.Application;
using OrderLightCqrs.Domain;

namespace OrderLightCqrs.Infrastructure.Write;

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
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public void Add(Order order)
    {
        _context.Orders.Add(order);
    }
}
