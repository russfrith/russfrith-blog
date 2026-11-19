using Microsoft.EntityFrameworkCore;
using Shop.VerticalSlice.Common;
using Shop.VerticalSlice.Data;
using Shop.VerticalSlice.Features.Orders.ApplyDiscountCode;
using Shop.VerticalSlice.Features.Orders.CreateOrder;
using Shop.VerticalSlice.Features.Products.CreateProduct;
using Shop.VerticalSlice.Features.Products.DeleteProduct;
using Shop.VerticalSlice.Features.Products.ListProducts;
using Shop.VerticalSlice.Features.Products.UpdateProduct;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Shop") ?? "Data Source=vertical-slice-shop.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<Shop.VerticalSlice.Features.Products.CreateProduct.Handler>();
builder.Services.AddScoped<Shop.VerticalSlice.Features.Products.UpdateProduct.Handler>();
builder.Services.AddScoped<Shop.VerticalSlice.Features.Products.DeleteProduct.Handler>();
builder.Services.AddScoped<Shop.VerticalSlice.Features.Products.ListProducts.Handler>();
builder.Services.AddScoped<Shop.VerticalSlice.Features.Orders.CreateOrder.Handler>();
builder.Services.AddScoped<Shop.VerticalSlice.Features.Orders.ApplyDiscountCode.Handler>();

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

app.MapCreateProduct();
app.MapUpdateProduct();
app.MapDeleteProduct();
app.MapListProducts();
app.MapCreateOrder();
app.MapApplyDiscountCode();

app.Run();

public partial class Program;
