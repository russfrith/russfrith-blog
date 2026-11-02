namespace OrderLightCqrs.Application;

/// <summary>
/// Writes an audit trail row for an order state change. The Infrastructure implementation
/// uses Dapper against the same connection and transaction EF Core is using, so the audit
/// row commits or rolls back atomically with the aggregate's own save. This is the one place
/// outside the read layer where Dapper is used for a write; see
/// Infrastructure/WriteExceptions/OrderAuditWriter.cs for why that is contained and tested.
/// </summary>
public interface IOrderAuditWriter
{
    Task WriteAsync(Guid orderId, string eventName, CancellationToken cancellationToken = default);
}
