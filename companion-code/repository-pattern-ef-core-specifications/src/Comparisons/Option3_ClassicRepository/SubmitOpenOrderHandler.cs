using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option3_ClassicRepository;

public class SubmitOpenOrderHandler(IOrderRepository orderRepository)
{
    public async Task<Order?> HandleAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var openOrders = await orderRepository.GetOpenOrdersForCustomerAsync(customerId, cancellationToken);

        var orderToSubmit = openOrders.FirstOrDefault(order => order.Status == OrderStatus.Draft);
        if (orderToSubmit is null)
            return null;

        orderToSubmit.Submit();
        await orderRepository.SaveChangesAsync(cancellationToken);

        return orderToSubmit;
    }
}
