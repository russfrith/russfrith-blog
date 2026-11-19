namespace Shop.Onion.Application.Orders;

public record CreateOrderLine(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreateOrderCommand(Guid CustomerId, List<CreateOrderLine> Lines);
