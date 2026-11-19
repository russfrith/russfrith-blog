using Shop.VerticalSlice.Common;

namespace Shop.VerticalSlice.Features.Products.CreateProduct;

public static class Endpoint
{
    public static void MapCreateProduct(this IEndpointRouteBuilder app)
    {
        app.MapPost("/products", async (Request request, Handler handler, CancellationToken ct) =>
            {
                var id = await handler.HandleAsync(request, ct);
                return Results.Created($"/products/{id}", new { id });
            })
            .WithValidation(new Validator());
    }
}
