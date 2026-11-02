namespace OrderLightCqrs.Application.Commands;

public record CreateOrderLineInput(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreateOrderCommand(Guid CustomerId, IReadOnlyList<CreateOrderLineInput> Lines);
