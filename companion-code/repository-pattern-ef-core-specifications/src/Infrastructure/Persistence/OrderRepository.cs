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
    //
    // The ORDER BY deliberately lives here, as a LINQ call after Include(),
    // not inside the raw SQL string above. An ORDER BY written inside a
    // FromSqlInterpolated string is not preserved once Include() composes a
    // join on top of it - EF treats the raw SQL as an unordered subquery for
    // that join, so the embedded ordering is silently dropped from the final
    // query. This contract test caught exactly that: rows came back in
    // whatever order the join happened to produce, not the order the SQL
    // text asked for. ThenBy(order => order.Id) is still a real tiebreaker on
    // top of that - the spec path gets one from EF's own Include() handling,
    // so this needs to match it for two orders tied on CreatedAt.
    public async Task<List<Order>> GetOpenOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var query = ComposeWithRawSql($"""
            SELECT * FROM orders
            WHERE customer_id = {customerId} AND status IN (0, 1)
            """);

        return await query.Include(order => order.Lines)
            .OrderByDescending(order => order.CreatedAt).ThenBy(order => order.Id)
            .ToListAsync(cancellationToken);
    }
}
