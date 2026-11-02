namespace OrderLightCqrs.Application.Dtos;

// CreatedAtUtc is DateTime, not DateTimeOffset: Npgsql's default mapping for "timestamp
// with time zone" on the read path is DateTime (Kind=Utc), while EF Core's Npgsql provider
// happily maps the Domain's DateTimeOffset on the write side. Dapper materializes DTOs
// straight from the reader's native type, so the read-side DTOs follow Npgsql's default
// rather than fighting it with a legacy compatibility switch.
public record OrderListItemDto(
    Guid OrderId,
    string CustomerName,
    DateTime CreatedAtUtc,
    string Status,
    int LineCount,
    decimal Total);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record OrderListFilter(
    string? Status,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int Page,
    int PageSize);
