namespace OrderLightCqrs.Domain;

public enum OrderStatus
{
    Draft = 0,
    Submitted = 1,
    Shipped = 2,
    Cancelled = 3
}
