namespace Shop.Onion.Domain;

public class Product
{
    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    private Product()
    {
    }

    public static Product Create(string sku, string name, decimal price)
    {
        ValidateSku(sku);
        ValidateName(name);
        ValidatePrice(price);

        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = name,
            Price = price
        };
    }

    public void Rename(string name)
    {
        ValidateName(name);
        Name = name;
    }

    public void Reprice(decimal price)
    {
        ValidatePrice(price);
        Price = price;
    }

    private static void ValidateSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("A product must have a SKU.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A product must have a name.");
        }
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainException("A product price cannot be negative.");
        }
    }
}
