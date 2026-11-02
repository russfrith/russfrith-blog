using OrderLightCqrs.Application.Dtos;
using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure.Reads;

namespace OrderLightCqrs.Tests.Integration;

[Collection("Database")]
public class OrderQueriesIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public OrderQueriesIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid CustomerId, Guid ProductId, Guid OrderId)> SeedOneSubmittedOrderAsync()
    {
        await using var context = _fixture.CreateContext();
        var customer = new Infrastructure.Reference.Customer { Id = Guid.NewGuid(), Name = "Ada Lovelace" };
        var product = new Infrastructure.Reference.Product { Id = Guid.NewGuid(), Name = "Mechanical Keyboard", Category = "Electronics" };
        context.Customers.Add(customer);
        context.Products.Add(product);

        var order = Order.Create(customer.Id);
        order.AddLine(product.Id, 2, 50m);
        order.Submit();
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return (customer.Id, product.Id, order.Id);
    }

    [Fact]
    public async Task GetOrderListAsync_FiltersByStatusAndReturnsCorrectTotals()
    {
        var (_, _, orderId) = await SeedOneSubmittedOrderAsync();

        var queries = new OrderQueries(_fixture.CreateConnectionFactory());
        var result = await queries.GetOrderListAsync(new OrderListFilter("Submitted", null, null, 1, 50));

        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal(orderId, item.OrderId);
        Assert.Equal("Ada Lovelace", item.CustomerName);
        Assert.Equal(1, item.LineCount);
        Assert.Equal(100m, item.Total);
    }

    [Fact]
    public async Task GetOrderListAsync_FilteredByNonMatchingStatus_ReturnsEmpty()
    {
        await SeedOneSubmittedOrderAsync();

        var queries = new OrderQueries(_fixture.CreateConnectionFactory());
        var result = await queries.GetOrderListAsync(new OrderListFilter("Cancelled", null, null, 1, 50));

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetOrderDetailAsync_ReturnsLinesWithProductNames()
    {
        var (customerId, productId, orderId) = await SeedOneSubmittedOrderAsync();

        var queries = new OrderQueries(_fixture.CreateConnectionFactory());
        var detail = await queries.GetOrderDetailAsync(orderId);

        Assert.NotNull(detail);
        Assert.Equal(customerId, detail!.CustomerId);
        Assert.Equal(100m, detail.Total);
        var line = Assert.Single(detail.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal("Mechanical Keyboard", line.ProductName);
    }

    [Fact]
    public async Task GetOrderDetailAsync_ForMissingOrder_ReturnsNull()
    {
        var queries = new OrderQueries(_fixture.CreateConnectionFactory());

        var detail = await queries.GetOrderDetailAsync(Guid.NewGuid());

        Assert.Null(detail);
    }

    [Fact]
    public async Task GetSalesReportAsync_AggregatesRevenueByCategoryAndMonth()
    {
        var (_, _, _) = await SeedOneSubmittedOrderAsync();

        var queries = new OrderQueries(_fixture.CreateConnectionFactory());
        var rows = await queries.GetSalesReportAsync(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        var row = Assert.Single(rows);
        Assert.Equal("Electronics", row.Category);
        Assert.Equal(100m, row.Revenue);
        Assert.Equal(1, row.OrderCount);
    }

    [Fact]
    public async Task GetCustomerOrderHistoryAsync_ReturnsOneRowPerLine()
    {
        var (customerId, productId, orderId) = await SeedOneSubmittedOrderAsync();

        var queries = new OrderQueries(_fixture.CreateConnectionFactory());
        var rows = await queries.GetCustomerOrderHistoryAsync(customerId);

        var row = Assert.Single(rows);
        Assert.Equal(orderId, row.OrderId);
        Assert.Equal(productId, row.ProductId);
        Assert.Equal(2, row.Quantity);
        Assert.Equal(100m, row.LineTotal);
    }
}
