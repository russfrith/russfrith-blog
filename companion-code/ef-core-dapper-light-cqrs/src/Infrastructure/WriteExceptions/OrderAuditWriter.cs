using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderLightCqrs.Application;
using OrderLightCqrs.Infrastructure.Write;

namespace OrderLightCqrs.Infrastructure.WriteExceptions;

/// <summary>
/// Documented exception #1: Dapper used outside the read layer, strictly for an audit-log
/// insert that must commit atomically with the EF Core save it's auditing. It borrows EF's
/// own connection and transaction via DbContext.Database.GetDbConnection()/GetDbTransaction()
/// and never opens, closes, or disposes either: EF owns their lifetime. This is the only
/// place that holds true, which is exactly why the architecture test (see
/// ArchitectureTests.DapperIsOnlyReferencedFromReadsOrDocumentedWriteExceptions) pins the
/// small set of classes allowed to reference Dapper for a write.
/// </summary>
public class OrderAuditWriter : IOrderAuditWriter
{
    private readonly AppDbContext _context;

    public OrderAuditWriter(AppDbContext context)
    {
        _context = context;
    }

    public async Task WriteAsync(Guid orderId, string eventName, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var transaction = _context.Database.CurrentTransaction?.GetDbTransaction();

        const string sql = """
            insert into order_audit_log (order_id, event_name, occurred_at_utc)
            values (@OrderId, @EventName, @OccurredAtUtc)
            """;

        var command = new CommandDefinition(
            sql,
            new { OrderId = orderId, EventName = eventName, OccurredAtUtc = DateTimeOffset.UtcNow },
            transaction,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }
}
