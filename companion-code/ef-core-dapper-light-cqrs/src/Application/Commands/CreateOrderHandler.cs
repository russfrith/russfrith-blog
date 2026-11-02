using OrderLightCqrs.Domain;

namespace OrderLightCqrs.Application.Commands;

public class CreateOrderHandler : ICreateOrderHandler
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(IOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = Order.Create(command.CustomerId);

        foreach (var line in command.Lines)
        {
            order.AddLine(line.ProductId, line.Quantity, line.UnitPrice);
        }

        _repository.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
