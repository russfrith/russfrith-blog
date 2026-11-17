using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Application.Specifications;

namespace RepositoryPattern.Infrastructure.Persistence;

// The one place that knows how to turn an ISpecification<T> into an EF Core
// query. Specs themselves stay free of EF Core concepts; this class is where
// Include(), Where(), OrderBy() and AsNoTracking() actually get called.
internal static class SpecificationEvaluator
{
    public static IQueryable<T> Apply<T>(IQueryable<T> query, ISpecification<T> specification) where T : class
    {
        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
            query = query.OrderBy(specification.OrderBy);
        else if (specification.OrderByDescending is not null)
            query = query.OrderByDescending(specification.OrderByDescending);

        if (specification.IsPagingEnabled)
            query = query.Skip(specification.Skip!.Value).Take(specification.Take!.Value);

        if (specification.AsNoTracking)
            query = query.AsNoTracking();

        return query;
    }
}
