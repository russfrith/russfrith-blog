using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option4_SpecificationRepository;

// Option 4: a specification-driven repository, reusing Application's own
// ISpecification<T> and the real OpenOrdersForCustomer spec, and the same
// IAggregateRoot constraint as the constrained hybrid (option 5) - that
// constraint is cheap regardless of which repository shape you build. What
// this doesn't have is the open-generic EfRepository<T> (a new aggregate
// still needs its own class, like this one), a dedicated tuning seam for one
// query, or the separate unit of work that option 5 adds. It still owns
// SaveChangesAsync itself, like option 3.
public interface IRepository<T> where T : class, IAggregateRoot
{
    Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
