namespace Shop.VerticalSlice.Features.Products.ListProducts;

public static class Endpoint
{
    public static void MapListProducts(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products", async (Handler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(ct)));
    }
}
