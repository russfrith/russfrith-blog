using Shop.VerticalSlice.Common;

namespace Shop.VerticalSlice.Features.Orders.ApplyDiscountCode;

public static class Endpoint
{
    public static void MapApplyDiscountCode(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{orderId:guid}/discount-code", async (Guid orderId, Request request, Handler handler, CancellationToken ct) =>
            {
                var response = await handler.HandleAsync(orderId, request, ct);
                return Results.Ok(response);
            })
            .WithValidation(new Validator());
    }
}
