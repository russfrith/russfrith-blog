using RepositoryPattern.Application.Specifications;

namespace RepositoryPattern.Comparisons.Option4_SpecificationRepository;

// Option 4: a specification-driven repository, reusing Application's own
// ISpecification<T> and the real OpenOrdersForCustomer spec - but without the
// IAggregateRoot constraint, the read/write split, or the separate unit of
// work that the constrained hybrid (option 5) adds. It still owns
// SaveChangesAsync itself, like option 3.
public interface IRepository<T> where T : class
{
    Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
