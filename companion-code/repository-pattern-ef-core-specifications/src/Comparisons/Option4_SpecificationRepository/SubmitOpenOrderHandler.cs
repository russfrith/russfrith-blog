using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option4_SpecificationRepository;

public class SubmitOpenOrderHandler(IRepository<Order> orderRepository)
{
    public async Task<Order?> HandleAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var openOrders = await orderRepository.ListAsync(new OpenOrdersForCustomer(customerId), cancellationToken);

        var orderToSubmit = openOrders.FirstOrDefault(order => order.Status == OrderStatus.Draft);
        if (orderToSubmit is null)
            return null;

        orderToSubmit.Submit();
        await orderRepository.SaveChangesAsync(cancellationToken);

        return orderToSubmit;
    }
}
