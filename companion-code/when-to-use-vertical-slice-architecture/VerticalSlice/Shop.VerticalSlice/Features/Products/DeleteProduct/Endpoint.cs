namespace Shop.VerticalSlice.Features.Products.DeleteProduct;

public static class Endpoint
{
    public static void MapDeleteProduct(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/products/{productId:guid}", async (Guid productId, Handler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(productId, ct);
            return Results.NoContent();
        });
    }
}
