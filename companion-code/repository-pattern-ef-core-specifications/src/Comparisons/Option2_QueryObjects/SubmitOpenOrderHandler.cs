using RepositoryPattern.Comparisons.Shared;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option2_QueryObjects;

public class SubmitOpenOrderHandler(GetOpenOrdersForCustomer query, ComparisonDbContext dbContext)
{
    public async Task<Order?> HandleAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var openOrders = await query.ExecuteAsync(customerId, cancellationToken);

        var orderToSubmit = openOrders.FirstOrDefault(order => order.Status == OrderStatus.Draft);
        if (orderToSubmit is null)
            return null;

        orderToSubmit.Submit();
        await dbContext.SaveChangesAsync(cancellationToken);

        return orderToSubmit;
    }
}
