namespace OrderLightCqrs.Domain;

public class Order
{
    private readonly List<OrderLine> _lines = new();

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(l => l.LineTotal);

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
            Status = OrderStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void AddLine(Guid productId, int quantity, decimal unitPrice)
    {
        EnsureModifiable();
        _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }

    public void ChangeLineQuantity(Guid orderLineId, int quantity)
    {
        EnsureModifiable();

        var line = _lines.FirstOrDefault(l => l.Id == orderLineId)
            ?? throw new DomainException($"Order line {orderLineId} was not found on order {Id}.");

        line.ChangeQuantity(quantity);
    }

    public void RemoveLine(Guid orderLineId)
    {
        EnsureModifiable();

        var line = _lines.FirstOrDefault(l => l.Id == orderLineId)
            ?? throw new DomainException($"Order line {orderLineId} was not found on order {Id}.");

        _lines.Remove(line);
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
        SubmittedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped)
        {
            throw new DomainException($"Order {Id} has already shipped and cannot be cancelled.");
        }

        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException($"Order {Id} is already cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }

    public void MarkShipped()
    {
        if (Status != OrderStatus.Submitted)
        {
            throw new DomainException($"Only submitted orders can ship. Order {Id} is {Status}.");
        }

        Status = OrderStatus.Shipped;
    }

    private void EnsureModifiable()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainException($"Order {Id} cannot be modified once it has been submitted ({Status}).");
        }
    }
}
