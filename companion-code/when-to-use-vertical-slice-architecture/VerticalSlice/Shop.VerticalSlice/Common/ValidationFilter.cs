using FluentValidation;

namespace Shop.VerticalSlice.Common;

/// <summary>
/// Endpoint filter that runs a FluentValidation validator against the first argument
/// of the matching type before the handler runs. This is the cross-cutting concern
/// MediatR's pipeline behaviors would normally cover; here it's an endpoint filter
/// instead, applied per-route with <see cref="EndpointFilterExtensions"/>.
/// </summary>
public class ValidationFilter<TRequest> : IEndpointFilter
{
    private readonly IValidator<TRequest> _validator;

    public ValidationFilter(IValidator<TRequest> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context);
        }

        var result = await _validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}

public static class EndpointFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder, IValidator<TRequest> validator)
    {
        return builder.AddEndpointFilter(new ValidationFilter<TRequest>(validator));
    }
}
