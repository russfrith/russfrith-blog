using FluentValidation;

namespace Shop.VerticalSlice.Features.Products.UpdateProduct;

public class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0);
    }
}
