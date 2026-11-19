using Shop.Onion.Domain;

namespace Shop.Onion.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidValues_Succeeds()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m);

        Assert.Equal("SKU-1", product.Sku);
        Assert.Equal("Widget", product.Name);
        Assert.Equal(9.99m, product.Price);
    }

    [Theory]
    [InlineData("", "Widget", 9.99)]
    [InlineData("SKU-1", "", 9.99)]
    public void Create_WithMissingRequiredField_Throws(string sku, string name, decimal price)
    {
        Assert.Throws<DomainException>(() => Product.Create(sku, name, price));
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        Assert.Throws<DomainException>(() => Product.Create("SKU-1", "Widget", -1m));
    }

    [Fact]
    public void Reprice_WithNegativeValue_Throws()
    {
        var product = Product.Create("SKU-1", "Widget", 9.99m);

        Assert.Throws<DomainException>(() => product.Reprice(-1m));
    }
}
