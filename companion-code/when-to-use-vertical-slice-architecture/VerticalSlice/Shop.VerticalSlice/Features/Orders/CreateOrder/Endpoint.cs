namespace Shop.VerticalSlice.Features.Orders.CreateOrder;

public static class Endpoint
{
    public static void MapCreateOrder(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (Request request, Handler handler, CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(request, ct);
            return Results.Created($"/orders/{id}", new { id });
        });
    }
}
