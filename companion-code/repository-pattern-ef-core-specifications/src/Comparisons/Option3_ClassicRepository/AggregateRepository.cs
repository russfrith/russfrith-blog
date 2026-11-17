using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option3_ClassicRepository;

// Exists purely to carry a compile-time constraint: nothing can derive from
// this for a type that isn't an aggregate root, even though the repository
// interface itself (IOrderRepository) stays concrete and named per aggregate,
// with no shared implementation here.
public abstract class AggregateRepository<TAggregate> where TAggregate : class, IAggregateRoot;
