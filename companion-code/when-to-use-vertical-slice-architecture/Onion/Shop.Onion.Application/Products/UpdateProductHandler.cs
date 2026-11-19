using Shop.Onion.Domain;

namespace Shop.Onion.Application.Products;

public class UpdateProductHandler : IUpdateProductHandler
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        Product product = await _repository.GetByIdAsync(command.ProductId, cancellationToken)
            ?? throw new DomainException($"Product {command.ProductId} was not found.");

        product.Rename(command.Name);
        product.Reprice(command.Price);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
