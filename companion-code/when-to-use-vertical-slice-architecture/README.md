# Vertical Slice vs. Onion Architecture

Companion solution for the post "Vertical Slice vs. Onion Architecture: Let the Requirements Decide". The same two features, a product catalogue (CRUD) and an order discount code (eligibility, stacking, expiry, usage limits), implemented twice: once in an Onion-style layout, once as vertical slices. Both run against SQLite with no external setup.

## Requirements

- .NET 10 SDK

## Structure

```
Shop.slnx
Onion/
  Shop.Onion.Domain/          Order, Product, DiscountCode aggregates and invariants
  Shop.Onion.Application/     Commands, handlers, repository interfaces
  Shop.Onion.Infrastructure/  EF Core, SQLite, repository implementations
  Shop.Onion.Api/             Minimal API host and composition root
  Shop.Onion.Tests/           Domain unit tests + EF-backed handler tests
VerticalSlice/
  Shop.VerticalSlice/         Feature folders: endpoint, handler, validation, data access per feature
  Shop.VerticalSlice.Tests/   Rule unit tests + EF-backed handler tests
```

## Running the APIs

Each side is its own ASP.NET Core minimal API, with its own SQLite file, created and migrated automatically on first run.

```
dotnet run --project Onion/Shop.Onion.Api
# listens on the URL printed in the console; try http://localhost:5000/products

dotnet run --project VerticalSlice/Shop.VerticalSlice
# a separate app and a separate SQLite file; same routes
```

Both seed three discount codes on startup: `SAVE10` (10%, stackable), `FLAT20` ($20 off, not stackable, $50 minimum), `BIGSPENDER` (15%, stackable, $500 minimum).

Example flow against either API:

```
POST /products            { "sku": "WIDGET-1", "name": "Widget", "price": 9.99 }
POST /orders               { "customerId": "<guid>", "lines": [{ "productId": "<guid>", "quantity": 2, "unitPrice": 50 }] }
POST /orders/{id}/discount-code   { "code": "SAVE10" }
```

## Running the tests

```
dotnet test Shop.slnx
```

34 tests across both sides: domain/rule unit tests for the discount logic (eligibility, expiry, usage limits, stacking, both independently of EF Core), plus EF Core-backed tests against an in-memory SQLite database for the handlers that touch the database.

## Docker

No Dockerfile is included; SQLite needs no server, so `dotnet run` is already a one-command start. If you want to containerize either API, a standard ASP.NET Core SDK-build Dockerfile works unmodified since both projects use `Microsoft.Data.Sqlite` rather than a networked database.
