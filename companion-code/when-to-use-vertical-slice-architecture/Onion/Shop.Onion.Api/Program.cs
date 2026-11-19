using Microsoft.EntityFrameworkCore;
using Shop.Onion.Application.Orders;
using Shop.Onion.Application.Products;
using Shop.Onion.Application.Products.Dtos;
using Shop.Onion.Application.Products.Queries;
using Shop.Onion.Domain;
using Shop.Onion.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Shop") ?? "Data Source=onion-shop.db";
builder.Services.AddOnionInfrastructure(connectionString);

builder.Services.AddScoped<ICreateProductHandler, CreateProductHandler>();
builder.Services.AddScoped<IUpdateProductHandler, UpdateProductHandler>();
builder.Services.AddScoped<IDeleteProductHandler, DeleteProductHandler>();
builder.Services.AddScoped<ICreateOrderHandler, CreateOrderHandler>();
builder.Services.AddScoped<IApplyDiscountCodeHandler, ApplyDiscountCodeHandler>();

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await SeedData.EnsureSeededAsync(context);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    if (error is DomainException domainException)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = domainException.Message });
        return;
    }

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
}));

app.MapGet("/products", async (IProductQueries queries, CancellationToken ct) =>
    Results.Ok(await queries.ListAsync(ct)));

app.MapPost("/products", async (CreateProductRequest request, ICreateProductHandler handler, CancellationToken ct) =>
{
    var id = await handler.HandleAsync(new CreateProductCommand(request.Sku, request.Name, request.Price), ct);
    return Results.Created($"/products/{id}", new { id });
});

app.MapPut("/products/{productId:guid}", async (Guid productId, UpdateProductRequest request, IUpdateProductHandler handler, CancellationToken ct) =>
{
    await handler.HandleAsync(new UpdateProductCommand(productId, request.Name, request.Price), ct);
    return Results.NoContent();
});

app.MapDelete("/products/{productId:guid}", async (Guid productId, IDeleteProductHandler handler, CancellationToken ct) =>
{
    await handler.HandleAsync(new DeleteProductCommand(productId), ct);
    return Results.NoContent();
});

app.MapPost("/orders", async (CreateOrderRequest request, ICreateOrderHandler handler, CancellationToken ct) =>
{
    var lines = request.Lines.Select(l => new CreateOrderLine(l.ProductId, l.Quantity, l.UnitPrice)).ToList();
    var id = await handler.HandleAsync(new CreateOrderCommand(request.CustomerId, lines), ct);
    return Results.Created($"/orders/{id}", new { id });
});

app.MapPost("/orders/{orderId:guid}/discount-code", async (Guid orderId, ApplyDiscountCodeRequest request, IApplyDiscountCodeHandler handler, CancellationToken ct) =>
{
    var result = await handler.HandleAsync(new ApplyDiscountCodeCommand(orderId, request.Code), ct);
    return Results.Ok(result);
});

app.Run();

public record CreateProductRequest(string Sku, string Name, decimal Price);
public record UpdateProductRequest(string Name, decimal Price);
public record CreateOrderLineRequest(Guid ProductId, int Quantity, decimal UnitPrice);
public record CreateOrderRequest(Guid CustomerId, List<CreateOrderLineRequest> Lines);
public record ApplyDiscountCodeRequest(string Code);

public partial class Program;
