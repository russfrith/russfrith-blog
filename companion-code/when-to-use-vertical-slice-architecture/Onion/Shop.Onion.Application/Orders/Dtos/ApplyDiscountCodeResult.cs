namespace Shop.Onion.Application.Orders.Dtos;

public record ApplyDiscountCodeResult(decimal Subtotal, decimal DiscountTotal, decimal Total);
