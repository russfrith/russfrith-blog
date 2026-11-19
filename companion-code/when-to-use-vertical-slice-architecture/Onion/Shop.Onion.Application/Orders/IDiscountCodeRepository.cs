using Shop.Onion.Domain;

namespace Shop.Onion.Application.Orders;

public interface IDiscountCodeRepository
{
    Task<DiscountCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
