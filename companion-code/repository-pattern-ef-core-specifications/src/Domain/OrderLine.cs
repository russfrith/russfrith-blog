namespace RepositoryPattern.Domain;

public class OrderLine
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private OrderLine()
    {
        // EF Core materialization.
    }

    internal static OrderLine Create(Guid orderId, string productName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("An order line requires a product name.");

        if (quantity <= 0)
            throw new DomainException("An order line's quantity must be positive.");

        if (unitPrice < 0)
            throw new DomainException("An order line's unit price cannot be negative.");

        return new OrderLine
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
