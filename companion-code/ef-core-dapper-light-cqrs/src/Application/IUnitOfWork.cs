namespace OrderLightCqrs.Application;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="action"/> inside a single database transaction. Used when a
    /// command handler needs to combine an EF Core save with a Dapper write (see
    /// <see cref="IOrderAuditWriter"/>) and both must commit or roll back together.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
