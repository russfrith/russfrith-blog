using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option2_QueryObjects;

// Exists purely to carry a compile-time constraint: nothing can derive from
// this for a type that isn't an aggregate root, even though a query object
// itself stays a concrete, non-generic class per use case, with no shared
// behavior of its own.
public abstract class AggregateQueryObject<TAggregate> where TAggregate : class, IAggregateRoot;
