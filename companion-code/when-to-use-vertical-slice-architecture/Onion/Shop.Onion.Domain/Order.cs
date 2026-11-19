namespace Shop.Onion.Domain;

public class Order
{
    private readonly List<OrderLine> _lines = new();
    private readonly List<AppliedDiscount> _appliedDiscounts = new();

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<AppliedDiscount> AppliedDiscounts => _appliedDiscounts.AsReadOnly();

    public decimal Subtotal => _lines.Sum(l => l.LineTotal);
    public decimal DiscountTotal => _appliedDiscounts.Sum(d => d.Amount);
    public decimal Total => Subtotal - DiscountTotal;

    private Order()
    {
    }

    public static Order Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("An order must belong to a customer.");
        }

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Draft
        };
    }

    public void AddLine(Guid productId, int quantity, decimal unitPrice)
    {
        EnsureModifiable();
        _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }

    /// <summary>
    /// Applies a discount code, enforcing eligibility, expiry, usage limits, and stacking rules.
    /// Does not mutate the code's redemption count; the caller persists that separately once the
    /// order itself is saved, so a failed save doesn't burn a redemption.
    /// </summary>
    public void ApplyDiscountCode(DiscountCode discountCode, DateTimeOffset nowUtc)
    {
        EnsureModifiable();

        if (_appliedDiscounts.Any(d => d.DiscountCodeId == discountCode.Id))
        {
            throw new DomainException($"Discount code {discountCode.Code} has already been applied to this order.");
        }

        if (discountCode.IsExpired(nowUtc))
        {
            throw new DomainException($"Discount code {discountCode.Code} expired on {discountCode.ExpiresAtUtc:u}.");
        }

        if (!discountCode.HasRedemptionsRemaining())
        {
            throw new DomainException($"Discount code {discountCode.Code} has reached its redemption limit.");
        }

        if (Subtotal < discountCode.MinimumOrderAmount)
        {
            throw new DomainException(
                $"Discount code {discountCode.Code} requires a minimum order of {discountCode.MinimumOrderAmount:C}.");
        }

        if (!discountCode.IsStackable && _appliedDiscounts.Count > 0)
        {
            throw new DomainException($"Discount code {discountCode.Code} cannot be combined with another discount.");
        }

        if (_appliedDiscounts.Any(d => !d.IsStackable))
        {
            throw new DomainException("This order already has a non-stackable discount applied.");
        }

        var amount = discountCode.CalculateDiscount(Subtotal);
        _appliedDiscounts.Add(new AppliedDiscount(discountCode.Id, discountCode.Code, discountCode.IsStackable, amount));
    }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainException($"Only draft orders can be submitted. Order {Id} is {Status}.");
        }

        if (_lines.Count == 0)
        {
            throw new DomainException("An order must have at least one line before it can be submitted.");
        }

        Status = OrderStatus.Submitted;
    }

    private void EnsureModifiable()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainException($"Order {Id} cannot be modified once it has been submitted ({Status}).");
        }
    }
}
