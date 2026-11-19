using Shop.VerticalSlice.Common;
using Shop.VerticalSlice.Data;
using Shop.VerticalSlice.Features.Orders.ApplyDiscountCode;

namespace Shop.VerticalSlice.Tests.Orders;

public class ApplyDiscountCodeHandlerTests : IClassFixture<SqliteContextFactory>
{
    private readonly SqliteContextFactory _factory;

    public ApplyDiscountCodeHandlerTests(SqliteContextFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HandleAsync_ValidCode_AppliesDiscountAndPersistsRedemption()
    {
        using var context = _factory.CreateContext();

        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Lines = [new OrderLineEntity { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 50m }]
        };
        var discountCode = new DiscountCodeEntity
        {
            Id = Guid.NewGuid(), Code = "SAVE10", Type = DiscountType.Percentage, Value = 10,
            MaxRedemptions = 5, IsStackable = true, ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        context.Orders.Add(order);
        context.DiscountCodes.Add(discountCode);
        await context.SaveChangesAsync();

        var handler = new Handler(context, TimeProvider.System);
        var response = await handler.HandleAsync(order.Id, new Request("SAVE10"));

        Assert.Equal(100m, response.Subtotal);
        Assert.Equal(10m, response.DiscountTotal);
        Assert.Equal(90m, response.Total);

        using var verifyContext = _factory.CreateContext();
        var persistedCode = await verifyContext.DiscountCodes.FindAsync(discountCode.Id);
        Assert.Equal(1, persistedCode!.RedemptionCount);
    }

    [Fact]
    public async Task HandleAsync_UnknownCode_ThrowsDomainException()
    {
        using var context = _factory.CreateContext();

        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Lines = [new OrderLineEntity { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 50m }]
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var handler = new Handler(context, TimeProvider.System);

        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(order.Id, new Request("DOESNOTEXIST")));
    }
}
