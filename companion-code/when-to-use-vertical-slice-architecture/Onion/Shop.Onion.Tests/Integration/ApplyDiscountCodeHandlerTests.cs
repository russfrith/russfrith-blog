using Shop.Onion.Application.Orders;
using Shop.Onion.Domain;
using Shop.Onion.Infrastructure.Orders;

namespace Shop.Onion.Tests.Integration;

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

        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 2, 50m);
        var discountCode = DiscountCode.Create("SAVE10", DiscountType.Percentage, 10, 0, true, 5, DateTimeOffset.UtcNow.AddDays(30));

        context.Orders.Add(order);
        context.DiscountCodes.Add(discountCode);
        await context.SaveChangesAsync();

        var handler = new ApplyDiscountCodeHandler(
            new OrderRepository(context),
            new DiscountCodeRepository(context),
            new Shop.Onion.Infrastructure.EfUnitOfWork(context),
            TimeProvider.System);

        var result = await handler.HandleAsync(new ApplyDiscountCodeCommand(order.Id, "SAVE10"));

        Assert.Equal(100m, result.Subtotal);
        Assert.Equal(10m, result.DiscountTotal);
        Assert.Equal(90m, result.Total);

        using var verifyContext = _factory.CreateContext();
        var persistedCode = await verifyContext.DiscountCodes.FindAsync(discountCode.Id);
        Assert.Equal(1, persistedCode!.RedemptionCount);
    }

    [Fact]
    public async Task HandleAsync_UnknownCode_ThrowsDomainException()
    {
        using var context = _factory.CreateContext();

        var order = Order.Create(Guid.NewGuid());
        order.AddLine(Guid.NewGuid(), 1, 50m);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var handler = new ApplyDiscountCodeHandler(
            new OrderRepository(context),
            new DiscountCodeRepository(context),
            new Shop.Onion.Infrastructure.EfUnitOfWork(context),
            TimeProvider.System);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.HandleAsync(new ApplyDiscountCodeCommand(order.Id, "DOESNOTEXIST")));
    }
}
