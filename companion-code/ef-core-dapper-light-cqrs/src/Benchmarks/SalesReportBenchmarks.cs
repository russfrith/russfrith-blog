using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using OrderLightCqrs.Application.Dtos;
using OrderLightCqrs.Domain;
using OrderLightCqrs.Infrastructure.Reads;
using OrderLightCqrs.Infrastructure.Write;

namespace OrderLightCqrs.Benchmarks;

/// <summary>
/// Scenario 3 - sales report: revenue by product category and month over the last 12
/// months. Heavy aggregation across the whole 120k-order dataset - the scenario where
/// fetching and summing in .NET versus letting Postgres aggregate should matter most.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3, invocationCount: 1)]
public class SalesReportBenchmarks
{
    private DateTimeOffset _fromUtc;
    private DateTimeOffset _toUtc;

    private static readonly Func<AppDbContext, DateTimeOffset, DateTimeOffset, IAsyncEnumerable<SalesReportRowDto>> CompiledQuery =
        EF.CompileAsyncQuery((AppDbContext ctx, DateTimeOffset fromUtc, DateTimeOffset toUtc) =>
            ctx.Set<OrderLine>()
                .Join(ctx.Orders, l => EF.Property<Guid>(l, "OrderId"), o => o.Id, (l, o) => new { l, o })
                .Where(x => x.o.CreatedAtUtc >= fromUtc && x.o.CreatedAtUtc < toUtc && x.o.Status != OrderStatus.Cancelled)
                .Join(ctx.Products, x => x.l.ProductId, p => p.Id, (x, p) => new { x.l, x.o, p })
                .GroupBy(x => new { x.p.Category, x.o.CreatedAtUtc.Year, x.o.CreatedAtUtc.Month })
                .Select(g => new SalesReportRowDto(
                    g.Key.Category,
                    g.Key.Year,
                    g.Key.Month,
                    g.Sum(x => x.l.Quantity * x.l.UnitPrice),
                    g.Select(x => x.o.Id).Distinct().Count())));

    [GlobalSetup]
    public void Setup()
    {
        _toUtc = DateTimeOffset.UtcNow;
        _fromUtc = _toUtc.AddMonths(-12);
    }

    [Benchmark(Baseline = true, Description = "EF tracked entities, grouped in memory")]
    public async Task<List<SalesReportRowDto>> EfTracked()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var orders = await context.Orders
            .Include(o => o.Lines)
            .Where(o => o.CreatedAtUtc >= _fromUtc && o.CreatedAtUtc < _toUtc && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        var productIds = orders.SelectMany(o => o.Lines).Select(l => l.ProductId).Distinct().ToList();
        var categories = await context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Category);

        var rows = orders
            .SelectMany(o => o.Lines.Select(l => (o.CreatedAtUtc, Category: categories[l.ProductId], OrderId: o.Id, l.Quantity, l.UnitPrice)))
            .GroupBy(x => (x.Category, x.CreatedAtUtc.Year, x.CreatedAtUtc.Month))
            .Select(g => new SalesReportRowDto(g.Key.Category, g.Key.Year, g.Key.Month, g.Sum(x => x.Quantity * x.UnitPrice), g.Select(x => x.OrderId).Distinct().Count()))
            .ToList();

        return rows;
    }

    [Benchmark(Description = "EF AsNoTracking entities, grouped in memory")]
    public async Task<List<SalesReportRowDto>> EfAsNoTracking()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var orders = await context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.CreatedAtUtc >= _fromUtc && o.CreatedAtUtc < _toUtc && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        var productIds = orders.SelectMany(o => o.Lines).Select(l => l.ProductId).Distinct().ToList();
        var categories = await context.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Category);

        var rows = orders
            .SelectMany(o => o.Lines.Select(l => (o.CreatedAtUtc, Category: categories[l.ProductId], OrderId: o.Id, l.Quantity, l.UnitPrice)))
            .GroupBy(x => (x.Category, x.CreatedAtUtc.Year, x.CreatedAtUtc.Month))
            .Select(g => new SalesReportRowDto(g.Key.Category, g.Key.Year, g.Key.Month, g.Sum(x => x.Quantity * x.UnitPrice), g.Select(x => x.OrderId).Distinct().Count()))
            .ToList();

        return rows;
    }

    [Benchmark(Description = "EF AsNoTracking + Select projection (GroupBy translated to SQL)")]
    public async Task<List<SalesReportRowDto>> EfProjection()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        return await context.Set<OrderLine>()
            .AsNoTracking()
            .Join(context.Orders, l => EF.Property<Guid>(l, "OrderId"), o => o.Id, (l, o) => new { l, o })
            .Where(x => x.o.CreatedAtUtc >= _fromUtc && x.o.CreatedAtUtc < _toUtc && x.o.Status != OrderStatus.Cancelled)
            .Join(context.Products, x => x.l.ProductId, p => p.Id, (x, p) => new { x.l, x.o, p })
            .GroupBy(x => new { x.p.Category, x.o.CreatedAtUtc.Year, x.o.CreatedAtUtc.Month })
            .Select(g => new SalesReportRowDto(
                g.Key.Category,
                g.Key.Year,
                g.Key.Month,
                g.Sum(x => x.l.Quantity * x.l.UnitPrice),
                g.Select(x => x.o.Id).Distinct().Count()))
            .ToListAsync();
    }

    [Benchmark(Description = "EF compiled query + projection (GroupBy translated to SQL)")]
    public async Task<List<SalesReportRowDto>> EfCompiledProjection()
    {
        await using var context = BenchmarkEnvironment.CreateContext();

        var rows = new List<SalesReportRowDto>();
        await foreach (var row in CompiledQuery(context, _fromUtc, _toUtc))
        {
            rows.Add(row);
        }

        return rows;
    }

    [Benchmark(Description = "Dapper (single aggregate query)")]
    public async Task<IReadOnlyList<SalesReportRowDto>> Dapper()
    {
        var queries = new OrderQueries(BenchmarkEnvironment.CreateConnectionFactory());
        return await queries.GetSalesReportAsync(_fromUtc, _toUtc);
    }
}
