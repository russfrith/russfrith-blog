using RepositoryPattern.Domain;

namespace RepositoryPattern.Application.Abstractions;

public interface IRepository<T> : IReadRepository<T> where T : class, IAggregateRoot
{
    void Add(T entity);
    void Remove(T entity);
}
