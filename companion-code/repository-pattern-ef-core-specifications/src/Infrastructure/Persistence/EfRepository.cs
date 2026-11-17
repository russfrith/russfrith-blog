using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Infrastructure.Persistence;

// Concrete, not abstract: an aggregate with no named queries needs no class
// of its own - it's registered as an open generic (see ServiceCollectionExtensions).
// An aggregate with named methods derives from this and adds them (see OrderRepository).
//
// Deliberately NOT abstract, and deliberately exposing no DbSet/IQueryable/DbContext
// as protected members - see ComposeWithRawSql, the one narrow exception, and
// FINDINGS.md for why.
internal class EfRepository<T>(OrderingDbContext dbContext) : IRepository<T> where T : class, IAggregateRoot
{
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Set<T>().FindAsync([id], cancellationToken);

    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken)
        => await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken)
        => await ApplySpecification(specification).ToListAsync(cancellationToken);

    public async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken)
        => await ApplySpecification(specification).AnyAsync(cancellationToken);

    public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken)
        => await ApplySpecification(specification).CountAsync(cancellationToken);

    public void Add(T entity) => dbContext.Set<T>().Add(entity);

    public void Remove(T entity) => dbContext.Set<T>().Remove(entity);

    private IQueryable<T> ApplySpecification(ISpecification<T> specification)
        => SpecificationEvaluator.Apply(dbContext.Set<T>(), specification);

    // The one protected member the base class exposes: a derived, named-method
    // repository (OrderRepository) can compose a hand-written SQL fragment with
    // Include()/ordering from a spec, without ever seeing a bare DbSet or IQueryable.
    protected IQueryable<T> ComposeWithRawSql(FormattableString sql, ISpecification<T>? specification = null)
    {
        IQueryable<T> query = dbContext.Set<T>().FromSqlInterpolated(sql);
        return specification is null ? query : SpecificationEvaluator.Apply(query, specification);
    }
}
