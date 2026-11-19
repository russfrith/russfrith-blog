using Shop.Onion.Domain;

namespace Shop.Onion.Tests.Domain;

public class OrderDiscountTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Order CreateOrderWithSubtotal(decimal subtotal)
    {
        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, subtotal);
        return order;
    }

    private static DiscountCode CreatePercentageCode(
        decimal percentage = 10,
        decimal minimumOrderAmount = 0,
        bool isStackable = true,
        int maxRedemptions = 100,
        DateTimeOffset? expiresAtUtc = null)
    {
        return DiscountCode.Create(
            "SAVE10", DiscountType.Percentage, percentage, minimumOrderAmount, isStackable, maxRedemptions,
            expiresAtUtc ?? Now.AddDays(30));
    }

    [Fact]
    public void ApplyDiscountCode_PercentageDiscount_ReducesTotalByPercentage()
    {
        var order = CreateOrderWithSubtotal(100m);
        var code = CreatePercentageCode(percentage: 10);

        order.ApplyDiscountCode(code, Now);

        Assert.Equal(10m, order.DiscountTotal);
        Assert.Equal(90m, order.Total);
    }

    [Fact]
    public void ApplyDiscountCode_FixedAmountDiscount_NeverExceedsSubtotal()
    {
        var order = CreateOrderWithSubtotal(15m);
        var code = DiscountCode.Create("FLAT20", DiscountType.FixedAmount, 20, 0, true, 100, Now.AddDays(30));

        order.ApplyDiscountCode(code, Now);

        Assert.Equal(15m, order.DiscountTotal);
        Assert.Equal(0m, order.Total);
    }

    [Fact]
    public void ApplyDiscountCode_Expired_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        var code = CreatePercentageCode(expiresAtUtc: Now.AddDays(-1));

        Assert.Throws<DomainException>(() => order.ApplyDiscountCode(code, Now));
    }

    [Fact]
    public void ApplyDiscountCode_BelowMinimumOrderAmount_Throws()
    {
        var order = CreateOrderWithSubtotal(50m);
        var code = CreatePercentageCode(minimumOrderAmount: 100m);

        Assert.Throws<DomainException>(() => order.ApplyDiscountCode(code, Now));
    }

    [Fact]
    public void ApplyDiscountCode_AtOrAboveMinimumOrderAmount_Succeeds()
    {
        var order = CreateOrderWithSubtotal(100m);
        var code = CreatePercentageCode(minimumOrderAmount: 100m);

        order.ApplyDiscountCode(code, Now);

        Assert.Equal(1, order.AppliedDiscounts.Count);
    }

    [Fact]
    public void ApplyDiscountCode_UsageLimitReached_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        var code = CreatePercentageCode(maxRedemptions: 1);
        code.Redeem();

        Assert.Throws<DomainException>(() => order.ApplyDiscountCode(code, Now));
    }

    [Fact]
    public void ApplyDiscountCode_SameCodeTwice_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        var code = CreatePercentageCode();

        order.ApplyDiscountCode(code, Now);

        Assert.Throws<DomainException>(() => order.ApplyDiscountCode(code, Now));
    }

    [Fact]
    public void ApplyDiscountCode_TwoStackableCodes_BothApply()
    {
        var order = CreateOrderWithSubtotal(100m);
        var first = DiscountCode.Create("SAVE10", DiscountType.Percentage, 10, 0, true, 100, Now.AddDays(30));
        var second = DiscountCode.Create("SAVE5", DiscountType.Percentage, 5, 0, true, 100, Now.AddDays(30));

        order.ApplyDiscountCode(first, Now);
        order.ApplyDiscountCode(second, Now);

        Assert.Equal(2, order.AppliedDiscounts.Count);
        Assert.Equal(15m, order.DiscountTotal);
    }

    [Fact]
    public void ApplyDiscountCode_NonStackableAfterAnotherDiscount_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        var stackable = CreatePercentageCode(isStackable: true);
        var nonStackable = DiscountCode.Create("FLAT20", DiscountType.FixedAmount, 20, 0, false, 100, Now.AddDays(30));

        order.ApplyDiscountCode(stackable, Now);

        Assert.Throws<DomainException>(() => order.ApplyDiscountCode(nonStackable, Now));
    }

    [Fact]
    public void ApplyDiscountCode_AnotherDiscountAfterNonStackable_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        var nonStackable = DiscountCode.Create("FLAT20", DiscountType.FixedAmount, 20, 0, false, 100, Now.AddDays(30));
        var another = CreatePercentageCode();

        order.ApplyDiscountCode(nonStackable, Now);

        Assert.Throws<DomainException>(() => order.ApplyDiscountCode(another, Now));
    }

    [Fact]
    public void ApplyDiscountCode_OnSubmittedOrder_Throws()
    {
        var order = CreateOrderWithSubtotal(100m);
        order.Submit();
        var code = CreatePercentageCode();

        Assert.Throws<DomainException>(() => order.ApplyDiscountCode(code, Now));
    }
}
