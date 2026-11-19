using Shop.VerticalSlice.Data;

namespace Shop.VerticalSlice.Features.Orders.CreateOrder;

public class Handler
{
    private readonly AppDbContext _context;

    public Handler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> HandleAsync(Request request, CancellationToken cancellationToken = default)
    {
        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Lines = request.Lines.Select(l => new OrderLineEntity
            {
                Id = Guid.NewGuid(),
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
