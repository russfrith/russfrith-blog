namespace OrderLightCqrs.Domain;

public class OrderLine
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private OrderLine()
    {
    }

    internal OrderLine(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Order line quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Order line unit price cannot be negative.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Order line quantity must be greater than zero.");
        }

        Quantity = quantity;
    }
}
