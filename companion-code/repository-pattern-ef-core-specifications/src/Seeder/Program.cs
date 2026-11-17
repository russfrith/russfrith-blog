using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RepositoryPattern.Infrastructure;
using RepositoryPattern.Infrastructure.Persistence;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Orders")
    ?? "Host=localhost;Database=repository_pattern;Username=postgres;Password=postgres";

var orderCount = int.TryParse(Environment.GetEnvironmentVariable("SEED_ORDER_COUNT"), out var parsed) ? parsed : 50_000;

Console.WriteLine($"Applying migrations to {MaskPassword(connectionString)}...");

var services = new ServiceCollection();
services.AddInfrastructure(connectionString);
using var provider = services.BuildServiceProvider();
using (var scope = provider.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Database.MigrateAsync();
}

Console.WriteLine($"Seeding {orderCount:N0} orders...");
var stopwatch = Stopwatch.StartNew();

// Seeded directly via Npgsql's binary COPY rather than through the domain
// model: at this volume, going through Order.Create()/AddLine() and EF's
// change tracker would make seeding the slowest part of `docker compose up`.
// This is a seed script, not a code path the post defends - the aggregate's
// invariants are upheld by construction below (every row has 1-3 lines,
// non-negative quantities and prices), just not enforced by Order itself.
var random = new Random(20261114);
const int customerCount = 2_000;
var customerIds = Enumerable.Range(0, customerCount).Select(_ => Guid.NewGuid()).ToArray();
var statuses = new[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3 }; // Draft, Submitted, Shipped, Cancelled weighted 3:3:3:1
var productNames = new[] { "Widget", "Gadget", "Gizmo", "Doohickey", "Thingamajig", "Contraption" };

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

var orderRows = new List<(Guid Id, Guid CustomerId, int Status, DateTimeOffset CreatedAt)>(orderCount);
var now = DateTimeOffset.UtcNow;

for (var i = 0; i < orderCount; i++)
{
    var createdAt = now.AddSeconds(-random.Next(0, 180 * 24 * 60 * 60));
    orderRows.Add((Guid.NewGuid(), customerIds[random.Next(customerCount)], statuses[random.Next(statuses.Length)], createdAt));
}

await using (var orderWriter = await connection.BeginBinaryImportAsync(
    "COPY orders (id, customer_id, status, created_at) FROM STDIN (FORMAT BINARY)"))
{
    foreach (var order in orderRows)
    {
        await orderWriter.StartRowAsync();
        await orderWriter.WriteAsync(order.Id, NpgsqlTypes.NpgsqlDbType.Uuid);
        await orderWriter.WriteAsync(order.CustomerId, NpgsqlTypes.NpgsqlDbType.Uuid);
        await orderWriter.WriteAsync(order.Status, NpgsqlTypes.NpgsqlDbType.Integer);
        await orderWriter.WriteAsync(order.CreatedAt, NpgsqlTypes.NpgsqlDbType.TimestampTz);
    }

    await orderWriter.CompleteAsync();
}

await using (var lineWriter = await connection.BeginBinaryImportAsync(
    "COPY order_lines (id, order_id, product_name, quantity, unit_price) FROM STDIN (FORMAT BINARY)"))
{
    foreach (var order in orderRows)
    {
        var lineCount = random.Next(1, 4);
        for (var l = 0; l < lineCount; l++)
        {
            await lineWriter.StartRowAsync();
            await lineWriter.WriteAsync(Guid.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid);
            await lineWriter.WriteAsync(order.Id, NpgsqlTypes.NpgsqlDbType.Uuid);
            await lineWriter.WriteAsync(productNames[random.Next(productNames.Length)], NpgsqlTypes.NpgsqlDbType.Text);
            await lineWriter.WriteAsync(random.Next(1, 6), NpgsqlTypes.NpgsqlDbType.Integer);
            await lineWriter.WriteAsync(Math.Round((decimal)(random.NextDouble() * 199 + 1), 2), NpgsqlTypes.NpgsqlDbType.Numeric);
        }
    }

    await lineWriter.CompleteAsync();
}

stopwatch.Stop();
Console.WriteLine($"Seeded {orderCount:N0} orders across {customerCount:N0} customers in {stopwatch.Elapsed.TotalSeconds:N1}s.");

static string MaskPassword(string connectionString)
    => string.Join(';', connectionString.Split(';').Select(part =>
        part.TrimStart().StartsWith("Password", StringComparison.OrdinalIgnoreCase) ? "Password=***" : part));
