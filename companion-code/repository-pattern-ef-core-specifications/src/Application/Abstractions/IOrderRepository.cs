using RepositoryPattern.Domain;

namespace RepositoryPattern.Application.Abstractions;

public interface IOrderRepository : IRepository<Order>
{
    // The one named, tuned query this post defends: it has an EF Core
    // FromSqlInterpolated implementation, and a behaviorally identical
    // specification (OpenOrdersForCustomer) that the contract tests pin it to.
    Task<List<Order>> GetOpenOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken);
}
