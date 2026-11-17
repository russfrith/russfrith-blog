using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Domain;
using RepositoryPattern.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("Missing connection string 'Orders'.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

// Everything below depends only on IOrderRepository / IUnitOfWork / ISpecification -
// no DbContext, no Microsoft.EntityFrameworkCore, enforced by the architecture tests.

app.MapGet("/customers/{customerId:guid}/open-orders", async (
    Guid customerId,
    IOrderRepository orderRepository,
    CancellationToken cancellationToken) =>
{
    var orders = await orderRepository.ListAsync(new OpenOrdersForCustomer(customerId), cancellationToken);
    return Results.Ok(orders.Select(OrderResponse.From));
});

app.MapPost("/orders/{orderId:guid}/submit", async (
    Guid orderId,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    CancellationToken cancellationToken) =>
{
    var order = await orderRepository.FirstOrDefaultAsync(new OrderWithLines(orderId), cancellationToken);
    if (order is null)
        return Results.NotFound();

    order.Submit();
    await unitOfWork.SaveChangesAsync(cancellationToken);

    return Results.Ok(OrderResponse.From(order));
});

app.Run();

internal sealed record OrderResponse(Guid Id, Guid CustomerId, string Status, int LineCount)
{
    public static OrderResponse From(Order order) => new(order.Id, order.CustomerId, order.Status.ToString(), order.Lines.Count);
}
