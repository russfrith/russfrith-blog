namespace Shop.VerticalSlice.Features.Orders.ApplyDiscountCode;

public record Response(decimal Subtotal, decimal DiscountTotal, decimal Total);
