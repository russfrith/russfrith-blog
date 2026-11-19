namespace Shop.Onion.Application.Products;

public record UpdateProductCommand(Guid ProductId, string Name, decimal Price);
