using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderLightCqrs.Application.Dtos;
using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure.Reads;
using OrderLightCqrs.Infrastructure.Write;

namespace OrderLightCqrs.Benchmarks;

/// <summary>
/// Scenario 2 - order detail view: one order with its lines and product names. Small
/// result, but called on every "view order" click, so per-call overhead - not throughput
/// on a big scan - is what matters. This is the scenario where EF and Dapper should be
/// closest, since there's barely anything to optimize away.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class OrderDetailBenchmarks
{
    private Guid _orderId;

    private static readonly Func<AppDbContext, Guid, Task<OrderDetailHeader?>> CompiledHeaderQuery =
        EF.CompileAsyncQuery((AppDbContext ctx, Guid orderId) =>
            ctx.Orders
                .Where(o => o.Id == orderId)
                .Select(o => new OrderDetailHeader(
                    o.Id,
                    o.CustomerId,
                    ctx.Customers.Where(c => c.Id == o.CustomerId).Select(c => c.Name).First(),
                    o.Status.ToString(),
                    o.CreatedAtUtc,
                    o.SubmittedAtUtc))
                .FirstOrDefault());

    private static readonly Func<AppDbContext, Guid, IAsyncEnumerable<OrderDetailLineDto>> CompiledLinesQuery =
        EF.CompileAsyncQuery((AppDbContext ctx, Guid orderId) =>
            ctx.Set<OrderLine>()
                .Where(l => EF.Property<Guid>(l, "OrderId") == orderId)
                .Select(l => new OrderDetailLineDto(
                    l.ProductId,
                    ctx.Products.Where(p => p.Id == l.ProductId).Select(p => p.Name).First(),
                    l.Quantity,
                    l.UnitPrice,
                    l.Quantity * l.UnitPrice)));

    [GlobalSetup]
    public async Task Setup()
    {
        await using var connection = new NpgsqlConnection(BenchmarkEnvironment.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select id from orders order by created_at_utc desc limit 1 offset 1000";
        _orderId = (Guid)(await command.ExecuteScalarAsync())!;
    }

    [Benchmark(Baseline = true, Description = "EF tracked entities (Include + second query)")]
    public async Task<OrderDetailDto?> EfTracked()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var order = await context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == _orderId);

        if (order is null)
        {
            return null;
        }

        var customerName = await context.Customers.Where(c => c.Id == order.CustomerId).Select(c => c.Name).FirstAsync();
        var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productNames = await context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        var lines = order.Lines
            .Select(l => new OrderDetailLineDto(l.ProductId, productNames[l.ProductId], l.Quantity, l.UnitPrice, l.LineTotal))
            .ToList();

        return new OrderDetailDto(order.Id, order.CustomerId, customerName, order.Status.ToString(), order.CreatedAtUtc.UtcDateTime, order.SubmittedAtUtc?.UtcDateTime, lines.Sum(l => l.LineTotal), lines);
    }

    [Benchmark(Description = "EF AsNoTracking entities (Include + second query)")]
    public async Task<OrderDetailDto?> EfAsNoTracking()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == _orderId);

        if (order is null)
        {
            return null;
        }

        var customerName = await context.Customers.AsNoTracking().Where(c => c.Id == order.CustomerId).Select(c => c.Name).FirstAsync();
        var productIds = order.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productNames = await context.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);

        var lines = order.Lines
            .Select(l => new OrderDetailLineDto(l.ProductId, productNames[l.ProductId], l.Quantity, l.UnitPrice, l.LineTotal))
            .ToList();

        return new OrderDetailDto(order.Id, order.CustomerId, customerName, order.Status.ToString(), order.CreatedAtUtc.UtcDateTime, order.SubmittedAtUtc?.UtcDateTime, lines.Sum(l => l.LineTotal), lines);
    }

    [Benchmark(Description = "EF AsNoTracking + Select projection (two queries)")]
    public async Task<OrderDetailDto?> EfProjection()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var header = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == _orderId)
            .Select(o => new OrderDetailHeader(
                o.Id,
                o.CustomerId,
                context.Customers.Where(c => c.Id == o.CustomerId).Select(c => c.Name).First(),
                o.Status.ToString(),
                o.CreatedAtUtc,
                o.SubmittedAtUtc))
            .FirstOrDefaultAsync();

        if (header is null)
        {
            return null;
        }

        var lines = await context.Set<OrderLine>()
            .AsNoTracking()
            .Where(l => EF.Property<Guid>(l, "OrderId") == _orderId)
            .Select(l => new OrderDetailLineDto(
                l.ProductId,
                context.Products.Where(p => p.Id == l.ProductId).Select(p => p.Name).First(),
                l.Quantity,
                l.UnitPrice,
                l.Quantity * l.UnitPrice))
            .ToListAsync();

        return header.ToDto(lines);
    }

    [Benchmark(Description = "EF compiled query + projection (two queries)")]
    public async Task<OrderDetailDto?> EfCompiledProjection()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var header = await CompiledHeaderQuery(context, _orderId);
        if (header is null)
        {
            return null;
        }

        var lines = new List<OrderDetailLineDto>();
        await foreach (var line in CompiledLinesQuery(context, _orderId))
        {
            lines.Add(line);
        }

        return header.ToDto(lines);
    }

    [Benchmark(Description = "Dapper (QueryMultiple, two result sets)")]
    public async Task<OrderDetailDto?> Dapper()
    {
        var queries = new OrderQueries(BenchmarkEnvironment.CreateConnectionFactory());
        return await queries.GetOrderDetailAsync(_orderId);
    }

    private record OrderDetailHeader(Guid OrderId, Guid CustomerId, string CustomerName, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? SubmittedAtUtc)
    {
        public OrderDetailDto ToDto(List<OrderDetailLineDto> lines) => new(
            OrderId, CustomerId, CustomerName, Status, CreatedAtUtc.UtcDateTime, SubmittedAtUtc?.UtcDateTime, lines.Sum(l => l.LineTotal), lines);
    }
}
