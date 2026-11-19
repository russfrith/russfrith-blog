using Microsoft.EntityFrameworkCore;
using Shop.VerticalSlice.Common;
using Shop.VerticalSlice.Data;

namespace Shop.VerticalSlice.Features.Orders.ApplyDiscountCode;

public class Handler
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public Handler(AppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<Response> HandleAsync(Guid orderId, Request request, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Lines)
            .Include(o => o.AppliedDiscounts)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new DomainException($"Order {orderId} was not found.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var discountCode = await _context.DiscountCodes.FirstOrDefaultAsync(d => d.Code == normalizedCode, cancellationToken)
            ?? throw new DomainException($"Discount code {request.Code} was not found.");

        var amount = DiscountRules.Apply(order, discountCode, _timeProvider.GetUtcNow());

        order.AppliedDiscounts.Add(new AppliedDiscountEntity
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            DiscountCodeId = discountCode.Id,
            Code = discountCode.Code,
            IsStackable = discountCode.IsStackable,
            Amount = amount
        });
        discountCode.RedemptionCount++;

        await _context.SaveChangesAsync(cancellationToken);

        var subtotal = order.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var discountTotal = order.AppliedDiscounts.Sum(d => d.Amount);

        return new Response(subtotal, discountTotal, subtotal - discountTotal);
    }
}
