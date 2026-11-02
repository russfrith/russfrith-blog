using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure.Write;

namespace OrderLightCqrs.Tests.Integration;

[Collection("Database")]
public class OrderRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public OrderRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_ThenGetById_RoundTripsAggregateWithLines()
    {
        await using var seedContext = _fixture.CreateContext();
        var customer = new Infrastructure.Reference.Customer { Id = Guid.NewGuid(), Name = "Test Customer" };
        var product = new Infrastructure.Reference.Product { Id = Guid.NewGuid(), Name = "Test Product", Category = "Test" };
        seedContext.Customers.Add(customer);
        seedContext.Products.Add(product);
        await seedContext.SaveChangesAsync();

        var order = Order.Create(customer.Id);
        order.AddLine(product.Id, 3, 19.99m);
        order.AddLine(product.Id, 1, 5.00m);

        await using var writeContext = _fixture.CreateContext();
        var repository = new OrderRepository(writeContext);
        repository.Add(order);
        await writeContext.SaveChangesAsync();

        await using var readContext = _fixture.CreateContext();
        var readRepository = new OrderRepository(readContext);
        var loaded = await readRepository.GetByIdAsync(order.Id);

        Assert.NotNull(loaded);
        Assert.Equal(order.Id, loaded!.Id);
        Assert.Equal(2, loaded.Lines.Count);
        Assert.Equal(64.97m, loaded.Total);
    }

    [Fact]
    public async Task GetById_ForMissingOrder_ReturnsNull()
    {
        await using var context = _fixture.CreateContext();
        var repository = new OrderRepository(context);

        var loaded = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(loaded);
    }

    [Fact]
    public async Task Submit_PersistsStatusChange()
    {
        await using var seedContext = _fixture.CreateContext();
        var customer = new Infrastructure.Reference.Customer { Id = Guid.NewGuid(), Name = "Test Customer" };
        var product = new Infrastructure.Reference.Product { Id = Guid.NewGuid(), Name = "Test Product", Category = "Test" };
        seedContext.Customers.Add(customer);
        seedContext.Products.Add(product);

        var order = Order.Create(customer.Id);
        order.AddLine(product.Id, 1, 10m);
        seedContext.Orders.Add(order);
        await seedContext.SaveChangesAsync();

        await using var submitContext = _fixture.CreateContext();
        var repository = new OrderRepository(submitContext);
        var loaded = await repository.GetByIdAsync(order.Id);
        loaded!.Submit();
        await submitContext.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateContext();
        var verifyRepository = new OrderRepository(verifyContext);
        var verified = await verifyRepository.GetByIdAsync(order.Id);

        Assert.Equal(OrderStatus.Submitted, verified!.Status);
        Assert.NotNull(verified.SubmittedAtUtc);
    }
}
