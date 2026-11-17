namespace RepositoryPattern.Application.Abstractions;

// Repositories never save; only a unit of work commits. That keeps "stage a
// change" and "commit the transaction" as separate decisions, which matters
// once a use case touches more than one aggregate.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
