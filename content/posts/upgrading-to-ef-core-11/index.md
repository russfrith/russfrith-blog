---
title: "Upgrading to EF Core 11: Prettier SQL and a Few Ugly Changes"
description: "A practical guide to moving from EF Core 10, plus a deep dive into Complex Types and why value objects matter"
date: 2026-10-10T20:15:50-04:00
draft: true
cover:
  image: Wild-and-Geometric-Bonsai-Contrast.png
  alt: "A bonsai tree in a ceramic pot, its right side pruned into sharp, right-angled geometric branches"
  caption: "Image created by author using ChatGPT"
ShowToc: true
tags: ["dotnet", "csharp", "sqlserver", "efcore"]
categories: ["programming"]
---

{{< aside >}}
EF Core 11 is still in development as of this writing (based on the Microsoft Learn docs for EF Core 11, last updated August 2026). Some of what's below could change before release. I tested against the `mcr.microsoft.com/dotnet/nightly/sdk:11.0-preview` image (resolving to SDK `11.0.100-rc.1.26411.119`) and `Microsoft.EntityFrameworkCore.SqlServer` `11.0.0-preview.6.26359.118`; see the [companion code](https://github.com/russfrith/russfrith-blog/tree/master/companion-code/upgrading-to-ef-core-11) for the runnable version of every SQL example below.
{{< /aside >}}

EF Core 11 ships alongside .NET 11, and it requires the .NET 11 SDK to build and the .NET 11 runtime to run; there's no running it on .NET 10 or earlier, and definitely not on .NET Framework.

The verdict up front: for most apps, this is a quiet upgrade. The breaking changes[[2](#references)] are mostly edge cases: sync I/O that was already deprecated, a SQL Server compatibility level bump, a couple of defaults changing. If you're on Cosmos DB, on vector columns, or still calling synchronous EF APIs, read the next section carefully before you touch the package reference. Everyone else can skim it and move on to the good stuff.

This post does two things. The first half is a straight upgrade guide: what breaks, what's new, a checklist. The second half is a deep dive into Complex Types, which I think is the most consequential change in this release even though it's not technically new in EF Core 11. EF Core 11 is what finally makes Complex Types usable for the kind of modeling I actually want to do. Use the table of contents to jump to whichever one you came for.

## Before you upgrade: what will break

{{< aside >}}
What I actually hit while building the companion code: `ToQueryString()`[[5](#references)] only returns the SQL for a split query's *first* statement; it won't show you the pruned join from the [Prettier SQL](#prettier-sql-for-reference-navigations) section unless you execute against a real database and capture the command log. Not a breaking change, just a gap in the tooling worth knowing about if you're trying to eyeball query changes the same way. The [compatibility-level default change](#sql-server-users) also surprised me a bit: pinning back to 150 doesn't make `Math.Min` throw, it just silently falls back to client evaluation and pulls both raw columns instead of translating to `LEAST`. Worth checking your logs for unexpected client-eval warnings after upgrading, not just outright errors.
{{< /aside >}}

### Everyone

- **Sync I/O APIs now always throw.** This was already the case for the Azure Cosmos DB provider since EF Core 9, where calling something like `ToList()` or `SaveChanges()` threw unless you'd opted back into "sync-over-async" blocking. In EF Core 11, that opt-out is gone entirely; sync I/O via Cosmos always throws, full stop. Convert any remaining sync calls (`SaveChanges()`, `ToList()`, `First()`, and so on) to their async equivalents (`SaveChangesAsync()`, `ToListAsync()`, `FirstAsync()`). This specific change is scoped to the Cosmos provider; relational providers (SQL Server, SQLite, etc.) still allow sync I/O.
- **EF throws by default when `Migrate()` finds no migrations.** Previously this logged an informational message and did nothing. Now it throws, on the theory that calling `Migrate()` with zero migrations in the assembly usually means something's misconfigured. If you genuinely manage schema outside of EF migrations, either drop the `Migrate()` call or suppress it: `options.ConfigureWarnings(w => w.Ignore(RelationalEventId.MigrationsNotFound))`.

### SQL Server users

- **`UseSqlServer` now defaults to compatibility level 160** (SQL Server 2022), up from 150 (SQL Server 2019). This lets EF generate SQL using newer features like `LEAST`/`GREATEST` by default. If your database is still on SQL Server 2019 or older, or just runs at a lower compatibility level, some of the SQL EF generates may not work against it. Pin the old behavior explicitly: `optionsBuilder.UseSqlServer(connectionString, o => o.UseCompatibilityLevel(150));`
- **`Microsoft.Data.SqlClient` moves to 7.0**, which strips Azure/Entra ID authentication dependencies (`Azure.Core`, `Azure.Identity`, `Microsoft.Identity.Client`) out of the core package to cut dependency bloat. If your app authenticates with anything like `ActiveDirectoryDefault`, `ActiveDirectoryInteractive`, `ActiveDirectoryManagedIdentity`, or `ActiveDirectoryServicePrincipal`, you now need to add `Microsoft.Data.SqlClient.Extensions.Azure` as a separate package reference. No code changes needed beyond that: just the extra package. Also, `SqlAuthenticationMethod.ActiveDirectoryPassword` is now obsolete; migrate off it if you're still using it.

### Vector users

- **`SqlVector<T>` properties are no longer included in `SELECT` when materializing entities.** Vector columns can hold hundreds or thousands of floats, and in most cases you write them once and only ever use them for search, never read them back, so EF stops pulling them back by default. The property comes back `null` on the loaded entity unless you project for it explicitly:

  ```csharp
  var embeddings = await context.Blogs
      .Select(b => new { b.Id, b.Embedding })
      .ToListAsync();
  ```

  Vectors still work fine in `WHERE`/`ORDER BY`, including with `VectorDistance()` and `VectorSearch()`; they just won't ride along on the entity projection. The docs note an opt-in to restore automatic loading is coming later in the EF Core 11 release, so this may get easier before GA.

### Cosmos DB users (the biggest change)

This is where most of the real risk lives if you're on Cosmos.

- **Illegal `id` characters are no longer escaped.** When generating a Cosmos `id` from a composite key, EF used to escape characters that are illegal in Cosmos resource ids (`/` → `^2F`, `\` → `^5C`, `?` → `^3F`, `#` → `^23`). That scheme had a real bug: the escape character `^` was never itself escaped, so a key value containing the literal string `^2F` collided with a key value containing `/`; two different entities silently mapped to the same document. EF Core 11 fixes this by just not escaping those characters at all; the raw key values go into the `id`.

  The catch: if you have existing data, EF generated those ids with the old escaped format (e.g. `Post|1|^2F`). After upgrading, EF will look for `Post|1|/` instead and won't find the existing document. You have two options: re-key/migrate the existing documents to the new unescaped format, or opt back into the old (buggy) escaping behavior as a stopgap:

  ```csharp
  AppContext.SetSwitch("Microsoft.EntityFrameworkCore.EscapeIllegalCosmosIdCharacters", true);
  ```

  Only do the latter if you can't migrate right away; the id-collision bug is still there if you do.

- **Unmapped JSON properties are no longer preserved on round-trip.** This follows from the Cosmos provider dropping its `Newtonsoft.Json`-based serialization (and the `__jObject` shadow property along with it) in favor of `System.Text.Json`. If your documents have fields outside your EF model that you were relying on EF to read-and-rewrite transparently, those fields will now be dropped on save. Map the fields explicitly in your model, or go around EF with `CosmosClient` directly for documents where you need full control over the JSON shape.
- **Undefined projections now throw instead of silently misbehaving.** Previously, projecting through an optional navigation where part of the path was missing in the document gave you inconsistent results: single-property projections silently dropped rows, multi-property projections threw a confusing `InvalidOperationException`. Now it's consistently an `InvalidOperationException` either way. Use `EF.Functions.IsDefined(...)` to filter, or `EF.Functions.CoalesceUndefined(...)` to supply a default.
- Smaller Cosmos changes worth knowing about: empty owned collections now materialize as an empty collection instead of `null`; the discriminator property is named `Discriminator` in the model again (while the JSON property name stays `$type` by default, so the model name and JSON name are now decoupled); and floating-point values are truncated rather than rounded when materializing into fixed-point types.

### Build and tooling

- **The `EFOptimizeContext` MSBuild property is gone.** Code generation (compiled models, precompiled queries) is now controlled entirely via `EFScaffoldModelStage` and `EFPrecompileQueriesStage`, set to `publish` or `build`. If you have `PublishAOT` set to `true`, code generation at publish time happens automatically without any of this.
- **`Microsoft.EntityFrameworkCore.Tools` and `.Tasks` no longer pull in `Microsoft.EntityFrameworkCore.Design`.** If your project was relying on that dependency coming along for free via the tools package, add it directly: `<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="11.0.0" PrivateAssets="all" />`

## What you get for the trouble

### Prettier SQL for reference navigations

This is the post's title payoff, so it gets the most space. EF Core 11 trims two kinds of waste out of queries that include to-one (reference) navigations.

First, split queries (`AsSplitQuery()`) used to join in reference navigations that the split statement didn't actually need. Given:

```csharp
var blogs = context.Blogs
    .Include(b => b.BlogType)
    .Include(b => b.Posts)
    .AsSplitQuery()
    .ToList();
```

the `Posts` split query used to unnecessarily join `BlogType`:

```sql
-- Before EF Core 11
SELECT [p].[Id], [p].[BlogId], [p].[Title], [b].[Id], [b0].[Id]
FROM [Blogs] AS [b]
INNER JOIN [BlogType] AS [b0] ON [b].[BlogTypeId] = [b0].[Id]
INNER JOIN [Post] AS [p] ON [b].[Id] = [p].[BlogId]
ORDER BY [b].[Id], [b0].[Id]
```

EF Core 11 prunes the join it doesn't need:

```sql
-- EF Core 11
SELECT [p].[Id], [p].[BlogId], [p].[Title], [b].[Id]
FROM [Blogs] AS [b]
INNER JOIN [Post] AS [p] ON [b].[Id] = [p].[BlogId]
ORDER BY [b].[Id]
```

Second, EF no longer adds a reference navigation's key to `ORDER BY` when it's already functionally determined by the parent's key via the foreign key:

```sql
-- Before EF Core 11
ORDER BY [b].[BlogId], [p].[PersonId]

-- EF Core 11
ORDER BY [b].[BlogId]
```

Neither change is just cosmetic. Microsoft's own benchmarks show a 29% improvement for the split-query join pruning, and a 22% improvement for the `ORDER BY` trim on single queries; bigger wins the more reference navigations you're including.

### Complex types, finally unblocked

EF Core 11 lifts the restriction that kept Complex Types and JSON column mapping off entity types using TPT (table-per-type) or TPC (table-per-concrete-type) inheritance, adds lambda-chained configuration, and adds keys/indexes on complex-type properties. This is the headline change for me personally, so it gets its own section below: [Complex Types as value objects](#complex-types-as-value-objects).

### FullJoin

.NET 11 adds `FullJoin` as a first-class LINQ operator, and EF Core 11 translates it to a SQL `FULL JOIN`:

```csharp
var results = await context.Customers
    .FullJoin(
        context.Orders,
        c => c.Id,
        o => o.CustomerId,
        (c, o) => new { Customer = c, Order = o })
    .ToListAsync();
```

```sql
SELECT [c].[Id], [c].[Name], [o].[Id], [o].[CustomerId], [o].[OrderDate]
FROM [Customers] AS [c]
FULL JOIN [Orders] AS [o] ON [c].[Id] = [o].[CustomerId]
```

This is the kind of thing you don't need often, but when you do need it, hand-writing it in raw SQL was the only option before now.

### Smaller wins worth knowing about

- `MaxByAsync`/`MinByAsync` translation: find the entity with the max/min value of a key selector, rather than just the max/min value itself.
- `EF.Functions.JsonPathExists()`: checks whether a JSON path exists in a document, translating to SQL Server's `JSON_PATH_EXISTS`.
- Vector index support in migrations, via `HasVectorIndex()`, plus `VectorSearch()`/`WithApproximate()` for approximate nearest-neighbor search against SQL Server 2025's `VECTOR_SEARCH()`.
- `GroupBy` translation improvements, including correct materialization of anonymous-type/DTO projections through left joins and grouping.
- `dotnet ef database update --add`: scaffolds and applies a migration in one step, useful for containerized or Aspire-style setups where recompiling isn't really an option.

There's more beyond this: full-text search table-valued functions, `JSON_CONTAINS` translation for SQL Server 2025, temporal period properties mapped to real CLR properties instead of shadow properties, and a pile of Cosmos DB features (transactional batches, bulk execution, session token management). The official [What's New in EF Core 11](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)[[1](#references)] page covers all of it.

## Complex Types as value objects

### A decade of circling back to the same problem

I've been using Entity Framework since the "full" .NET Framework days, and classic EF had a real `ComplexType` feature: you could define a reference-type class with no key of its own, map it to columns on the owner's table, and use it for things like `Address` without EF pretending it was a separate entity. It had sharp edges (reference types only, no sharing the same instance across two properties without duplicating it), but it worked, and it modeled the right concept.

When EF Core shipped in 2016, complex types didn't come with it. The replacement was owned entity types, introduced in EF Core 2.0, and owned types are, underneath, actual entity types. EF gives them a hidden key so it can track them, which means they inherit all the baggage that comes with identity: a shadow primary key you never asked for, change-tracking semantics built around that key, and (the part that bit people most often) an inability to point two properties at the same owned instance. For seven years, that was the best EF Core had.

Here's that bug made concrete, using `Address` as the example. Take an `Order` with a billing and a shipping address that happen to be the same place:

```csharp
[ComplexType]
public record Address(string Line1, string? Line2, string City, string Country, string PostCode);

public class Order
{
    public int Id { get; set; }
    public required Address BillingAddress { get; set; }
    public required Address ShippingAddress { get; set; }
}
```

```csharp
var address = new Address("221B Baker Street", null, "London", "UK", "NW1 6XE");

var order = new Order
{
    BillingAddress = address,
    ShippingAddress = address
};

context.Orders.Add(order);
await context.SaveChangesAsync();
```

With `Address` as an owned entity type (the only option before EF Core 8), `Orders.Add(order)` throws immediately, before `SaveChangesAsync()` even runs:

```
InvalidOperationException: The same object instance of type 'Address' is being referenced from the navigation 'BillingAddress' on 'Order' and from the navigation 'ShippingAddress' on 'Order'. Owned entity types cannot have multiple owners pointing to the same instance. Consider creating a copy of the object to use for each navigation.
```

EF sees two properties pointing at the same owned instance and reads that as the same hidden-keyed entity trying to belong to two owners at once, which it can't allow. With `Address` as a complex type, the same code just works: there's no hidden identity to collide over, so two properties holding equal values are simply two properties holding equal values. That gap between "this is obviously fine" and "EF throws an exception" is exactly the gap complex types close.

EF Core 8 finally brought real complex types back[[3](#references)], as a genuine third category alongside primitive types and entity types: structured, multi-value, and explicitly *not* identified by a key. The general reference docs for the feature are worth bookmarking too[[4](#references)]. EF Core 10 added JSON column mapping and complex collections (JSON only, with no dedicated table). EF Core 11 fills in what was still missing for anyone doing serious modeling with them: TPT/TPC inheritance support, lambda-chained configuration so you don't have to thread through `ComplexProperty()` just to set a `HasMaxLength`, and keys/indexes on properties nested inside a complex type. That's the combination that makes this, for me, the first EF Core release where modeling a value object doesn't feel like working around the tool.

### Why the entity/value distinction matters, DDD or not

You don't need to buy into Domain-Driven Design as a methodology to care about this distinction. It answers a concrete question you're already asking every time you add a property: does this thing have an identity I need to track independently of its data, or is it fully described by its current values?

A `Customer` has identity: two customers with identical names and addresses are still two different customers, and if one of them changes their address, that doesn't affect the other. An `Address` or a `PhoneNumber`, on the other hand, is usually just a value: two instances with the same data *are* the same address, there's no meaningful sense in which they're "different objects that happen to agree," and nothing is lost by replacing one with another that has equal data. Model the first kind as an entity and the second kind as a value, and most of your equality and mutation bugs around these supporting types just don't happen: there's no separate identity to go stale, no shared row that two different owners are secretly both pointing at.

Model a value as an entity instead (which is exactly what owned types forced you into for years) and you get the opposite: a hidden key implies an identity that doesn't exist, which is how you end up debugging why updating one customer's address silently didn't touch another customer's identical-looking address, or why EF threw an error because the same `Address` instance got reused in two places.

### Modeling a PhoneNumber

`Address` already did its job above: it's the example that makes the owned-type bug concrete. For the rest of this section, where I want to walk through TPT mapping, lambda-chained configuration, and indexing a nested property, I'm reaching for `PhoneNumber` instead. I'm deliberately not reaching for `Money`, which drags in currency conversion and exchange rates, a whole separate problem I don't want this example wading into. `PhoneNumber` has real international modeling wrinkles of its own: country codes, variable-length national numbers, formatting that differs by locale, without dragging in anything outside what a value-object example needs to show.

```csharp
[ComplexType]
public record PhoneNumber(int CountryCode, string NationalNumber)
{
    public override string ToString() => $"+{CountryCode} {NationalNumber}";
}
```

An immutable record is the right default shape for a complex type. Complex type instances can be shared across multiple properties on the same entity, and if the shared instance were mutable, changing it through one property would silently change it everywhere else it's referenced too. Making it immutable (a `record`, a `readonly record struct`, or a plain class with an all-args constructor and no setters) sidesteps that entirely: "updating" a phone number means assigning a new instance, never mutating the one you have.

While we're at it, a person's name shouldn't be a bare `string` either. It is less obviously a value object than a phone number, since names don't have the same structural rules across cultures, but even a trivial two-field version beats a raw primitive for something with this much latent structure:

```csharp
[ComplexType]
public record PersonName(string First, string Last)
{
    public override string ToString() => $"{First} {Last}";
}
```

Now a couple of entities that use both value objects, mapped with TPT inheritance (the thing EF Core 11 specifically unblocks):

```csharp
public abstract class Person
{
    public int Id { get; set; }
    public required PersonName Name { get; set; }
    public required PhoneNumber Phone { get; set; }
}

public class Customer : Person
{
    public string Company { get; set; } = string.Empty;
}

public class Employee : Person
{
    public DateOnly HireDate { get; set; }
}
```

### Configuring it: lambda chaining, keys, and TPT

Before EF Core 11, configuring a property nested inside a complex type meant calling `ComplexProperty()` first to get its builder, then `Property()` on that:

```csharp
// Before EF Core 11
modelBuilder.Entity<Person>()
    .ComplexProperty(p => p.Phone)
    .Property(p => p.NationalNumber)
    .HasMaxLength(15);
```

EF Core 11 lets you chain straight through the member access instead:

```csharp
modelBuilder.Entity<Person>()
    .Property(p => p.Phone.NationalNumber)
    .HasMaxLength(15);
```

Indexes work the same way now, which matters if you want to look customers up by phone number (something you couldn't do against a nested complex type property before EF Core 11):

```csharp
modelBuilder.Entity<Person>()
    .HasIndex(p => p.Phone.NationalNumber);
```

And then the TPT mapping itself, which is the part that genuinely didn't work before this release:

```csharp
modelBuilder.Entity<Person>().UseTptMappingStrategy();
```

This mirrors the same mechanism EF Core 11's docs demonstrate with an `Animal`/`AnimalDetails` example: EF creates `Name_First`, `Name_Last`, `Phone_CountryCode`, and `Phone_NationalNumber` columns on the `Person` table (the TPT base table), with `Customer` and `Employee` each getting their own table for just the columns specific to them. Before EF Core 11, adding a complex type to a TPT- or TPC-mapped hierarchy at all would fail: this was a hard blocker, not a rough edge. If you'd rather store the phone number as JSON instead of flattened columns, the same property maps that way too, TPT and all: `.ComplexProperty(p => p.Phone, b => b.ToJson())`.

### Cosmos gets complex types too

Worth calling out on its own, since it's a genuinely separate code path: complex types were not supported at all on the Azure Cosmos DB provider before EF Core 11. As of this release they are, embedded as nested JSON objects (or arrays, for complex collections) directly inside the document, with no separate configuration dance and no owned-type workaround needed there either. If you're modeling value objects against Cosmos, this closes the same gap it closes on the relational side.

### The gotchas

- **Complex types must be explicitly configured.** Unlike owned types, EF won't discover a complex type by convention: you need `[ComplexType]` or `ComplexProperty()`/`ComplexCollection()` in `OnModelCreating`.
- **No `DbSet<T>` of a complex type.** They only ever exist as part of an owning entity; querying one directly means projecting it off its owner (`context.People.Select(p => p.Phone)`), not querying it on its own.
- **Collections of complex types only map to JSON.** `ComplexCollection()` works, but only onto a single JSON column via `ToJson()`; there's still no way to put a complex-type collection into its own table the way you could with an owned-type collection.
- **Shared, mutable instances are still a footgun.** Nothing stops you from making a complex type mutable, but if you do and then share one instance across two properties, mutating it through either property changes both. Immutability isn't enforced: it's just the pattern that avoids the problem.

## Upgrade checklist

- Search for sync calls (`ToList()`, `SaveChanges()`, `First()`, etc.) against Cosmos DB and convert them to async.
- Check your SQL Server version and compatibility level against 160; pin to 150 explicitly if you're not on SQL Server 2022 or later.
- Confirm your migrations assembly is actually being found, or suppress `RelationalEventId.MigrationsNotFound` if you intentionally run without migrations.
- If you use Cosmos DB, plan for the `id`-escaping change before upgrading: decide whether you're migrating existing data or using the `AppContext` switch as a stopgap.
- If you use vectors, review any code that reads `SqlVector<T>` properties off loaded entities; add explicit projections where needed.
- If you're on Cosmos DB and relied on unmapped JSON properties surviving a round-trip, map those properties explicitly now.
- If you have owned JSON collections without an explicit key, plan to migrate them to `ComplexCollection`; they're obsolete as of EF Core 11.
- Run your test suite and diff the generated SQL for your hot-path queries; the compatibility level bump and the join/ORDER BY pruning can both change query plans.

## Wrap-up

Most of EF Core 11's breaking changes are narrow (sync I/O, a compatibility level default, a SqlClient major version bump) and will cost most apps little. The real work is concentrated on Cosmos DB, vectors, and sync I/O, so budget your testing time accordingly. This is all based on docs for a release still in development, so some of it could shift before EF Core 11 ships with .NET 11.

The pruned joins and trimmed `ORDER BY` clauses for reference navigations are worth a closer look on their own, and I may come back to the SQL generation improvements in a follow-up post. But if you only take one thing from this post, take the [Complex Types section above](#complex-types-as-value-objects): it's the reason I'm actually excited about this release.

## References
1. Microsoft. [What's New in EF Core 11](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew). _Microsoft Learn_, last updated August 2026.
2. Microsoft. [Breaking changes in EF Core 11 (EF11)](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/breaking-changes). _Microsoft Learn_, last updated July 2026.
3. Microsoft. [What's New in EF Core 8: Complex types](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-8.0/whatsnew#complex-types). _Microsoft Learn_.
4. Microsoft. [Complex Types](https://learn.microsoft.com/en-us/ef/core/modeling/complex-types). _Microsoft Learn_.
5. Microsoft. [Getting the generated SQL](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries#getting-the-generated-sql). _Microsoft Learn_.
