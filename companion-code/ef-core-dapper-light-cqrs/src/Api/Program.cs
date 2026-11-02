using OrderLightCqrs.Application.Commands;
using OrderLightCqrs.Application.Dtos;
using OrderLightCqrs.Application.Queries;
using OrderLightCqrs.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:OrdersDb configuration.");

builder.Services.AddOrderLightCqrsInfrastructure(connectionString);

var app = builder.Build();

app.MapGet("/", () => "OrderLightCqrs API");

// Write side: command handler -> repository -> aggregate -> EF Core -> unit of work.
app.MapPost("/orders", async (CreateOrderCommand command, ICreateOrderHandler handler) =>
{
    var orderId = await handler.HandleAsync(command);
    return Results.Created($"/orders/{orderId}", new { orderId });
});

app.MapPost("/orders/{orderId:guid}/submit", async (Guid orderId, ISubmitOrderHandler handler) =>
{
    await handler.HandleAsync(new SubmitOrderCommand(orderId));
    return Results.NoContent();
});

app.MapPost("/orders/bulk-mark-shipped", async (int olderThanDays, IBulkMarkOrdersShippedHandler handler) =>
{
    var count = await handler.HandleAsync(new BulkMarkOrdersShippedCommand(olderThanDays));
    return Results.Ok(new { updated = count });
});

// Read side: query service -> Dapper -> DTO.
app.MapGet("/orders", async (string? status, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, int page, int pageSize, IOrderQueries queries) =>
{
    var filter = new OrderListFilter(status, fromUtc, toUtc, page == 0 ? 1 : page, pageSize == 0 ? 50 : pageSize);
    return Results.Ok(await queries.GetOrderListAsync(filter));
});

app.MapGet("/orders/{orderId:guid}", async (Guid orderId, IOrderQueries queries) =>
{
    var detail = await queries.GetOrderDetailAsync(orderId);
    return detail is null ? Results.NotFound() : Results.Ok(detail);
});

app.MapGet("/reports/sales", async (DateTimeOffset fromUtc, DateTimeOffset toUtc, IOrderQueries queries) =>
{
    return Results.Ok(await queries.GetSalesReportAsync(fromUtc, toUtc));
});

app.MapGet("/customers/{customerId:guid}/order-history", async (Guid customerId, IOrderQueries queries) =>
{
    return Results.Ok(await queries.GetCustomerOrderHistoryAsync(customerId));
});

app.Run();

public partial class Program;
