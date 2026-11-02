using Microsoft.EntityFrameworkCore;
using OrderLightCqrs.Application;

namespace OrderLightCqrs.Infrastructure.Write;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public EfUnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await action(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
