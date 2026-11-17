using RepositoryPattern.Domain;

namespace RepositoryPattern.Application.Specifications;

public sealed class OrderWithLines : Specification<Order>
{
    public OrderWithLines(Guid orderId) : base(order => order.Id == orderId)
    {
        AddInclude(order => order.Lines);
    }
}
