using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option3_ClassicRepository;

// Option 3: the "classic" named-method repository, the kind that predates
// specifications. Note it owns SaveChangesAsync itself - there's no separate
// unit of work, which is exactly the thing the constrained hybrid (option 5)
// forbids.
public interface IOrderRepository
{
    Task<List<Order>> GetOpenOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
