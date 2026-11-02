namespace OrderLightCqrs.Infrastructure.Reference;

/// <summary>
/// Plain reference data, not a domain aggregate. Customers aren't modified through this
/// app (they'd belong to their own bounded context in a real system); here they exist
/// only so Orders have a customer to reference and the read side has a name to join to.
/// </summary>
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
