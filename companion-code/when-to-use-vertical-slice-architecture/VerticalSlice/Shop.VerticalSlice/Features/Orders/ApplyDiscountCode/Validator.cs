using FluentValidation;

namespace Shop.VerticalSlice.Features.Orders.ApplyDiscountCode;

public class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Code).NotEmpty().MaximumLength(32);
    }
}
