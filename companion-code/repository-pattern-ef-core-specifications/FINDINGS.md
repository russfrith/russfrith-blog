# Findings

Working notes from building this. Not part of the post - this is for me, written while the
code was still fresh, so I don't have to reconstruct "why did I do it that way" from memory
when I sit down to write.

## Friction points and surprises

- **EF Core silently adds a secondary sort key to collection Includes, but a hand-written
  `ORDER BY` inside `FromSqlInterpolated` doesn't survive `Include()` at all.**
  `OpenOrdersForCustomer` only specifies `OrderByDescending(o => o.CreatedAt)`, but the generated
  SQL for `Include(o => o.Lines)` comes out as `ORDER BY o.created_at DESC, o.id`. EF appends the
  primary key itself, because a single-query collection Include needs a stable row order to
  correctly regroup child rows back onto their parent. I didn't ask for this and it isn't
  documented anywhere obvious - I only noticed it in the API container's query logs.

  The named-method path had a much bigger problem than "no implicit tiebreaker," and I only
  found it by writing a standalone diagnostic and printing full-precision timestamps directly.
  `GetOpenOrdersForCustomerAsync`'s original version put `ORDER BY created_at DESC` *inside* the
  `FromSqlInterpolated` string, then chained `.Include(order => order.Lines)` on the result. That
  ordering doesn't survive. EF composes the raw SQL as an unordered subquery to perform the join
  `Include()` needs, so whatever `ORDER BY` is written inside the SQL text is advisory at best -
  I reproduced rows coming back in a different order than the SQL text asked for, for two orders
  400ms apart with no tie whatsoever, not just a tie-breaking edge case. The contract test caught
  this as a flaky, order-reversed failure that didn't reproduce every run, which looked exactly
  like the CreatedAt tie I'd anticipated, until the diagnostic showed it wasn't a tie at all.
  (Along the way I also found the test environment's own clock resolution is coarser than I
  assumed - a 5ms `Task.Delay` between inserts wasn't always enough to produce distinct
  `CreatedAt` values under Docker Desktop on Windows - which is a real, separate gotcha but not
  the cause of this specific failure.)

  The actual fix: move the ordering out of the raw SQL string entirely and express it as a real
  LINQ call *after* `.Include()` - `.OrderByDescending(order => order.CreatedAt).ThenBy(order =>
  order.Id)`. That guarantees EF adds the ordering to the outermost composed query, not to a
  subquery that's free to be reordered. The `.ThenBy(order => order.Id)` is still a real
  tiebreaker on top of that, matching the one the spec path gets implicitly from `Include()`.
  This is a sharper, more general lesson than "remember to add the same tiebreaker": an `ORDER BY`
  inside hand-written SQL composed with `Include()` isn't just missing a tiebreaker, it isn't
  trustworthy as an order *at all*, and the fix is to never put it there in the first place.

- **The "one protected member" exception needed a real shape, not just a lesser evil.** The
  instructions asked for at most one narrow protected member on `EfRepository<T>`, used only for
  FromSql composition, and told me to remove it if it wasn't earned. I first tried exposing
  `protected IQueryable<T> ApplySpecification(...)` (spec in, query out) and separately giving
  `OrderRepository` raw access to compose SQL - but that's two protected surfaces, and the second
  one really is just a `DbSet` by another name. What I ended up with,
  `ComposeWithRawSql(FormattableString sql, ISpecification<T>? specification = null)`, forces the
  one legitimate use case (hand-written SQL, optionally finished with a spec's includes/ordering)
  and makes a bare "give me the queryable" call impossible without lying about a SQL fragment.
  It's a real constraint, not a named loophole - and `StructuralRuleTests` pins it down by name so
  a second helper can't quietly show up later.

- **`Order.Lines` as a read-only collection over a private field needed explicit EF
  configuration** (`HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field)`) in two
  places - `OrderConfiguration` and the comparison project's own `ComparisonDbContext`. This is
  the one place EF Core's own concepts (`PropertyAccessMode`) leak as far as the entity
  configuration, never into the domain model itself. Small, but worth knowing before you design an
  aggregate around backing fields.

- **Comparisons needing its own `ComparisonDbContext`, duplicated from `OrderingDbContext`, was
  the one duplication I didn't love.** Both map the same two tables. I considered giving
  `Comparisons` `InternalsVisibleTo` access to Infrastructure's internal context instead, but that
  would have meant the "simpler" options (1-4) are built on top of the exact same scaffolding as
  option 5, which defeats the point of comparing them as separately-built alternatives. Keeping
  them separate was the right call, but it's worth being honest that it's ~27 lines of duplicated
  mapping code that exists only for the sake of the comparison.

- **Generic constraints already enforce rule #2 ("only `IAggregateRoot` types can be `T`") at
  compile time** - `IReadRepository<T> where T : class, IAggregateRoot` can't be violated by
  accident; the compiler refuses. `StructuralRuleTests.Repository_interfaces_constrain_T_to_
  aggregate_roots` is really a regression guard against someone *removing* the constraint later,
  not a runtime check. I used plain reflection for this and the other structural rules (internal
  visibility, protected-member shape) rather than forcing them into ArchUnitNET, which is built
  for dependency-direction rules between namespaces/assemblies, not generic-parameter constraints
  or access modifiers on a single type. ArchUnitNET handles the direction-of-dependency rules
  (Domain -> nothing, Application/Api -> no EF Core, nothing outside Infrastructure -> Infrastructure
  internals) well; I wouldn't force it to do the rest just to keep one tool in play. See
  `DependencyRuleTests.cs` vs. `StructuralRuleTests.cs`.

- **ArchUnitNET needs the target assembly explicitly loaded to assert against it.** My first
  version of the "Application doesn't depend on EF Core" rule only loaded this solution's own five
  assemblies into the `Architecture`. The rule reported *zero* violations - not because there
  weren't any, but because `Microsoft.EntityFrameworkCore.DbContext` wasn't a real node in the
  loaded model, just an unresolved external reference ArchUnitNET couldn't match a namespace
  selector against. I had to add `typeof(DbContext).Assembly` to the `LoadAssemblies(...)` call
  itself. I only caught this because I did the deliberate-negative-check step below - without it,
  I'd have shipped a rule that always passes, for the wrong reason, which is worse than not having
  it. This is the headline "watch out" from this whole project.

- **`Program.cs` using top-level statements means the Api project has no public type to anchor a
  `typeof()` against for ArchUnitNET.** The generated `Program` class and the `OrderResponse`
  record both end up in the *global* namespace, not `RepositoryPattern.Api`. I had to select Api's
  types by assembly (`ResideInAssemblyMatching(".*RepositoryPattern.Api.*")`), not namespace - and
  even then, `ResideInAssembly("RepositoryPattern.Api")` (exact match) matched nothing because
  ArchUnitNET compares against the assembly's full name including version/culture/token, not its
  simple name. Worth knowing before reaching for assembly-based selectors.

- **Running integration and comparison test collections in parallel against the same real
  Postgres database is a trap.** xUnit runs different test collections in parallel by default. My
  `OrderRepositoryContractTests` (collection "Database") and `ScenarioEquivalenceTests` (collection
  "Comparisons") each truncate the shared tables before every test. Run in parallel, one
  collection's `TRUNCATE` can wipe rows the other just inserted mid-test, which is exactly what
  happened the first time I ran the full suite - a flaky, data-dependent failure that passed in
  isolation. `[assembly: CollectionBehavior(DisableTestParallelization = true)]` fixed it. This
  isn't specific to the repository pattern, but it's the kind of thing "integration tests against
  real Postgres" quietly implies and the instructions didn't call out.

## Comparison table

Measured from the actual code in this repository (line/file counts via `Get-ChildItem | Measure-
Object -Line`, excluding generated migrations and the shared `ComparisonDbContext`/`Shared`
helper used by options 1-4).

| | Option 1: raw `DbContext` | Option 2: query objects | Option 3: classic repository | Option 4: spec repository | Option 5: constrained hybrid |
|---|---|---|---|---|---|
| Files / lines (the option's own code) | 1 file, 24 lines | 3 files, 41 lines | 4 files, 49 lines | 3 files, 51 lines | 1 file, 20 lines |
| Shared one-time cost it leans on | none | none | none | Application's `ISpecification<T>` (8 files, 106 lines) | Application (8 files, 106 lines) + Infrastructure (8 files, 179 lines) |
| Where the query's contract lives | inline LINQ in the handler | the query object's `ExecuteAsync` | the repository interface's method name | the `ISpecification<T>` object | both: `IOrderRepository`'s method **and** `OpenOrdersForCustomer`, pinned equal by a contract test |
| Returns | aggregates | aggregates | aggregates | aggregates | aggregates |
| Constrained to aggregate roots? | no - any entity | yes - via a thin `AggregateQueryObject<TAggregate>` base, compiler-enforced | yes - via a thin `AggregateRepository<TAggregate>` base, compiler-enforced | yes (`T : class, IAggregateRoot`, compiler-enforced) | yes (`T : class, IAggregateRoot`, compiler-enforced) |
| `SaveChanges`/commit | on `DbContext`, called from the handler | on `DbContext`, called from the handler | on the repository itself | on the repository itself | on a separate `IUnitOfWork` only |
| Unit-testable without a database | **no** - `DbContext` only does real query translation against a real provider | **no** - same reason, one level removed | **no** - interface helps for *mocking* the handler, but the query itself still needs EF+DB to prove correct | **yes** - `OpenOrdersForCustomer.Criteria` is a plain `Expression<Func<Order,bool>>`, compiled and run against in-memory `Order` instances in `SpecificationTests`, no DB | **yes** - same spec, same test, plus a contract test proving the tuned path matches it |
| Ease of tuning one query | rewrite the LINQ in place; nothing stops a second use case from copy-pasting a slightly different version | same, but at least isolated to one class | swap the interface's implementation; still ad hoc inside it | swap the generic `Repository<T>`'s evaluator for everyone, or bypass the abstraction entirely - there's no hook for "just this one query" | add exactly one named method with its own tuned implementation, `GetOpenOrdersForCustomerAsync`, while every other query keeps using the generic spec path |
| What breaks first if the team grows | nothing structural - just discipline, and that's the point proponents of option 1 make | nothing structural either | duplicate near-identical repository methods across aggregates, each written by hand | a generic repository with no governance - nothing stops someone from reaching back into `DbContext` for "just this once" | the ceremony itself: a new aggregate needs an interface, maybe a derived repository class, and DI registration before anyone can query it |

## Where the constrained hybrid loses

- **Raw line count for a single, simple query.** Option 1 is 24 lines and one file. Option 5's
  *own* code is smaller (20 lines), but that's misleading - it's sitting on 106 + 179 = 285 lines
  of Application/Infrastructure scaffolding that options 1-4 don't need at all for this scenario.
  If this app only ever needed this one query, option 1 wins outright and it isn't close.
- **Time to add a second aggregate.** Option 1 and 2 scale by just writing more LINQ against the
  same `DbContext`. The hybrid needs, at minimum, a registration of the open-generic
  `EfRepository<T>` (one-time cost, already paid here) - but the *moment* an aggregate needs even
  one named method, it needs its own interface, its own `internal` class deriving from
  `EfRepository<T>`, and a DI registration line. That's real ceremony per aggregate with special
  needs, and most real systems have more than one.
- **Nobody on this list is actually wrong about correctness.** The `ScenarioEquivalenceTests`
  prove all five options are behaviorally identical for this scenario. The hybrid's benefit is
  entirely about longer-term properties - testability without a database, a named seam for
  tuning, enforcement via architecture tests - not about doing the one thing better *today*. If a
  team doesn't expect to need those properties, the extra ceremony is a cost with no offsetting
  benefit yet.
- **The inheritance-based `EfRepository<T>` base class did create minor friction**, though less
  than expected. `OrderRepository : EfRepository<Order>, IOrderRepository` works cleanly for this
  one case. The friction is structural rather than syntactic: the base class can only expose a
  derived class to *one* narrow extension point (`ComposeWithRawSql`), so if a future aggregate
  needed a different kind of extensibility (say, a bulk-update hook), the base class would need a
  second protected member, and the "only one protected member" discipline starts to erode one
  legitimate case at a time. A composition-based alternative - a named-method repository that
  takes `IRepository<Order>` as a constructor dependency and wraps it, rather than inheriting from
  `EfRepository<Order>` - would avoid ever touching the base class's protected surface, at the
  cost of re-implementing `Add`/`Remove`/`GetByIdAsync` as thin delegating calls in every derived
  repository. I didn't build this second version; for one aggregate it would have been pure
  ceremony with no payoff to show in the post. Inheritance earns its keep here specifically
  because there's exactly one axis of extension (raw SQL) and it's unlikely to need a second one
  for a small number of aggregates - if it ever needs a third narrow exception, that's the signal
  to switch to composition.

## A measured number, not an invented one

Seeding (via `src/Seeder`, Npgsql binary `COPY`, no EF Core in the hot path): **50,000 orders and
~100,000 order lines in 0.9 seconds**, inside the `migrate-and-seed` container, on this machine.

Query timing (busiest seeded customer, 30 open orders out of 50,000 total orders / 2,000
customers; 50 iterations each, first call discarded as a warm-up, measured from a throwaway
console app against the same running Postgres container - not part of the published code):

| Path | Avg per call |
|---|---|
| Spec path (`ListAsync(new OpenOrdersForCustomer(id))`, EF-translated LINQ + `Include`) | 2.87 ms |
| Tuned path (`GetOpenOrdersForCustomerAsync(id)`, `FromSqlInterpolated` + `Include`) | 1.82 ms |

About a 37% reduction for this query, on this data shape, on this machine. Both paths still do
the same `Include`-driven join for `Lines` - the measured difference comes from EF's LINQ
translation/query-plan overhead on the filter+sort, not from avoiding the join itself. This is a
real but modest number for a `WHERE`+`ORDER BY` on an indexed column, and I don't want the post to
oversell it: the win from "one named, tunable seam" isn't usually going to be dramatic on simple
queries like this one. It matters more as the query gets more complex than this (aggregations,
window functions, multi-table joins EF can't express well) - which this post doesn't need to
demonstrate, since the point is the *seam*, not this particular number.

## Cyclomatic complexity (measured after the initial build, for the post's charts)

No complexity tool was already in this toolchain, so I wrote a small Roslyn-based
analyzer (`CSharpSyntaxTree.ParseText` + a `DescendantNodes()` walk) implementing
standard McCabe cyclomatic complexity: 1 per method, +1 per decision point (`if`/
`else if`, loops, `catch`, `case` labels, switch-expression arms, ternaries, `&&`,
`||`, `??`). This is the same counting rule SonarQube/NDepend/Visual Studio Code
Metrics use under the hood - not a bespoke definition. Run against the real source
files, same file scope as the "files / lines" row in the comparison table above
(excluding migrations):

| | Option 1 | Option 2 | Option 3 | Option 4 | Option 5 |
|---|---|---|---|---|---|
| Own-code complexity (sum across its own methods) | 3 | 4 | 5 | 6 | 2 |
| Own-code method count | 1 | 2 | 3 | 3 | 1 |
| The scenario's orchestration method alone (`HandleAsync`/equivalent) | 3 | 2 | 2 | 2 | 2 |

Shared scaffolding, for reference (not comparable to the per-option rows above,
since it's a one-time cost, not paid per scenario): Application's specs/interfaces,
10 total across 8 methods (avg 1.25); Infrastructure's EF plumbing, 22 total across
16 methods (avg 1.38). Nothing in the shared scaffolding is individually complex;
the ceremony cost already documented above is a *count* of small, simple files and
methods, not complexity concentrated in any one of them. `SpecificationEvaluator.Apply`
is the single most complex method in the whole solution at 6 (it has five
conditionals, one per optional spec feature: criteria, includes, ordering direction,
paging, tracking), which is exactly the complexity the hybrid centralizes into one
place instead of letting every hand-written query repeat it.

The one genuinely interesting result: option 1's single method (complexity 3) is
*more* complex than options 2-5's orchestration method (complexity 2 each), and the
difference is exactly the `&&` in `Where(order => order.CustomerId == customerId &&
Order.OpenStatuses.Contains(order.Status))`. Options 2-5 all push that same
filtering decision into a separate, named, independently-tested method or
specification; option 1 doesn't, because it has nowhere separate to put it. The
decision doesn't disappear when you add a seam, it just stops living inline with
the orchestration logic - which is the specification's testability argument from
earlier in this doc, restated as a complexity number instead of a testability claim.

**Decided to leave this out of the published post.** I drafted a chart and a
paragraph around this, then pulled both. The numbers are small (2-6) and mostly
flat, and the one "interesting" result isn't independent evidence of anything -
it's a mechanical artifact of decomposition: moving a condition out of a method
necessarily drops that method's own complexity count by exactly one, regardless
of whether the pattern behind it is good. It would have read as padding a thin
finding into a whole chart, and the testability argument already makes the real
point more directly without needing a second metric to point the same way.

## Does this change the post's thesis?

No, but it sharpens two things:

1. **The honest cost of the hybrid is ceremony-per-aggregate, not raw line count for one query.**
   I'd planned to lead with "look how few lines option 5 needs" - the real comparison is more
   interesting and more honest if it's framed as "cheap once the scaffolding exists, expensive the
   first time," which the table above makes concrete.
2. **"A named repository method, a specification, and a query object are the same idea" needs one
   caveat I hadn't considered going in: only the specification is unit-testable without a
   database**, because it's the only one of the three that's a plain, inert data object rather
   than something that executes a query itself. That's worth a sentence in the post - it's the
   sharpest practical argument for specifications specifically, not just "constrained repositories
   in general."

## Appendix: the one case we don't recommend

The instructions asked that an `IQueryable`-exposing repository not be implemented, only
mentioned here as a snippet for contrast - this is intentionally **not** part of the five
options above or `ArchitectureTests`, specifically because nobody we're aware of actually
recommends it:

```csharp
public interface IOrderRepository
{
    IQueryable<Order> Orders { get; } // leaks EF Core's query provider past the interface
}
```

The problem isn't style, it's that every caller now effectively depends on whatever `DbContext`
is behind `Orders` - the interface stops being a seam at all, since the real contract is
"whatever LINQ-to-Entities can translate," which is an EF Core concept, not an application one.
