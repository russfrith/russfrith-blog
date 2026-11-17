using System.Linq.Expressions;

namespace RepositoryPattern.Application.Specifications;

// Our own, minimal specification contract: criteria, includes, ordering, paging,
// a tracking flag. No projections - a spec always describes how to fetch an
// aggregate, never a DTO shape.
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Skip { get; }
    int? Take { get; }
    bool IsPagingEnabled { get; }
    bool AsNoTracking { get; }
}
