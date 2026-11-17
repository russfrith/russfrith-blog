using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Comparisons.Shared;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option2_QueryObjects;

// Option 2: a named, per-use-case query object replaces the ad hoc LINQ from
// option 1, but it still depends on DbContext directly - there's no
// repository boundary between it and EF Core. Deriving from
// AggregateQueryObject<Order> costs nothing and still gets the compiler to
// refuse this same class written against OrderLine instead.
public class GetOpenOrdersForCustomer(ComparisonDbContext dbContext) : AggregateQueryObject<Order>
{
    public async Task<List<Order>> ExecuteAsync(Guid customerId, CancellationToken cancellationToken)
        => await dbContext.Orders
            .Include(order => order.Lines)
            .Where(order => order.CustomerId == customerId && Order.OpenStatuses.Contains(order.Status))
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
}
