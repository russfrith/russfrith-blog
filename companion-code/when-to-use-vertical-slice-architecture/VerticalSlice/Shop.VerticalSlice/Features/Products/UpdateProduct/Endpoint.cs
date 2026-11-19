using Shop.VerticalSlice.Common;

namespace Shop.VerticalSlice.Features.Products.UpdateProduct;

public static class Endpoint
{
    public static void MapUpdateProduct(this IEndpointRouteBuilder app)
    {
        app.MapPut("/products/{productId:guid}", async (Guid productId, Request request, Handler handler, CancellationToken ct) =>
            {
                await handler.HandleAsync(productId, request, ct);
                return Results.NoContent();
            })
            .WithValidation(new Validator());
    }
}
