using Shop.Onion.Application.Orders.Dtos;
using Shop.Onion.Domain;

namespace Shop.Onion.Application.Orders;

public class ApplyDiscountCodeHandler : IApplyDiscountCodeHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IDiscountCodeRepository _discountCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ApplyDiscountCodeHandler(
        IOrderRepository orderRepository,
        IDiscountCodeRepository discountCodeRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _orderRepository = orderRepository;
        _discountCodeRepository = discountCodeRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ApplyDiscountCodeResult> HandleAsync(ApplyDiscountCodeCommand command, CancellationToken cancellationToken = default)
    {
        Order order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new DomainException($"Order {command.OrderId} was not found.");

        DiscountCode discountCode = await _discountCodeRepository.GetByCodeAsync(command.Code, cancellationToken)
            ?? throw new DomainException($"Discount code {command.Code} was not found.");

        order.ApplyDiscountCode(discountCode, _timeProvider.GetUtcNow());
        discountCode.Redeem();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApplyDiscountCodeResult(order.Subtotal, order.DiscountTotal, order.Total);
    }
}
