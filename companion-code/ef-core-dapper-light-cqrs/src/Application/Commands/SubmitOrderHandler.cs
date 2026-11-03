namespace OrderLightCqrs.Application.Commands;

/// <summary>
/// Submits an order and records an audit row in the same transaction. This is the
/// walkthrough example of Dapper joining an EF Core save: the aggregate change and the
/// audit insert either both commit or both roll back.
/// </summary>
public class SubmitOrderHandler : ISubmitOrderHandler
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderAuditWriter _auditWriter;

    public SubmitOrderHandler(IOrderRepository repository, IUnitOfWork unitOfWork, IOrderAuditWriter auditWriter)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _auditWriter = auditWriter;
    }

    public async Task HandleAsync(SubmitOrderCommand command, CancellationToken cancellationToken = default)
    {
        Domain.Order order = await _repository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new Domain.DomainException($"Order {command.OrderId} was not found.");

        order.Submit();

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _unitOfWork.SaveChangesAsync(ct);
            await _auditWriter.WriteAsync(order.Id, "OrderSubmitted", ct);
        }, cancellationToken);
    }
}
