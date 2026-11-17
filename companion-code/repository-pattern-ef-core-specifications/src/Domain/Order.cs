namespace RepositoryPattern.Domain;

public class Order : IAggregateRoot
{
    // The statuses an order is still "in flight" under. Defined once here so
    // both the domain's own checks and query specifications stay in sync.
    public static readonly OrderStatus[] OpenStatuses = [OrderStatus.Draft, OrderStatus.Submitted];

    private readonly List<OrderLine> _lines = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines;

    public bool IsOpen => OpenStatuses.Contains(Status);

    private Order()
    {
        // EF Core materialization.
    }

    public static Order Create(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("An order requires a customer.");

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddLine(string productName, int quantity, decimal unitPrice)
    {
        EnsureDraft();

        _lines.Add(OrderLine.Create(Id, productName, quantity, unitPrice));
    }

    public void Submit()
    {
        EnsureDraft();

        if (_lines.Count == 0)
            throw new DomainException("An order needs at least one line before it can be submitted.");

        Status = OrderStatus.Submitted;
    }

    public void Ship()
    {
        if (Status != OrderStatus.Submitted)
            throw new DomainException("Only a submitted order can ship.");

        Status = OrderStatus.Shipped;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped)
            throw new DomainException("A shipped order cannot be cancelled.");

        Status = OrderStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("An order cannot be modified after it has been submitted.");
    }
}
