# In Defense of the Repository - companion code

A small Order/OrderLine domain, built five different ways, to compare a constrained,
specification-driven repository against coding directly against EF Core's `DbContext`.

Everything here runs in Docker. The only host prerequisite is Docker (with Compose).

## What's in here

- **Domain** - the `Order` aggregate: invariants, statuses, no EF Core or any other dependency.
- **Application** - `ISpecification<T>`, `IRepository<T>`/`IReadRepository<T>`, `IUnitOfWork`,
  and the named specs (`OpenOrdersForCustomer`, `OrderWithLines`).
- **Infrastructure** - the EF Core (Npgsql) implementation: `OrderingDbContext`, the
  `SpecificationEvaluator`, the open-generic `EfRepository<T>`, and `OrderRepository`'s one
  tuned, hand-written-SQL method.
- **Api** - a minimal API exposing two endpoints, wired up only through `IOrderRepository` and
  `IUnitOfWork` - no `DbContext` in sight.
- **Comparisons** - the same scenario ("get a customer's open orders, submit one") implemented
  five ways: raw `DbContext`, query objects, a classic named-method repository, a specification
  repository, and the constrained hybrid above.
- **Seeder** - a console app that applies migrations and seeds the database (50,000 orders by
  default) via Npgsql's binary `COPY`, for a dataset large enough that the tuned path isn't
  trivially identical to the spec path.
- **Tests** - unit tests (aggregate invariants, specification criteria/paging), integration
  tests against real Postgres, and contract tests proving the spec path and the named-method
  path return identical results.
- **ArchitectureTests** - ArchUnitNET rules enforcing the dependency direction (Domain depends on
  nothing; Application/Api don't reference EF Core; nothing outside Infrastructure depends on its
  internals), plus reflection-based checks for the generic constraints, `internal` visibility, and
  the one narrow protected member `EfRepository<T>` is allowed to expose.

See `FINDINGS.md` for what building this surfaced, including a full comparison table and where
the constrained approach loses to simpler options.

## Run everything

```
docker compose up --build
```

This starts Postgres, waits for it to be healthy, runs `migrate-and-seed` (applies EF Core
migrations, then seeds 50,000 orders), and starts the Api on `http://localhost:8080`.

Try it:

```
curl http://localhost:8080/customers/<a-customer-id-from-the-seeded-data>/open-orders
```

(Any seeded customer ID works - query Postgres directly if you want one: `docker compose exec
postgres psql -U postgres -d repository_pattern -c "SELECT DISTINCT customer_id FROM orders LIMIT 5;"`.)

## Run the test suite

```
docker compose run --rm tests
```

This builds the whole solution and runs both the main test project and `ArchitectureTests`
against a real Postgres database in the `postgres` service - no SQLite, no in-memory provider.

To seed a smaller or larger dataset:

```
SEED_ORDER_COUNT=5000 docker compose up migrate-and-seed
```

## Tear down

```
docker compose down -v
```

(`-v` also removes the Postgres data volume, so the next `up` reseeds from scratch.)

