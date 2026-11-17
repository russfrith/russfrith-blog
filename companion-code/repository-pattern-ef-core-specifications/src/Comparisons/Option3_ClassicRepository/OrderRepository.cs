using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Comparisons.Shared;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option3_ClassicRepository;

public class OrderRepository(ComparisonDbContext dbContext) : IOrderRepository
{
    public async Task<List<Order>> GetOpenOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken)
        => await dbContext.Orders
            .Include(order => order.Lines)
            .Where(order => order.CustomerId == customerId && Order.OpenStatuses.Contains(order.Status))
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
