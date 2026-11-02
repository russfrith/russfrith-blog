namespace OrderLightCqrs.Application.Dtos;

public record OrderDetailLineDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record OrderDetailDto(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    decimal Total,
    IReadOnlyList<OrderDetailLineDto> Lines);
