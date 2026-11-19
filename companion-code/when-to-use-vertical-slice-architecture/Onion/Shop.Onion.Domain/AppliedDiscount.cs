namespace Shop.Onion.Domain;

public class AppliedDiscount
{
    public Guid DiscountCodeId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public bool IsStackable { get; private set; }
    public decimal Amount { get; private set; }

    private AppliedDiscount()
    {
    }

    internal AppliedDiscount(Guid discountCodeId, string code, bool isStackable, decimal amount)
    {
        DiscountCodeId = discountCodeId;
        Code = code;
        IsStackable = isStackable;
        Amount = amount;
    }
}
