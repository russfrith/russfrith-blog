# EF Core + Dapper light CQRS - companion code

Runnable code for the post ["When EF Core Isn't Enough: Adding Dapper Without Breaking Your Domain Model"](../../content/posts/ef-core-dapper-light-cqrs/index.md).

A small order-management system demonstrating a light CQRS split on top of a single PostgreSQL database:

- **Write side**: `CreateOrderHandler` / `SubmitOrderHandler` -> `IOrderRepository` -> the `Order` aggregate -> EF Core -> `IUnitOfWork`. Repositories are aggregate-shaped: they return `Order`, never a DTO, never `IQueryable`.
- **Read side**: `IOrderQueries` -> Dapper -> DTOs. No repositories, no aggregates, no domain types.
- Two documented exceptions to "Dapper only on the read side," both contained to `Infrastructure/WriteExceptions/` and covered by architecture tests:
  - `OrderAuditWriter` - a Dapper insert that joins the same transaction as an EF Core save (`SubmitOrderHandler`).
  - `BulkMarkOrdersShippedHandler` - a set-based UPDATE that bypasses the aggregate entirely for a nightly batch job.

No event sourcing, no separate read store, no mediator - everything above is a plain injected interface.

## Projects

```
src/
  Domain/            Order aggregate, OrderLine, invariants. No dependencies.
  Application/       Commands, query interfaces (IOrderQueries), DTOs, handler interfaces.
  Infrastructure/
    Write/            AppDbContext, OrderRepository, EfUnitOfWork.
    Reads/             OrderQueries (Dapper). Only namespace allowed to reference Dapper for reads.
    WriteExceptions/   The two documented Dapper-for-writes exceptions.
    Reference/         Customer/Product - plain reference data, not aggregates.
  Api/               Minimal API wiring the above together.
  MigrateAndSeed/    Applies EF migrations, then bulk-seeds ~120k orders via Npgsql binary COPY.
  Tests/             Unit tests (aggregate invariants), architecture tests (NetArchTest), integration tests (real Postgres).
  Benchmarks/        BenchmarkDotNet: EF tracked / AsNoTracking / AsNoTracking+Select / compiled query / Dapper, per scenario.
```

## Requirements

- Docker and Docker Compose. That's the only thing you need on the host.
- To run anything outside Docker: .NET 10 SDK and a local PostgreSQL 17.

## Running it

Start Postgres, run migrations + seed data, then bring up the API:

```bash
docker compose up --build
```

This starts `postgres` (healthchecked), runs `migrate-and-seed` once (applies EF Core migrations, then seeds ~120,000 orders and ~425,000 order lines with Npgsql's binary COPY - a few seconds, not minutes), and starts `api` on `http://localhost:8080`.

Try it:

```bash
curl "http://localhost:8080/orders?status=Submitted&page=1&pageSize=5"
```

### Tests

Integration tests run against the same compose Postgres, in a separate `orders_test` database (created automatically by `docker/init-test-db.sql` the first time the Postgres volume initializes) so they never touch the seeded benchmark data:

```bash
docker compose --profile tests up --abort-on-container-exit tests
```

### Benchmarks

Requires `migrate-and-seed` to have run first (seed data in place) and the Postgres container warm:

```bash
docker compose up -d postgres migrate-and-seed
docker compose --profile benchmarks up --abort-on-container-exit benchmarks
```

Results land in `./benchmark-results/` (mounted from the container) as Markdown, CSV, and HTML reports per benchmark class.

The numbers in the post come from this containerized setup with Postgres and the benchmark runner each capped at `cpus: "2"`, `memory: 2G` (see `docker-compose.yml`'s `deploy.resources.limits`) on specific host hardware - treat them as relative comparisons between query strategies, not absolute numbers for your own hardware.

### Resetting the database

```bash
docker compose down
docker volume rm ef-core-dapper-light-cqrs_orders-pg-data
docker compose up --build
```

## Running outside Docker

```bash
# Postgres 17 listening on localhost:5432, databases "orders" and "orders_test"
export ORDERS_DB_CONNECTION="Host=localhost;Port=5432;Database=orders;Username=postgres;Password=postgres"
export ORDERS_TEST_DB_CONNECTION="Host=localhost;Port=5432;Database=orders_test;Username=postgres;Password=postgres"

dotnet run --project src/MigrateAndSeed/OrderLightCqrs.MigrateAndSeed.csproj -c Release
dotnet run --project src/Api/OrderLightCqrs.Api.csproj -c Release
dotnet test src/Tests/OrderLightCqrs.Tests.csproj
dotnet run --project src/Benchmarks/OrderLightCqrs.Benchmarks.csproj -c Release
```

`SEED_CUSTOMER_COUNT`, `SEED_PRODUCT_COUNT`, and `SEED_ORDER_COUNT` env vars override the seed size (defaults: 3000 / 400 / 120000) for faster local iteration.

## Known harmless warning

`migrate-and-seed` and `api` log `Cannot load library libgssapi_krb5.so.2` on startup. That's Npgsql probing for optional Kerberos/GSSAPI support, which isn't installed in the slim runtime image and isn't used by this app's password-based auth. It's a warning, not an error - both containers run and exit cleanly.
