using EfCore11Demo;
using Microsoft.EntityFrameworkCore;

// Verifies the SQL claims made in the post by printing what EF Core 11
// actually generates for each example. ToQueryString() translates a query
// to SQL without opening a connection, so none of this needs a live
// database - it's checking EF's query translation, not query execution.

const string connectionString =
    "Server=(local);Database=EfCore11Demo;Trusted_Connection=True;TrustServerCertificate=True;";

var defaultOptions = new DbContextOptionsBuilder<DemoContext>()
    .UseSqlServer(connectionString)
    .Options;

var pinnedTo150Options = new DbContextOptionsBuilder<DemoContext>()
    .UseSqlServer(connectionString, o => o.UseCompatibilityLevel(150))
    .Options;

Console.WriteLine("EF Core 11 upgrade guide - generated SQL verification");
Console.WriteLine("=======================================================");

using (var context = new DemoContext(defaultOptions))
{
    PrintSection(
        "1. Split query: to-one join pruning",
        "Blogs.Include(b => b.BlogType).Include(b => b.Posts).AsSplitQuery()",
        context.Blogs
            .Include(b => b.BlogType)
            .Include(b => b.Posts)
            .AsSplitQuery()
            .ToQueryString());

    PrintSection(
        "2. Single query: redundant ORDER BY trimming",
        "Blogs.Include(b => b.Owner).Include(b => b.Posts)",
        context.Blogs
            .Include(b => b.Owner)
            .Include(b => b.Posts)
            .ToQueryString());

    PrintSection(
        "3. FullJoin translation",
        "SalesCustomers.FullJoin(SalesOrders, c => c.Id, o => o.CustomerId, ...)",
        context.SalesCustomers
            .FullJoin(
                context.SalesOrders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { Customer = c, Order = o })
            .ToQueryString());

    PrintSection(
        "4. EF.Functions.JsonPathExists()",
        "Blogs.Where(b => EF.Functions.JsonPathExists(b.JsonData, \"$.OptionalInt\"))",
        context.Blogs
            .Where(b => EF.Functions.JsonPathExists(b.JsonData!, "$.OptionalInt"))
            .ToQueryString());
}

Console.WriteLine("5. SQL Server compatibility level default (160) vs pinned (150)");
Console.WriteLine(new string('-', 65));
Console.WriteLine("LINQ: Blogs.Select(b => Math.Min(b.Id, b.BlogTypeId))");
Console.WriteLine();

using (var defaultContext = new DemoContext(defaultOptions))
using (var pinnedContext = new DemoContext(pinnedTo150Options))
{
    var defaultSql = defaultContext.Blogs
        .Select(b => new { b.Id, MinId = Math.Min(b.Id, b.BlogTypeId) })
        .ToQueryString();

    var pinnedSql = pinnedContext.Blogs
        .Select(b => new { b.Id, MinId = Math.Min(b.Id, b.BlogTypeId) })
        .ToQueryString();

    Console.WriteLine("Default (compatibility level 160, SQL Server 2022+):");
    Console.WriteLine(defaultSql);
    Console.WriteLine();
    Console.WriteLine("Pinned to compatibility level 150 (SQL Server 2019):");
    Console.WriteLine(pinnedSql);
    Console.WriteLine();
}

Console.WriteLine("6. Complex Types on TPT: generated schema");
Console.WriteLine(new string('-', 42));
Console.WriteLine(
    "LINQ: Person (abstract, PersonName Name + PhoneNumber Phone, both [ComplexType]), " +
    "Customer : Person, Employee : Person, UseTptMappingStrategy()");
Console.WriteLine();

using (var context = new DemoContext(defaultOptions))
{
    // GenerateCreateScript() builds DDL from the model, the same way
    // ToQueryString() builds a query - no live connection required. Look
    // for Name_First/Name_Last/Phone_CountryCode/Phone_NationalNumber on
    // [Person], separate [Customer]/[Employee] tables sharing that key
    // (TPT), a 15-character NationalNumber column (the lambda-chained
    // HasMaxLength), and an index on Phone_NationalNumber.
    Console.WriteLine(context.Database.GenerateCreateScript());
}

Console.WriteLine("7. Owned types vs complex types: the shared-instance bug");
Console.WriteLine(new string('-', 58));
Console.WriteLine(
    "LINQ: new Order { BillingAddress = address, ShippingAddress = address } " +
    "where both properties point at the same Address instance");
Console.WriteLine();

// This is a change-tracking check, not a query, so Add() throws (or
// doesn't) immediately without touching the database.
var sharedAddress = new Address("221B Baker Street", null, "London", "UK", "NW1 6XE");

var legacyOptions = new DbContextOptionsBuilder<LegacyOwnedAddressContext>()
    .UseSqlServer(connectionString)
    .Options;

using (var legacyContext = new LegacyOwnedAddressContext(legacyOptions))
{
    var order = new Order { BillingAddress = sharedAddress, ShippingAddress = sharedAddress };
    try
    {
        legacyContext.Orders.Add(order);
        Console.WriteLine("Owned type (pre-EF Core 8 equivalent): Add() succeeded (unexpected; the bug should have thrown here).");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine("Owned type (pre-EF Core 8 equivalent): Add() threw, as the post describes:");
        Console.WriteLine($"  {ex.Message}");
    }
}

Console.WriteLine();

using (var context = new DemoContext(defaultOptions))
{
    var order = new Order { BillingAddress = sharedAddress, ShippingAddress = sharedAddress };
    context.Orders.Add(order);
    Console.WriteLine("Complex type (EF Core 11): Add() succeeded, no exception.");
}

return;

static void PrintSection(string title, string linq, string sql)
{
    Console.WriteLine(title);
    Console.WriteLine(new string('-', title.Length));
    Console.WriteLine($"LINQ: {linq}");
    Console.WriteLine();
    Console.WriteLine(sql);
    Console.WriteLine();
}
