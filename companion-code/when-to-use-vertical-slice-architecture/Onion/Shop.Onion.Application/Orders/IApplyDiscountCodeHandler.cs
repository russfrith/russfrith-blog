using Shop.Onion.Application.Orders.Dtos;

namespace Shop.Onion.Application.Orders;

public interface IApplyDiscountCodeHandler
{
    Task<ApplyDiscountCodeResult> HandleAsync(ApplyDiscountCodeCommand command, CancellationToken cancellationToken = default);
}
