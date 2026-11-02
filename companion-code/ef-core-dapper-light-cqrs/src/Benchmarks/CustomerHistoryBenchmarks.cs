using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderLightCqrs.Application.Dtos;
using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure.Reads;
using OrderLightCqrs.Infrastructure.Write;

namespace OrderLightCqrs.Benchmarks;

/// <summary>
/// Scenario 4 - customer order history export: every order and line for one customer with
/// a large history (thousands of rows). Seed data deliberately gives one customer a heavy
/// history so this scenario reflects a real "export my order history" / support-lookup
/// screen rather than a synthetic worst case.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3, invocationCount: 1)]
public class CustomerHistoryBenchmarks
{
    private Guid _customerId;

    // Joins Set<OrderLine>() directly rather than SelectMany-ing the Lines navigation, and
    // keeps CreatedAtUtc as DateTimeOffset until after materialization. SelectMany over a
    // collection navigation combined with .UtcDateTime in the same projection hits an EF
    // Core 10 translation gap (DateTimeOffset -> nullable DateTime coercion in the shaper);
    // joining the backing DbSet directly avoids it.
    private static readonly Func<AppDbContext, Guid, IAsyncEnumerable<CustomerHistoryRow>> CompiledQuery =
        EF.CompileAsyncQuery((AppDbContext ctx, Guid customerId) =>
            ctx.Orders
                .Where(o => o.CustomerId == customerId)
                .Join(ctx.Set<OrderLine>(), o => o.Id, l => EF.Property<Guid>(l, "OrderId"), (o, l) => new { o, l })
                .Join(ctx.Products, x => x.l.ProductId, p => p.Id, (x, p) => new { x.o, x.l, p })
                .OrderByDescending(x => x.o.CreatedAtUtc)
                .Select(x => new CustomerHistoryRow(
                    x.o.Id,
                    x.o.CreatedAtUtc,
                    x.o.Status.ToString(),
                    x.l.ProductId,
                    x.p.Name,
                    x.l.Quantity,
                    x.l.UnitPrice,
                    x.l.Quantity * x.l.UnitPrice)));

    [GlobalSetup]
    public async Task Setup()
    {
        await using var connection = new NpgsqlConnection(BenchmarkEnvironment.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select customer_id from orders group by customer_id order by count(*) desc limit 1";
        _customerId = (Guid)(await command.ExecuteScalarAsync())!;
    }

    [Benchmark(Baseline = true, Description = "EF tracked entities (Include + second query)")]
    public async Task<List<CustomerOrderHistoryRowDto>> EfTracked()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var orders = await context.Orders
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == _customerId)
            .ToListAsync();

        var productIds = orders.SelectMany(o => o.Lines).Select(l => l.ProductId).Distinct().ToList();
        var productNames = await context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        return orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .SelectMany(o => o.Lines.Select(l => new CustomerOrderHistoryRowDto(
                o.Id, o.CreatedAtUtc.UtcDateTime, o.Status.ToString(), l.ProductId, productNames[l.ProductId], l.Quantity, l.UnitPrice, l.LineTotal)))
            .ToList();
    }

    [Benchmark(Description = "EF AsNoTracking entities (Include + second query)")]
    public async Task<List<CustomerOrderHistoryRowDto>> EfAsNoTracking()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var orders = await context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == _customerId)
            .ToListAsync();

        var productIds = orders.SelectMany(o => o.Lines).Select(l => l.ProductId).Distinct().ToList();
        var productNames = await context.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        return orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .SelectMany(o => o.Lines.Select(l => new CustomerOrderHistoryRowDto(
                o.Id, o.CreatedAtUtc.UtcDateTime, o.Status.ToString(), l.ProductId, productNames[l.ProductId], l.Quantity, l.UnitPrice, l.LineTotal)))
            .ToList();
    }

    [Benchmark(Description = "EF AsNoTracking + Select projection (single query)")]
    public async Task<List<CustomerOrderHistoryRowDto>> EfProjection()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var rows = await context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == _customerId)
            .Join(context.Set<OrderLine>(), o => o.Id, l => EF.Property<Guid>(l, "OrderId"), (o, l) => new { o, l })
            .Join(context.Products, x => x.l.ProductId, p => p.Id, (x, p) => new { x.o, x.l, p })
            .OrderByDescending(x => x.o.CreatedAtUtc)
            .Select(x => new CustomerHistoryRow(
                x.o.Id,
                x.o.CreatedAtUtc,
                x.o.Status.ToString(),
                x.l.ProductId,
                x.p.Name,
                x.l.Quantity,
                x.l.UnitPrice,
                x.l.Quantity * x.l.UnitPrice))
            .ToListAsync();

        return rows.Select(r => r.ToDto()).ToList();
    }

    [Benchmark(Description = "EF compiled query + projection (single query)")]
    public async Task<List<CustomerOrderHistoryRowDto>> EfCompiledProjection()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var rows = new List<CustomerOrderHistoryRowDto>();
        await foreach (var row in CompiledQuery(context, _customerId))
        {
            rows.Add(row.ToDto());
        }

        return rows;
    }

    [Benchmark(Description = "Dapper (single query)")]
    public async Task<IReadOnlyList<CustomerOrderHistoryRowDto>> Dapper()
    {
        var queries = new OrderQueries(BenchmarkEnvironment.CreateConnectionFactory());
        return await queries.GetCustomerOrderHistoryAsync(_customerId);
    }

    private record CustomerHistoryRow(Guid OrderId, DateTimeOffset CreatedAtUtc, string Status, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal)
    {
        public CustomerOrderHistoryRowDto ToDto() => new(OrderId, CreatedAtUtc.UtcDateTime, Status, ProductId, ProductName, Quantity, UnitPrice, LineTotal);
    }
}
