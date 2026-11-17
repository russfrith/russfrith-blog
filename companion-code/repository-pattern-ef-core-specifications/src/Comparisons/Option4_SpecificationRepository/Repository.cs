using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Comparisons.Shared;

namespace RepositoryPattern.Comparisons.Option4_SpecificationRepository;

public class Repository<T>(ComparisonDbContext dbContext) : IRepository<T> where T : class
{
    public async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken)
    {
        IQueryable<T> query = dbContext.Set<T>();

        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderByDescending is not null)
            query = query.OrderByDescending(specification.OrderByDescending);

        return await query.ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
