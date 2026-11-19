namespace Shop.Onion.Application.Products.Dtos;

public record ProductDto(Guid Id, string Sku, string Name, decimal Price);
