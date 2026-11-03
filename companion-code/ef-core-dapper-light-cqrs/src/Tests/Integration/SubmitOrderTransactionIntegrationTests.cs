using Microsoft.EntityFrameworkCore;
using OrderLightCqrs.Application;
using OrderLightCqrs.Application.Commands;
using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure.Write;
using OrderLightCqrs.Infrastructure.WriteExceptions;

namespace OrderLightCqrs.Tests.Integration;

/// <summary>
/// Proves the claim in the post: the EF Core save (order status) and the Dapper insert
/// (audit row) commit together or not at all, because they share one connection and one
/// transaction (DbContext.Database.GetDbConnection() / CurrentTransaction).
/// </summary>
[Collection("Database")]
public class SubmitOrderTransactionIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public SubmitOrderTransactionIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedDraftOrderAsync()
    {
        await using var context = _fixture.CreateContext();
        var customer = new Infrastructure.Reference.Customer { Id = Guid.NewGuid(), Name = "Test Customer" };
        var product = new Infrastructure.Reference.Product { Id = Guid.NewGuid(), Name = "Test Product", Category = "Test" };
        context.Customers.Add(customer);
        context.Products.Add(product);

        var order = Order.Create(customer.Id);
        order.AddLine(product.Id, 1, 10m);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return order.Id;
    }

    [Fact]
    public async Task Submit_WritesAuditRow_InSameTransactionAsEfSave()
    {
        var orderId = await SeedDraftOrderAsync();

        await using var context = _fixture.CreateContext();
        var handler = new SubmitOrderHandler(
            new OrderRepository(context),
            new EfUnitOfWork(context),
            new OrderAuditWriter(context));

        await handler.HandleAsync(new SubmitOrderCommand(orderId));

        await using var verifyContext = _fixture.CreateContext();
        Order? order = await new OrderRepository(verifyContext).GetByIdAsync(orderId);
        Assert.Equal(OrderStatus.Submitted, order!.Status);

        var auditRows = await verifyContext.OrderAuditLog.Where(a => a.OrderId == orderId).ToListAsync();
        Assert.Single(auditRows);
        Assert.Equal("OrderSubmitted", auditRows[0].EventName);
    }

    [Fact]
    public async Task Submit_WhenAuditWriteFails_RollsBackTheStatusChangeToo()
    {
        var orderId = await SeedDraftOrderAsync();

        await using var context = _fixture.CreateContext();
        var handler = new SubmitOrderHandler(
            new OrderRepository(context),
            new EfUnitOfWork(context),
            new ThrowingAuditWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new SubmitOrderCommand(orderId)));

        await using var verifyContext = _fixture.CreateContext();
        Order? order = await new OrderRepository(verifyContext).GetByIdAsync(orderId);

        // If the audit write and the EF save were NOT sharing a transaction, this would be
        // Submitted (EF's SaveChangesAsync already committed before the audit write ran).
        Assert.Equal(OrderStatus.Draft, order!.Status);

        var auditRows = await verifyContext.OrderAuditLog.Where(a => a.OrderId == orderId).ToListAsync();
        Assert.Empty(auditRows);
    }

    private class ThrowingAuditWriter : IOrderAuditWriter
    {
        public Task WriteAsync(Guid orderId, string eventName, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated audit write failure.");
    }
}
