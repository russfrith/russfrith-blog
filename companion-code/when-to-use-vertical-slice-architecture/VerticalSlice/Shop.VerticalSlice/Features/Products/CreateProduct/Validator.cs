using FluentValidation;

namespace Shop.VerticalSlice.Features.Products.CreateProduct;

public class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Sku).NotEmpty().MaximumLength(64);
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0);
    }
}
