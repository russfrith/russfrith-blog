using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Infrastructure.Persistence;

internal sealed class OrderRepository(OrderingDbContext dbContext) : EfRepository<Order>(dbContext), IOrderRepository
{
    // The tuned path: hand-written SQL instead of the two-table join EF would
    // otherwise generate from the spec's Include(). OpenOrdersForCustomer (the
    // spec) is this method's behavioral definition - the contract tests assert
    // the two return identical rows in identical order.
    public async Task<List<Order>> GetOpenOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var query = ComposeWithRawSql($"""
            SELECT * FROM orders
            WHERE customer_id = {customerId} AND status IN (0, 1)
            ORDER BY created_at DESC
            """);

        return await query.Include(order => order.Lines).ToListAsync(cancellationToken);
    }
}
