namespace Shop.VerticalSlice.Data;

public class ProductEntity
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public enum OrderStatus
{
    Draft = 0,
    Submitted = 1
}

public class OrderEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public List<OrderLineEntity> Lines { get; set; } = new();
    public List<AppliedDiscountEntity> AppliedDiscounts { get; set; } = new();
}

public class OrderLineEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

public class DiscountCodeEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public bool IsStackable { get; set; }
    public int MaxRedemptions { get; set; }
    public int RedemptionCount { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public class AppliedDiscountEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid DiscountCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsStackable { get; set; }
    public decimal Amount { get; set; }
}
