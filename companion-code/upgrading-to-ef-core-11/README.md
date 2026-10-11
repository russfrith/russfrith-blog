# Upgrading to EF Core 11 — companion code

Verification code for the post ["Upgrading to EF Core 11: Prettier SQL and a Few Ugly Changes"](../../content/posts/upgrading-to-ef-core-11/index.md).

This isn't a benchmark; it's a single console program (`EfCore11Demo`) that prints the actual SQL and schema EF Core 11 generates for each example quoted in the post:

- The to-one join pruning and redundant `ORDER BY` trimming for reference navigations.
- The `FullJoin` translation.
- `EF.Functions.JsonPathExists()`.
- The SQL Server compatibility level default (160) versus pinning back to 150, using `Math.Min` translation (`LEAST`) as the visible difference.
- The Complex Types / value objects deep dive: `PersonName` and `PhoneNumber` as `[ComplexType]` records, mapped onto a `Person`/`Customer`/`Employee` hierarchy with `UseTptMappingStrategy()`, lambda-chained `Property()`/`HasMaxLength()` configuration, and an index on a property nested inside a complex type.
- The `Address`/owned-type shared-instance bug: the same `Order` with `BillingAddress` and `ShippingAddress` pointing at one `Address` instance, configured once as an owned type (throws) and once as a complex type (doesn't).

Sections 1-5 use [`ToQueryString()`](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries#getting-the-generated-sql), section 6 uses [`Database.GenerateCreateScript()`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.infrastructure.databasefacade.generatecreatescript), and both translate from the model without opening a connection. Section 7 is different: it isn't a query or schema translation, it's `DbSet.Add()` itself throwing (or not), which is a change-tracking check that also doesn't need a live database, just a different reason why. None of this needs a real SQL Server instance; it's checking EF's model, query translation, and change tracking, not actual database I/O.

Note on naming: the models for the breaking-changes examples use `Owner` (for `Blog.Owner`) and `SalesCustomer`/`SalesOrder` (for the `FullJoin` example) instead of the generic `Person`/`Customer`/`Order` names those examples use in Microsoft's own docs. That's to avoid colliding with `Person`/`Customer`/`Order` in the value-objects section below, which are the names the post actually uses for that example. `Address` itself isn't marked `[ComplexType]`, since it's deliberately configured two different ways in two different `DbContext` classes (`DemoContext` as a complex type, `LegacyOwnedAddressContext` as an owned type) to reproduce both sides of the bug.

EF Core 11 is still in preview as of this writing, so the package version pinned in `EfCore11Demo.csproj` (and the SQL in the post) could drift before release. The `Dockerfile` intentionally tracks the floating `mcr.microsoft.com/dotnet/nightly/sdk:11.0-preview` tag rather than pinning a digest, so `docker build` keeps working as the preview moves forward; that also means a rebuild today may not match what originally produced the output below. For an exact repro, the last known-good combination was:

- Image: `mcr.microsoft.com/dotnet/nightly/sdk:11.0-preview`, resolving to SDK `11.0.100-rc.1.26411.119` (digest `sha256:3c8fca6c90a642f57bf9970c0158401219e7563573f39e253716ffc21e20eda6`)
- `Microsoft.EntityFrameworkCore.SqlServer` `11.0.0-preview.6.26359.118`
- Tested 2026-10-11

One caveat on the split-query example: `ToQueryString()` only returns the SQL for a split query's *first* statement (EF prints a note to that effect in its own output). The join-pruning fix the post describes applies to the *second* statement (the one generated for the `Posts` collection), which isn't observable through `ToQueryString()` without executing against a real database. The first-statement output here confirms the query still runs and returns the main entity plus its reference navigation; it doesn't independently verify the pruned join. The `ORDER BY` trimming example (section 2) has no such limitation and is fully verified.

## Run locally

Requires the [.NET 11 preview SDK](https://dotnet.microsoft.com/en-us/download/dotnet/11.0).

```bash
dotnet run --project EfCore11Demo/EfCore11Demo.csproj -c Release
```

## Run in Docker

```bash
docker build -t ef11-upgrade-demo .
docker run --rm -v "$PWD/results:/out" ef11-upgrade-demo
```

The container builds the project with the `mcr.microsoft.com/dotnet/nightly/sdk:11.0-preview` image (EF Core 11 isn't available on the stable SDK yet), runs it, and tees the output to `./results/generated-sql.txt` on the host as well as the console.
