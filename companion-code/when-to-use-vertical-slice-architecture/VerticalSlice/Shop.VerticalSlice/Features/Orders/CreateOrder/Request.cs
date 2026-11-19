namespace Shop.VerticalSlice.Features.Orders.CreateOrder;

public record OrderLineRequest(Guid ProductId, int Quantity, decimal UnitPrice);

public record Request(Guid CustomerId, List<OrderLineRequest> Lines);
