using RepositoryPattern.Domain;

namespace RepositoryPattern.Application.Specifications;

// The behavioral definition of "a customer's open orders". The repository's
// tuned, hand-written-SQL path is asserted to return exactly the same rows,
// in exactly the same order, as this spec - see the contract tests.
public sealed class OpenOrdersForCustomer : Specification<Order>
{
    public OpenOrdersForCustomer(Guid customerId)
        : base(order => order.CustomerId == customerId && Order.OpenStatuses.Contains(order.Status))
    {
        AddInclude(order => order.Lines);
        ApplyOrderByDescending(order => order.CreatedAt);
    }
}
