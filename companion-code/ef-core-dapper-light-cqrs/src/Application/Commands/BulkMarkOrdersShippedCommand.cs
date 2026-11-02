namespace OrderLightCqrs.Application.Commands;

/// <summary>
/// The documented gray-area exception: marks every submitted order older than
/// <paramref name="OlderThanDays"/> as shipped in one set-based UPDATE, without loading
/// each Order aggregate. Justified because "nightly batch moves thousands of orders from
/// Submitted to Shipped" has no interesting invariant per row; the only invariant
/// (Submitted -> Shipped is a legal transition) is encoded directly in the WHERE clause.
/// See Infrastructure/WriteExceptions/BulkMarkOrdersShippedHandler.cs.
/// </summary>
public record BulkMarkOrdersShippedCommand(int OlderThanDays);
