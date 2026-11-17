using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Application.Abstractions;

public interface IReadRepository<T> where T : class, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken);
    Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken);
    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken);
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken);
}
