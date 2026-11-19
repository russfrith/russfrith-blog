using Shop.VerticalSlice.Common;
using Shop.VerticalSlice.Data;
using Shop.VerticalSlice.Features.Orders.ApplyDiscountCode;

namespace Shop.VerticalSlice.Tests.Orders;

public class DiscountRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static OrderEntity CreateOrderWithSubtotal(decimal subtotal)
    {
        var orderId = Guid.NewGuid();
        return new OrderEntity
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            Lines = [new OrderLineEntity { Id = Guid.NewGuid(), OrderId = orderId, ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = subtotal }]
        };
    }

    private static DiscountCodeEntity CreatePercentageCode(
        decimal percentage = 10,
        decimal minimumOrderAmount = 0,
        bool isStackable = true,
        int maxRedemptions = 100,
        int redemptionCount = 0,
        DateTimeOffset? expiresAtUtc = null)
    {
        return new DiscountCodeEntity
        {
            Id = Guid.NewGuid(),
            Code = "SAVE10",
            Type = DiscountType.Percentage,
            Value = percentage,
            MinimumOrderAmount = minimumOrderAmount,
            IsStackable = isStackable,
            MaxRedemptions = maxRedemptions,
            RedemptionCount = redemptionCount,
            ExpiresAtUtc = expiresAtUtc ?? Now.AddDays(30)
        };
    }

    [Fact]
    public void Apply_PercentageDiscount_ReturnsPercentageOfSubtotal()
    {
        var order = CreateOrderWithSubtotal(100m);
        var code = CreatePercentageCode(percentage: 10);

        var amount = DiscountRules.Apply(order, code, Now);

        Assert.Equal(10m, amount);
    }

    [Fact]
    public void Apply_FixedAmountDiscount_NeverExceedsSubtotal()
    {
        var order = CreateOrderWithSubtotal(15m);
        var code = new DiscountCodeEntity
        {
            Id = Guid.NewGuid(), Code = "FLAT20", Type = DiscountType.FixedAmount, Value = 20,
            MaxRedemptions = 100, ExpiresAtUtc = Now.AddDays(30), IsStackable = true
        };

        var amount = DiscountRules.Apply(order, code, Now);

        Assert.Equal(15m, amount);
    }

    [Fact]
    public void Apply_Expired_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        var code = CreatePercentageCode(expiresAtUtc: Now.AddDays(-1));

        Assert.Throws<DomainException>(() => DiscountRules.Apply(order, code, Now));
    }

    [Fact]
    public void Apply_BelowMinimumOrderAmount_Throws()
    {
        var order = CreateOrderWithSubtotal(50m);
        var code = CreatePercentageCode(minimumOrderAmount: 100m);

        Assert.Throws<DomainException>(() => DiscountRules.Apply(order, code, Now));
    }

    [Fact]
    public void Apply_UsageLimitReached_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        var code = CreatePercentageCode(maxRedemptions: 1, redemptionCount: 1);

        Assert.Throws<DomainException>(() => DiscountRules.Apply(order, code, Now));
    }

    [Fact]
    public void Apply_SameCodeTwice_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        var code = CreatePercentageCode();
        order.AppliedDiscounts.Add(new AppliedDiscountEntity { Id = Guid.NewGuid(), OrderId = order.Id, DiscountCodeId = code.Id, Code = code.Code, IsStackable = true, Amount = 10m });

        Assert.Throws<DomainException>(() => DiscountRules.Apply(order, code, Now));
    }

    [Fact]
    public void Apply_NonStackableAfterAnotherDiscount_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        order.AppliedDiscounts.Add(new AppliedDiscountEntity { Id = Guid.NewGuid(), OrderId = order.Id, DiscountCodeId = Guid.NewGuid(), Code = "SAVE5", IsStackable = true, Amount = 5m });
        var nonStackable = new DiscountCodeEntity
        {
            Id = Guid.NewGuid(), Code = "FLAT20", Type = DiscountType.FixedAmount, Value = 20,
            MaxRedemptions = 100, ExpiresAtUtc = Now.AddDays(30), IsStackable = false
        };

        Assert.Throws<DomainException>(() => DiscountRules.Apply(order, nonStackable, Now));
    }

    [Fact]
    public void Apply_AnotherDiscountAfterNonStackable_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        order.AppliedDiscounts.Add(new AppliedDiscountEntity { Id = Guid.NewGuid(), OrderId = order.Id, DiscountCodeId = Guid.NewGuid(), Code = "FLAT20", IsStackable = false, Amount = 20m });
        var another = CreatePercentageCode();

        Assert.Throws<DomainException>(() => DiscountRules.Apply(order, another, Now));
    }

    [Fact]
    public void Apply_OnSubmittedOrder_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        order.Status = OrderStatus.Submitted;
        var code = CreatePercentageCode();

        Assert.Throws<DomainException>(() => DiscountRules.Apply(order, code, Now));
    }

    [Fact]
    public void Apply_TwoStackableCodes_BothSucceed()
    {
        var order = CreateOrderWithSubtotal(100m);
        order.AppliedDiscounts.Add(new AppliedDiscountEntity { Id = Guid.NewGuid(), OrderId = order.Id, DiscountCodeId = Guid.NewGuid(), Code = "SAVE5", IsStackable = true, Amount = 5m });
        var second = CreatePercentageCode(percentage: 10);

        var amount = DiscountRules.Apply(order, second, Now);

        Assert.Equal(10m, amount);
    }
}
