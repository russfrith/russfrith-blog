using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Comparisons.Shared;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option1_RawDbContext;

// Option 1: DbContext used directly in the handler, written the way its
// proponents recommend it - no repository, no query object, EF Core's LINQ
// provider and change tracking used as-is. This is the simplest of the five.
public class OpenOrdersScenario(ComparisonDbContext dbContext)
{
    public async Task<Order?> GetOpenOrdersAndSubmitOneAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var openOrders = await dbContext.Orders
            .Include(order => order.Lines)
            .Where(order => order.CustomerId == customerId && Order.OpenStatuses.Contains(order.Status))
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderToSubmit = openOrders.FirstOrDefault(order => order.Status == OrderStatus.Draft);
        if (orderToSubmit is null)
            return null;

        orderToSubmit.Submit();
        await dbContext.SaveChangesAsync(cancellationToken);

        return orderToSubmit;
    }
}
