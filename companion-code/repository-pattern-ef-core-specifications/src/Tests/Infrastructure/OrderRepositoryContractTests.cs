using Microsoft.Extensions.DependencyInjection;
using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Domain;
using Xunit;

namespace RepositoryPattern.Tests.Infrastructure;

// The contract this post leans on: the tuned, hand-written-SQL named method
// and the spec that defines its behavior must return identical rows in
// identical order, against real Postgres.
[Collection("Database")]
public class OrderRepositoryContractTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Spec_path_and_named_method_path_return_identical_orders_in_identical_order()
    {
        using var scope = fixture.Services.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var customerId = Guid.NewGuid();

        // Each order is created a few milliseconds apart: both paths order by
        // CreatedAt with no secondary tiebreaker, so creating them at the same
        // instant would leave their relative order undefined by either query.
        var draft = Order.Create(customerId);
        draft.AddLine("Widget", 1, 9.99m);
        orderRepository.Add(draft);
        await Task.Delay(5);

        var submitted = Order.Create(customerId);
        submitted.AddLine("Gadget", 2, 4.50m);
        submitted.Submit();
        orderRepository.Add(submitted);
        await Task.Delay(5);

        var shipped = Order.Create(customerId);
        shipped.AddLine("Gizmo", 1, 19.99m);
        shipped.Submit();
        shipped.Ship();
        orderRepository.Add(shipped);

        var otherCustomersOrder = Order.Create(Guid.NewGuid());
        otherCustomersOrder.AddLine("Widget", 1, 9.99m);
        orderRepository.Add(otherCustomersOrder);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var viaSpec = await orderRepository.ListAsync(new OpenOrdersForCustomer(customerId), CancellationToken.None);
        var viaNamedMethod = await orderRepository.GetOpenOrdersForCustomerAsync(customerId, CancellationToken.None);

        Assert.Equal(viaSpec.Select(order => order.Id), viaNamedMethod.Select(order => order.Id));
        Assert.Equal(2, viaSpec.Count);
        Assert.All(viaSpec, order => Assert.True(order.IsOpen));
    }

    [Fact]
    public async Task GetByIdAsync_loads_a_tracked_order_that_can_be_submitted_and_saved()
    {
        using var scope = fixture.Services.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var order = Order.Create(Guid.NewGuid());
        order.AddLine("Widget", 1, 9.99m);
        orderRepository.Add(order);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var reloaded = await orderRepository.GetByIdAsync(order.Id, CancellationToken.None);
        reloaded!.Submit();
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var afterSubmit = await orderRepository.FirstOrDefaultAsync(new OrderWithLines(order.Id), CancellationToken.None);
        Assert.Equal(OrderStatus.Submitted, afterSubmit!.Status);
    }
}
