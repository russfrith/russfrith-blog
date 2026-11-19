using Shop.Onion.Domain;

namespace Shop.Onion.Application.Products;

public class CreateProductHandler : ICreateProductHandler
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        if (await _repository.SkuExistsAsync(command.Sku, cancellationToken))
        {
            throw new DomainException($"A product with SKU {command.Sku} already exists.");
        }

        var product = Product.Create(command.Sku, command.Name, command.Price);
        _repository.Add(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
