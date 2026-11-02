using Microsoft.EntityFrameworkCore;
using OrderLightCqrs.Application.Commands;
using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure.WriteExceptions;

namespace OrderLightCqrs.Tests.Integration;

[Collection("Database")]
public class BulkMarkOrdersShippedIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public BulkMarkOrdersShippedIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HandleAsync_ShipsOldSubmittedOrders_ButNotRecentOrDraftOrders()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await using var context = _fixture.CreateContext();
        context.Customers.Add(new Infrastructure.Reference.Customer { Id = customerId, Name = "Test" });
        context.Products.Add(new Infrastructure.Reference.Product { Id = productId, Name = "Test", Category = "Test" });

        var oldSubmitted = Order.Create(customerId);
        oldSubmitted.AddLine(productId, 1, 10m);
        oldSubmitted.Submit();

        var recentSubmitted = Order.Create(customerId);
        recentSubmitted.AddLine(productId, 1, 10m);
        recentSubmitted.Submit();

        var draft = Order.Create(customerId);
        draft.AddLine(productId, 1, 10m);

        context.Orders.AddRange(oldSubmitted, recentSubmitted, draft);
        await context.SaveChangesAsync();

        // Backdate one order's submitted_at_utc past the cutoff using a direct update,
        // since the aggregate itself has no reason to expose a way to backdate a timestamp.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"update orders set submitted_at_utc = now() - interval '10 days' where id = {oldSubmitted.Id}");

        var handler = new BulkMarkOrdersShippedHandler(context);
        var updatedCount = await handler.HandleAsync(new BulkMarkOrdersShippedCommand(OlderThanDays: 3));

        Assert.Equal(1, updatedCount);

        await using var verifyContext = _fixture.CreateContext();
        var orders = await verifyContext.Orders
            .Where(o => o.Id == oldSubmitted.Id || o.Id == recentSubmitted.Id || o.Id == draft.Id)
            .ToListAsync();
        var statuses = orders.ToDictionary(o => o.Id, o => o.Status);

        Assert.Equal(OrderStatus.Shipped, statuses[oldSubmitted.Id]);
        Assert.Equal(OrderStatus.Submitted, statuses[recentSubmitted.Id]);
        Assert.Equal(OrderStatus.Draft, statuses[draft.Id]);
    }
}
