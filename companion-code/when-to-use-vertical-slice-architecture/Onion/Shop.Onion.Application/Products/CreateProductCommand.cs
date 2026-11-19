namespace Shop.Onion.Application.Products;

public record CreateProductCommand(string Sku, string Name, decimal Price);
