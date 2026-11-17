using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option5_ConstrainedHybrid;

// Option 5: the constrained hybrid this post defends. Unlike options 1-4,
// this isn't a smaller stand-in built for the comparison - it's the actual
// production IOrderRepository and IUnitOfWork from Application/Infrastructure,
// wired up the same way the Api project wires them up.
public class SubmitOpenOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
{
    public async Task<Order?> HandleAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var openOrders = await orderRepository.GetOpenOrdersForCustomerAsync(customerId, cancellationToken);

        var orderToSubmit = openOrders.FirstOrDefault(order => order.Status == OrderStatus.Draft);
        if (orderToSubmit is null)
            return null;

        orderToSubmit.Submit();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return orderToSubmit;
    }
}
