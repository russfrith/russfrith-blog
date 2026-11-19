namespace Shop.Onion.Domain;

public class DiscountCode
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public DiscountType Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal MinimumOrderAmount { get; private set; }
    public bool IsStackable { get; private set; }
    public int MaxRedemptions { get; private set; }
    public int RedemptionCount { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    private DiscountCode()
    {
    }

    public static DiscountCode Create(
        string code,
        DiscountType type,
        decimal value,
        decimal minimumOrderAmount,
        bool isStackable,
        int maxRedemptions,
        DateTimeOffset expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("A discount code must have a code.");
        }

        if (type == DiscountType.Percentage && (value <= 0 || value > 100))
        {
            throw new DomainException("A percentage discount must be between 0 and 100.");
        }

        if (type == DiscountType.FixedAmount && value <= 0)
        {
            throw new DomainException("A fixed-amount discount must be greater than zero.");
        }

        if (minimumOrderAmount < 0)
        {
            throw new DomainException("Minimum order amount cannot be negative.");
        }

        if (maxRedemptions <= 0)
        {
            throw new DomainException("Max redemptions must be greater than zero.");
        }

        return new DiscountCode
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Type = type,
            Value = value,
            MinimumOrderAmount = minimumOrderAmount,
            IsStackable = isStackable,
            MaxRedemptions = maxRedemptions,
            RedemptionCount = 0,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool HasRedemptionsRemaining() => RedemptionCount < MaxRedemptions;

    public decimal CalculateDiscount(decimal subtotal)
    {
        return Type switch
        {
            DiscountType.Percentage => Math.Round(subtotal * (Value / 100m), 2, MidpointRounding.AwayFromZero),
            DiscountType.FixedAmount => Math.Min(Value, subtotal),
            _ => throw new DomainException($"Unknown discount type: {Type}.")
        };
    }

    public void Redeem()
    {
        if (!HasRedemptionsRemaining())
        {
            throw new DomainException($"Discount code {Code} has reached its redemption limit.");
        }

        RedemptionCount++;
    }
}
