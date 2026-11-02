namespace OrderLightCqrs.Application.Dtos;

public record CustomerOrderHistoryRowDto(
    Guid OrderId,
    DateTime CreatedAtUtc,
    string Status,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
