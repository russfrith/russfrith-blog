using Shop.VerticalSlice.Common;
using Shop.VerticalSlice.Data;

namespace Shop.VerticalSlice.Features.Orders.ApplyDiscountCode;

/// <summary>
/// The rule-heavy part of this slice: eligibility, expiry, usage limits, and stacking.
/// Kept as a small, pure, unit-testable type rather than inline in the handler, but
/// scoped to this feature folder rather than a shared domain project (see the post
/// for the trade-off that choice makes against the Onion version's Order aggregate).
/// </summary>
public static class DiscountRules
{
    public static decimal Apply(OrderEntity order, DiscountCodeEntity discountCode, DateTimeOffset nowUtc)
    {
        if (order.Status != OrderStatus.Draft)
        {
            throw new DomainException($"Order {order.Id} cannot be modified once it has been submitted ({order.Status}).");
        }

        if (order.AppliedDiscounts.Any(d => d.DiscountCodeId == discountCode.Id))
        {
            throw new DomainException($"Discount code {discountCode.Code} has already been applied to this order.");
        }

        if (nowUtc >= discountCode.ExpiresAtUtc)
        {
            throw new DomainException($"Discount code {discountCode.Code} expired on {discountCode.ExpiresAtUtc:u}.");
        }

        if (discountCode.RedemptionCount >= discountCode.MaxRedemptions)
        {
            throw new DomainException($"Discount code {discountCode.Code} has reached its redemption limit.");
        }

        var subtotal = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        if (subtotal < discountCode.MinimumOrderAmount)
        {
            throw new DomainException(
                $"Discount code {discountCode.Code} requires a minimum order of {discountCode.MinimumOrderAmount:C}.");
        }

        if (!discountCode.IsStackable && order.AppliedDiscounts.Count > 0)
        {
            throw new DomainException($"Discount code {discountCode.Code} cannot be combined with another discount.");
        }

        if (order.AppliedDiscounts.Any(d => !d.IsStackable))
        {
            throw new DomainException("This order already has a non-stackable discount applied.");
        }

        return discountCode.Type switch
        {
            DiscountType.Percentage => Math.Round(subtotal * (discountCode.Value / 100m), 2, MidpointRounding.AwayFromZero),
            DiscountType.FixedAmount => Math.Min(discountCode.Value, subtotal),
            _ => throw new DomainException($"Unknown discount type: {discountCode.Type}.")
        };
    }
}
