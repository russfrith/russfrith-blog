namespace OrderLightCqrs.Infrastructure.Write;

/// <summary>
/// Schema-only: EF owns the migration for this table so it lives in the same migration
/// history as everything else, but nothing ever queries or saves it through EF. Every row
/// is written by Dapper (see WriteExceptions/OrderAuditWriter.cs) inside the same
/// transaction as the EF save it's auditing.
/// </summary>
public class OrderAuditLogEntry
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
