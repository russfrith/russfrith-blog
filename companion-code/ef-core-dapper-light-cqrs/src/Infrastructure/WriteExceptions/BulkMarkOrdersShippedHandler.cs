using Dapper;
using Microsoft.EntityFrameworkCore;
using OrderLightCqrs.Application.Commands;
using OrderLightCqrs.Infrastructure.Write;

namespace OrderLightCqrs.Infrastructure.WriteExceptions;

/// <summary>
/// Documented exception #2: a set-based write that bypasses the Order aggregate entirely.
/// Loading and saving thousands of Order aggregates one at a time just to flip a status
/// column isn't protecting any invariant EF Core can't express in a WHERE clause, so this
/// is a plain UPDATE. The one invariant that matters - only a Submitted order can become
/// Shipped - is encoded directly in the SQL below, and is exactly what
/// OrderAggregateTests.MarkShipped_FromDraft_Throws (the Domain-level equivalent) checks
/// for the normal, per-aggregate path. Kept out of IOrderRepository on purpose: this is not
/// a repository method, it's a named, narrow, tested exception.
/// </summary>
public class BulkMarkOrdersShippedHandler : IBulkMarkOrdersShippedHandler
{
    private readonly AppDbContext _context;

    public BulkMarkOrdersShippedHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> HandleAsync(BulkMarkOrdersShippedCommand command, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        const string sql = """
            update orders
            set status = 'Shipped'
            where status = 'Submitted'
              and submitted_at_utc < @CutoffUtc
            """;

        var cutoffUtc = DateTimeOffset.UtcNow.AddDays(-command.OlderThanDays);

        var commandDefinition = new CommandDefinition(
            sql,
            new { CutoffUtc = cutoffUtc },
            cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(commandDefinition);
    }
}
