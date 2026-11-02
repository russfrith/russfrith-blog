using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderLightCqrs.Infrastructure.Write;

var connectionString = Environment.GetEnvironmentVariable("ORDERS_DB_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=orders;Username=postgres;Password=postgres";

var customerCount = int.Parse(Environment.GetEnvironmentVariable("SEED_CUSTOMER_COUNT") ?? "3000");
var productCount = int.Parse(Environment.GetEnvironmentVariable("SEED_PRODUCT_COUNT") ?? "400");
var orderCount = int.Parse(Environment.GetEnvironmentVariable("SEED_ORDER_COUNT") ?? "120000");

Console.WriteLine($"Migrating and seeding {orderCount:N0} orders for {customerCount:N0} customers / {productCount:N0} products.");

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();

await using (var context = new AppDbContext(optionsBuilder.Options))
{
    Console.WriteLine("Applying EF Core migrations...");
    await context.Database.MigrateAsync();
}

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

var existingOrders = await new NpgsqlCommand("select count(*) from orders", connection).ExecuteScalarAsync();
if (existingOrders is long count && count > 0)
{
    Console.WriteLine($"orders already has {count:N0} rows, skipping seed.");
    return;
}

var stopwatch = Stopwatch.StartNew();
var random = new Random(20261102);

string[] categories =
[
    "Electronics", "Home & Garden", "Sporting Goods", "Books",
    "Toys & Games", "Apparel", "Grocery", "Office Supplies"
];

var productNamePrefixes = new[] { "Pro", "Ultra", "Classic", "Compact", "Deluxe", "Essential", "Everyday", "Studio" };
var productNameNouns = new[] { "Widget", "Kit", "Set", "Bundle", "Device", "Organizer", "Unit", "Pack" };

// --- Customers ---
Console.WriteLine("Seeding customers...");
var customerIds = new Guid[customerCount];
await using (var writer = await connection.BeginBinaryImportAsync("copy customers (id, name) from stdin (format binary)"))
{
    for (var i = 0; i < customerCount; i++)
    {
        var id = Guid.NewGuid();
        customerIds[i] = id;
        await writer.StartRowAsync();
        await writer.WriteAsync(id, NpgsqlTypes.NpgsqlDbType.Uuid);
        await writer.WriteAsync($"Customer {i + 1:D5}", NpgsqlTypes.NpgsqlDbType.Varchar);
    }

    await writer.CompleteAsync();
}

// --- Products ---
Console.WriteLine("Seeding products...");
var productIds = new Guid[productCount];
var productCategories = new string[productCount];
var productBasePrices = new decimal[productCount];
await using (var writer = await connection.BeginBinaryImportAsync("copy products (id, name, category) from stdin (format binary)"))
{
    for (var i = 0; i < productCount; i++)
    {
        var id = Guid.NewGuid();
        var category = categories[random.Next(categories.Length)];
        var name = $"{productNamePrefixes[random.Next(productNamePrefixes.Length)]} {productNameNouns[random.Next(productNameNouns.Length)]} {i + 1:D4}";

        productIds[i] = id;
        productCategories[i] = category;
        productBasePrices[i] = Math.Round((decimal)(random.NextDouble() * 495 + 5), 2);

        await writer.StartRowAsync();
        await writer.WriteAsync(id, NpgsqlTypes.NpgsqlDbType.Uuid);
        await writer.WriteAsync(name, NpgsqlTypes.NpgsqlDbType.Varchar);
        await writer.WriteAsync(category, NpgsqlTypes.NpgsqlDbType.Varchar);
    }

    await writer.CompleteAsync();
}

// --- Orders + order lines ---
// Customer order counts follow a Zipf-like skew: a handful of repeat customers place many
// orders, most customers place only a few. This matters for the customer-order-history
// benchmark scenario, which specifically targets a customer with a large history.
Console.WriteLine("Building customer weight distribution...");
var weights = new double[customerCount];
double weightTotal = 0;
for (var i = 0; i < customerCount; i++)
{
    weights[i] = 1.0 / Math.Pow(i + 1, 0.9);
    weightTotal += weights[i];
}

var cumulative = new double[customerCount];
double running = 0;
for (var i = 0; i < customerCount; i++)
{
    running += weights[i] / weightTotal;
    cumulative[i] = running;
}

Guid PickCustomer()
{
    var r = random.NextDouble();
    var idx = Array.BinarySearch(cumulative, r);
    if (idx < 0)
    {
        idx = ~idx;
    }

    idx = Math.Min(idx, customerCount - 1);
    return customerIds[idx];
}

// Make sure at least one customer has a genuinely large history for the export benchmark:
// customerIds[0] carries the heaviest weight already, but pin it explicitly so the seed is
// deterministic regardless of how the random draws land.
var heavyCustomerId = customerIds[0];

string[] statusPool = ["Draft", "Submitted", "Submitted", "Submitted", "Submitted", "Submitted", "Shipped", "Shipped", "Shipped", "Cancelled"];

var now = DateTimeOffset.UtcNow;
var windowStart = now.AddDays(-395);

Console.WriteLine($"Seeding {orderCount:N0} orders...");
var orderIds = new Guid[orderCount];

await using (var orderWriter = await connection.BeginBinaryImportAsync(
    "copy orders (id, customer_id, status, created_at_utc, submitted_at_utc) from stdin (format binary)"))
{
    for (var i = 0; i < orderCount; i++)
    {
        var customerId = i % 500 == 0 ? heavyCustomerId : PickCustomer();
        var status = statusPool[random.Next(statusPool.Length)];
        var createdAt = windowStart.AddSeconds(random.NextDouble() * (now - windowStart).TotalSeconds);
        DateTimeOffset? submittedAt = status is "Submitted" or "Shipped"
            ? createdAt.AddMinutes(random.Next(5, 240))
            : null;

        var orderId = Guid.NewGuid();
        orderIds[i] = orderId;

        await orderWriter.StartRowAsync();
        await orderWriter.WriteAsync(orderId, NpgsqlTypes.NpgsqlDbType.Uuid);
        await orderWriter.WriteAsync(customerId, NpgsqlTypes.NpgsqlDbType.Uuid);
        await orderWriter.WriteAsync(status, NpgsqlTypes.NpgsqlDbType.Varchar);
        await orderWriter.WriteAsync(createdAt, NpgsqlTypes.NpgsqlDbType.TimestampTz);
        if (submittedAt.HasValue)
        {
            await orderWriter.WriteAsync(submittedAt.Value, NpgsqlTypes.NpgsqlDbType.TimestampTz);
        }
        else
        {
            await orderWriter.WriteNullAsync();
        }

        if (i > 0 && i % 20000 == 0)
        {
            Console.WriteLine($"  ...{i:N0} orders");
        }
    }

    await orderWriter.CompleteAsync();
}

Console.WriteLine("Seeding order lines...");
await using (var lineWriter = await connection.BeginBinaryImportAsync(
    "copy order_lines (id, product_id, quantity, unit_price, order_id) from stdin (format binary)"))
{
    for (var i = 0; i < orderCount; i++)
    {
        var lineRoll = random.NextDouble();
        var lineCount = lineRoll < 0.5 ? random.Next(1, 3) : lineRoll < 0.8 ? random.Next(3, 6) : random.Next(6, 11);

        for (var l = 0; l < lineCount; l++)
        {
            var productIndex = random.Next(productCount);
            var quantity = random.Next(1, 6);
            var jitter = 1 + (random.NextDouble() * 0.1 - 0.05);
            var unitPrice = Math.Round(productBasePrices[productIndex] * (decimal)jitter, 2);

            await lineWriter.StartRowAsync();
            await lineWriter.WriteAsync(Guid.NewGuid(), NpgsqlTypes.NpgsqlDbType.Uuid);
            await lineWriter.WriteAsync(productIds[productIndex], NpgsqlTypes.NpgsqlDbType.Uuid);
            await lineWriter.WriteAsync(quantity, NpgsqlTypes.NpgsqlDbType.Integer);
            await lineWriter.WriteAsync(unitPrice, NpgsqlTypes.NpgsqlDbType.Numeric);
            await lineWriter.WriteAsync(orderIds[i], NpgsqlTypes.NpgsqlDbType.Uuid);
        }

        if (i > 0 && i % 20000 == 0)
        {
            Console.WriteLine($"  ...{i:N0} orders' worth of lines");
        }
    }

    await lineWriter.CompleteAsync();
}

stopwatch.Stop();
Console.WriteLine($"Done in {stopwatch.Elapsed.TotalSeconds:N1}s. Heaviest customer id for the history benchmark: {heavyCustomerId}");
