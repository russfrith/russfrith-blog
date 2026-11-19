namespace Shop.Onion.Application.Orders;

public record ApplyDiscountCodeCommand(Guid OrderId, string Code);
