using Shop.Onion.Domain;

namespace Shop.Onion.Application.Products;

public class DeleteProductHandler : IDeleteProductHandler
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken = default)
    {
        Product product = await _repository.GetByIdAsync(command.ProductId, cancellationToken)
            ?? throw new DomainException($"Product {command.ProductId} was not found.");

        _repository.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
