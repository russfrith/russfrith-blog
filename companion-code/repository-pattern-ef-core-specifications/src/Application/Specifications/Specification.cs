using System.Linq.Expressions;

namespace RepositoryPattern.Application.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    // Read repositories default every spec to no-tracking; a spec can opt
    // back into tracking for the rare case it feeds a write path.
    public bool AsNoTracking { get; private set; } = true;

    protected Specification(Expression<Func<T, bool>>? criteria = null)
    {
        Criteria = criteria;
    }

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => Includes.Add(includeExpression);

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) => OrderBy = orderByExpression;

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression) =>
        OrderByDescending = orderByDescendingExpression;

    protected void ApplyPaging(int skip, int take)
    {
        if (OrderBy is null && OrderByDescending is null)
            throw new InvalidOperationException("Paging requires an ordering to be applied first.");

        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected void TrackResults() => AsNoTracking = false;
}
