---
title: "Upgrading to EF Core 11: Prettier SQL and a Few Ugly Changes"
description: "A practical guide to moving from EF Core 10, plus the new features worth the trouble"
date: 2026-10-10T20:15:50-04:00
draft: true
cover:
  image: TODO-cover-image.jpg
  alt: "TODO alt text"
  caption: "TODO caption - check actual image source before writing"
tags: ["dotnet", "csharp", "sqlserver", "efcore"]
categories: ["programming"]
---

{{< aside >}}
EF Core 11 is still in development as of this writing (based on the Microsoft Learn docs for EF Core 11, last updated August 2026). Some of what's below could change before release. TODO: note the exact preview build you tested against, and swap in your own "what broke for me" notes in section 2 once you've actually run the upgrade.
{{< /aside >}}

EF Core 11 ships alongside .NET 11, and it requires the .NET 11 SDK to build and the .NET 11 runtime to run - there's no running it on .NET 10 or earlier, and definitely not on .NET Framework.

The verdict up front: for most apps, this is a quiet upgrade. The breaking changes are mostly edge cases - sync I/O that was already deprecated, a SQL Server compatibility level bump, a couple of defaults changing. If you're on Cosmos DB, on vector columns, or still calling synchronous EF APIs, read the next section carefully before you touch the package reference. Everyone else can skim it and move on to the good stuff.

If you're mid-upgrade right now, jump to [What Will Break](#2-before-you-upgrade-what-will-break). If you're just curious what's new, skip to [What You Get for the Trouble](#3-what-you-get-for-the-trouble).

## 2. Before you upgrade: what will break

### Everyone

- **Sync I/O APIs now always throw.** This was already the case for the Azure Cosmos DB provider since EF Core 9, where calling something like `ToList()` or `SaveChanges()` threw unless you'd opted back into "sync-over-async" blocking. In EF Core 11, that opt-out is gone entirely - sync I/O via Cosmos always throws, full stop. Convert any remaining sync calls (`SaveChanges()`, `ToList()`, `First()`, and so on) to their async equivalents (`SaveChangesAsync()`, `ToListAsync()`, `FirstAsync()`). This specific change is scoped to the Cosmos provider; relational providers (SQL Server, SQLite, etc.) still allow sync I/O.
- **EF throws by default when `Migrate()` finds no migrations.** Previously this logged an informational message and did nothing. Now it throws, on the theory that calling `Migrate()` with zero migrations in the assembly usually means something's misconfigured. If you genuinely manage schema outside of EF migrations, either drop the `Migrate()` call or suppress it: `options.ConfigureWarnings(w => w.Ignore(RelationalEventId.MigrationsNotFound))`.

### SQL Server users

- **`UseSqlServer` now defaults to compatibility level 160** (SQL Server 2022), up from 150 (SQL Server 2019). This lets EF generate SQL using newer features like `LEAST`/`GREATEST` by default. If your database is still on SQL Server 2019 or older, or just runs at a lower compatibility level, some of the SQL EF generates may not work against it. Pin the old behavior explicitly: `optionsBuilder.UseSqlServer(connectionString, o => o.UseCompatibilityLevel(150));`
- **`Microsoft.Data.SqlClient` moves to 7.0**, which strips Azure/Entra ID authentication dependencies (`Azure.Core`, `Azure.Identity`, `Microsoft.Identity.Client`) out of the core package to cut dependency bloat. If your app authenticates with anything like `ActiveDirectoryDefault`, `ActiveDirectoryInteractive`, `ActiveDirectoryManagedIdentity`, or `ActiveDirectoryServicePrincipal`, you now need to add `Microsoft.Data.SqlClient.Extensions.Azure` as a separate package reference. No code changes needed beyond that - just the extra package. Also, `SqlAuthenticationMethod.ActiveDirectoryPassword` is now obsolete; migrate off it if you're still using it.

### Vector users

- **`SqlVector<T>` properties are no longer included in `SELECT` when materializing entities.** Vector columns can hold hundreds or thousands of floats, and in most cases you write them once and only ever use them for search, never read them back - so EF stops pulling them back by default. The property comes back `null` on the loaded entity unless you project for it explicitly:

  ```csharp
  var embeddings = await context.Blogs
      .Select(b => new { b.Id, b.Embedding })
      .ToListAsync();
  ```

  Vectors still work fine in `WHERE`/`ORDER BY`, including with `VectorDistance()` and `VectorSearch()` - they just won't ride along on the entity projection. The docs note an opt-in to restore automatic loading is coming later in the EF Core 11 release, so this may get easier before GA.

### Cosmos DB users (the biggest change)

This is where most of the real risk lives if you're on Cosmos.

- **Illegal `id` characters are no longer escaped.** When generating a Cosmos `id` from a composite key, EF used to escape characters that are illegal in Cosmos resource ids (`/` → `^2F`, `\` → `^5C`, `?` → `^3F`, `#` → `^23`). That scheme had a real bug: the escape character `^` was never itself escaped, so a key value containing the literal string `^2F` collided with a key value containing `/` - two different entities silently mapped to the same document. EF Core 11 fixes this by just not escaping those characters at all; the raw key values go into the `id`.

  The catch: if you have existing data, EF generated those ids with the old escaped format (e.g. `Post|1|^2F`). After upgrading, EF will look for `Post|1|/` instead and won't find the existing document. You have two options - re-key/migrate the existing documents to the new unescaped format, or opt back into the old (buggy) escaping behavior as a stopgap:

  ```csharp
  AppContext.SetSwitch("Microsoft.EntityFrameworkCore.EscapeIllegalCosmosIdCharacters", true);
  ```

  Only do the latter if you can't migrate right away - the id-collision bug is still there if you do.

- **Unmapped JSON properties are no longer preserved on round-trip.** This follows from the Cosmos provider dropping its `Newtonsoft.Json`-based serialization (and the `__jObject` shadow property along with it) in favor of `System.Text.Json`. If your documents have fields outside your EF model that you were relying on EF to read-and-rewrite transparently, those fields will now be dropped on save. Map the fields explicitly in your model, or go around EF with `CosmosClient` directly for documents where you need full control over the JSON shape.
- **Undefined projections now throw instead of silently misbehaving.** Previously, projecting through an optional navigation where part of the path was missing in the document gave you inconsistent results: single-property projections silently dropped rows, multi-property projections threw a confusing `InvalidOperationException`. Now it's consistently an `InvalidOperationException` either way. Use `EF.Functions.IsDefined(...)` to filter, or `EF.Functions.CoalesceUndefined(...)` to supply a default.
- Smaller Cosmos changes worth knowing about: empty owned collections now materialize as an empty collection instead of `null`; the discriminator property is named `Discriminator` in the model again (while the JSON property name stays `$type` by default - the model name and JSON name are now decoupled); and floating-point values are truncated rather than rounded when materializing into fixed-point types.

### Build and tooling

- **The `EFOptimizeContext` MSBuild property is gone.** Code generation (compiled models, precompiled queries) is now controlled entirely via `EFScaffoldModelStage` and `EFPrecompileQueriesStage`, set to `publish` or `build`. If you have `PublishAOT` set to `true`, code generation at publish time happens automatically without any of this.
- **`Microsoft.EntityFrameworkCore.Tools` and `.Tasks` no longer pull in `Microsoft.EntityFrameworkCore.Design`.** If your project was relying on that dependency coming along for free via the tools package, add it directly: `<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="11.0.0" PrivateAssets="all" />`

## 3. What you get for the trouble

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

Neither change is just cosmetic. Microsoft's own benchmarks show a 29% improvement for the split-query join pruning, and a 22% improvement for the `ORDER BY` trim on single queries - bigger wins the more reference navigations you're including.

### Complex types and JSON columns on TPT/TPC inheritance

Complex types and JSON column mapping have been around for a few versions, but until now you couldn't use either on entity types mapped with TPT (table-per-type) or TPC (table-per-concrete-type) inheritance. EF Core 11 lifts that restriction. Given:

```csharp
public abstract class Animal
{
    public int Id { get; set; }
    public string Name { get; set; }
    public required AnimalDetails Details { get; set; }
}

[ComplexType]
public class AnimalDetails
{
    public DateTime BirthDate { get; set; }
    public string? Veterinarian { get; set; }
}

modelBuilder.Entity<Animal>().UseTptMappingStrategy();
```

EF Core 11 creates `Details_BirthDate` and `Details_Veterinarian` columns on the `Animal` table, same as it would without TPT. Mapping `Details` to a JSON column instead works the same way, just by adding `.ComplexProperty(a => a.Details, b => b.ToJson())`.

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

- `MaxByAsync`/`MinByAsync` translation - find the entity with the max/min value of a key selector, rather than just the max/min value itself.
- `EF.Functions.JsonPathExists()` - checks whether a JSON path exists in a document, translating to SQL Server's `JSON_PATH_EXISTS`.
- Vector index support in migrations, via `HasVectorIndex()`, plus `VectorSearch()`/`WithApproximate()` for approximate nearest-neighbor search against SQL Server 2025's `VECTOR_SEARCH()`.
- `GroupBy` translation improvements, including correct materialization of anonymous-type/DTO projections through left joins and grouping.
- `dotnet ef database update --add` - scaffolds and applies a migration in one step, useful for containerized or Aspire-style setups where recompiling isn't really an option.

There's more beyond this - full-text search table-valued functions, `JSON_CONTAINS` translation for SQL Server 2025, temporal period properties mapped to real CLR properties instead of shadow properties, and a pile of Cosmos DB features (transactional batches, bulk execution, session token management). The official [What's New in EF Core 11](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew) page covers all of it.

## 4. Upgrade checklist

- Search for sync calls (`ToList()`, `SaveChanges()`, `First()`, etc.) against Cosmos DB and convert them to async.
- Check your SQL Server version and compatibility level against 160; pin to 150 explicitly if you're not on SQL Server 2022 or later.
- Confirm your migrations assembly is actually being found, or suppress `RelationalEventId.MigrationsNotFound` if you intentionally run without migrations.
- If you use Cosmos DB, plan for the `id`-escaping change before upgrading - decide whether you're migrating existing data or using the `AppContext` switch as a stopgap.
- If you use vectors, review any code that reads `SqlVector<T>` properties off loaded entities - add explicit projections where needed.
- If you're on Cosmos DB and relied on unmapped JSON properties surviving a round-trip, map those properties explicitly now.
- Run your test suite and diff the generated SQL for your hot-path queries - the compatibility level bump and the join/ORDER BY pruning can both change query plans.

## 5. Wrap-up

Most of EF Core 11's breaking changes are narrow - sync I/O, a compatibility level default, a SqlClient major version bump - and will cost most apps little. The real work is concentrated on Cosmos DB, vectors, and sync I/O, so budget your testing time accordingly. This is all based on docs for a release still in development, so some of it could shift before EF Core 11 ships with .NET 11. The pruned joins and trimmed `ORDER BY` clauses for reference navigations are worth a closer look on their own - I may come back to the SQL generation improvements in a follow-up post.
