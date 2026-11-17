---
title: "In Defense of the Repository: Constrained, Specification-Driven, and Tunable"
description: "Why a narrow, enforced repository is still worth having over coding directly against EF Core's DbContext, with a five-way comparison and real numbers"
date: 2026-11-17T16:28:17-05:00
draft: false
cover:
  image: grok-books-on-shelves.jpg
  alt: "Books in a library"
  caption: "Image created by author using [Grok](https://grok.com/)"
ShowToc: true
tags: ["dotnet", "csharp", "efcore", "postgresql", "architecture"]
categories: ["database"]
---

Many, dare I say most, .NET developers argue against the Repository Pattern when using Entity Framework Core (EF Core). A `DbContext` is already a unit of work. A `DbSet<T>` is already an `IQueryable<T>`, which is already a repository and a specification mechanism rolled into one, courtesy of LINQ. That's the honest, strong version of the argument against adding a repository layer on top of EF Core, and the people making it aren't wrong about the common failure mode: a repository with `GetAll()`, a dozen near-duplicate `GetByXAsync()` methods, or one that just hands back `IQueryable<T>` and calls it abstraction. That kind of repository adds a file; it doesn't do anything a plain `DbSet` doesn't already do.

My position is narrower than "repositories are good." It's that a repository earns its keep specifically when it's a *narrow, enforced seam*, not a general-purpose wrapper around `DbContext`. I'm borrowing "seam" from Michael Feathers' *Working Effectively with Legacy Code*, where he defines it as [a place where you can alter behavior in a program without editing in that place](https://martinfowler.com/bliki/LegacySeam.html), which is exactly what a named repository method, a specification, or a query object gives you over inline LINQ: somewhere to swap the real implementation without touching every caller. A narrower contract is easier to read, test, tune, and enforce than the full surface of `DbContext` itself. Without that enforcement, nothing stops a query from reaching straight past the aggregate root: a `DbSet<OrderLine>` looks like any other simple, convenient LINQ query, and it's just as easy to write one that mutates a line's quantity directly, outside `Order`'s own invariants, as it is to write one that respects them. A named repository method, a specification object, and a per-use-case query object are all the same underlying idea: a named query contract with a swappable implementation behind it. The question worth arguing about isn't "repository or no repository," it's "how narrow is the contract, and what enforces it." This post builds the same small scenario five ways, including the two "no repository" versions written the way their proponents would actually write them, and reports where the constrained version wins, where it loses, and where the numbers are more modest than the pitch.

Everything below is pulled directly from <!-- LINK: companion-code/repository-pattern-ef-core-specifications --> a runnable .NET 10 / PostgreSQL 17 solution. I'll flag every number as measured, and where I measured something only once, on one machine, I'll say so rather than imply more than it proves.

## The honest case against repositories

Here's the strongest version of the argument, stated the way I think its proponents would want it stated, not strawmanned:

- **`DbContext` already is a unit of work.** `SaveChangesAsync()` commits everything staged against it in one transaction. Wrapping it in `IUnitOfWork.SaveChangesAsync()` doesn't add a capability; it adds a layer of indirection over a capability EF Core already gives you for free.
- **`DbSet<T>` already is a repository, and a better one than most hand-rolled ones.** It supports `Add`, `Remove`, `Find`, and the full range of LINQ querying, all backed by an API surface that's been tested across more production workloads than a bespoke interface ever will be.
- **LINQ-to-Entities already is a specification mechanism.** `Where(predicate)` composed with `Include(nav)` and `OrderBy(key)` is exactly what a specification object re-expresses with more ceremony: a filter, a shape, an order, expressed as data instead of as an inline expression. It's arguably a more elegant mechanism than any specification could be, too: satisfying `IQueryable<T>`'s provider contract (translating arbitrary expression trees into SQL) is a genuinely hard problem, but once a provider exists, the queries written against it are about as simple as query syntax gets. A hand-rolled `ISpecification<T>` only has to be as expressive as the cases its author anticipated; `IQueryable<T>` already handles whatever the provider author chose to support, which is most of what SQL can do.
- **An extra layer is an extra thing to maintain**, for a case most apps don't actually hit: swapping the ORM. Nobody swaps EF Core for something else mid-project, and if they did, the repository wouldn't save them, because the repository's `ISpecification<T>` implementation is usually still built around `IQueryable`-shaped thinking anyway. <!-- VERIFY + LINK -->

<!-- VERIFY + LINK --> I want to cite exactly where EF Core's own documentation makes this case explicitly, rather than paraphrase from memory; I'm leaving this marker because I don't want to guess a URL.

I think this argument is correct about what it's correct about. It's an argument against *unconstrained* repositories: ones with no enforced boundary, where `IQueryable<T>` or `DbSet<T>` itself leaks past the interface, or where the "repository" is really just `DbContext` with a thinner name. It is not, in my view, an argument against *any* seam between application code and persistence code. The disagreement is about where the premises land, not about who's reading the EF Core docs correctly.

<!-- YOUR VOICE -->

## Five ways to do the same thing

The scenario: **get a customer's open orders, submit the first draft one.** "Open" means `Draft` or `Submitted`, defined once on the `Order` aggregate itself so nothing downstream has to redefine it:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Domain/Order.cs:1-18 -->
```csharp
namespace RepositoryPattern.Domain;

public class Order : IAggregateRoot
{
    // The statuses an order is still "in flight" under. Defined once here so
    // both the domain's own checks and query specifications stay in sync.
    public static readonly OrderStatus[] OpenStatuses = [OrderStatus.Draft, OrderStatus.Submitted];

    private readonly List<OrderLine> _lines = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines;

    public bool IsOpen => OpenStatuses.Contains(Status);
```

I built the same scenario five times: the two "skip the repository" versions written the way their proponents would write them, a classic named-method repository, a specification-driven repository, and the constrained hybrid this post argues for.

**Option 1: raw `DbContext`.** The simplest of the five, and the one the honest case above describes:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Comparisons/Option1_RawDbContext/OpenOrdersScenario.cs:1-29 -->
```csharp
using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Comparisons.Shared;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option1_RawDbContext;

// Option 1: DbContext used directly in the handler, written the way its
// proponents recommend it - no repository, no query object, EF Core's LINQ
// provider and change tracking used as-is. This is the simplest of the five.
public class OpenOrdersScenario(ComparisonDbContext dbContext)
{
    public async Task<Order?> GetOpenOrdersAndSubmitOneAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var openOrders = await dbContext.Orders
            .Include(order => order.Lines)
            .Where(order => order.CustomerId == customerId && Order.OpenStatuses.Contains(order.Status))
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderToSubmit = openOrders.FirstOrDefault(order => order.Status == OrderStatus.Draft);
        if (orderToSubmit is null)
            return null;

        orderToSubmit.Submit();
        await dbContext.SaveChangesAsync(cancellationToken);

        return orderToSubmit;
    }
}
```

**Option 5: the constrained hybrid.** Same scenario, against the actual production `IOrderRepository` and `IUnitOfWork`, not a stand-in built for this comparison:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Comparisons/Option5_ConstrainedHybrid/SubmitOpenOrderHandler.cs:1-25 -->
```csharp
using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Comparisons.Option5_ConstrainedHybrid;

// Option 5: the constrained hybrid this post defends. Unlike options 1-4,
// this isn't a smaller stand-in built for the comparison - it's the actual
// production IOrderRepository and IUnitOfWork from Application/Infrastructure,
// wired up the same way the Api project wires them up.
public class SubmitOpenOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
{
    public async Task<Order?> HandleAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var openOrders = await orderRepository.GetOpenOrdersForCustomerAsync(customerId, cancellationToken);

        var orderToSubmit = openOrders.FirstOrDefault(order => order.Status == OrderStatus.Draft);
        if (orderToSubmit is null)
            return null;

        orderToSubmit.Submit();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return orderToSubmit;
    }
}
```

Read those two side by side and the handler bodies are nearly identical. The difference is entirely in what each one is built on top of, not in what it does. That's the whole argument in a nutshell: options 1 and 5 look almost the same at the call site, and all the honest cost of option 5 is hidden one layer down, in scaffolding that doesn't exist for option 1 at all.

Here's the comparison, measured from the actual code in the repository (file and line counts via `Get-ChildItem | Measure-Object -Line`, excluding generated migrations and the `Shared`/`ComparisonDbContext` helper options 1-4 lean on). All five return aggregates, never an `IQueryable` or a DTO, so that row isn't worth a column of its own:

| | Option 1: raw `DbContext` | Option 2: query objects | Option 3: classic repository | Option 4: spec repository | Option 5: constrained hybrid |
|---|---|---|---|---|---|
| Files / lines (the option's own code) | 1 file, 24 lines | 3 files, 41 lines | 4 files, 49 lines | 3 files, 51 lines | 1 file, 20 lines |
| Constrained to aggregate roots? | no, any entity | yes (via a thin `AggregateQueryObject<TAggregate>` base, compiler-enforced) | yes (via a thin `AggregateRepository<TAggregate>` base, compiler-enforced) | yes (`T : class, IAggregateRoot`, compiler-enforced) | yes (`T : class, IAggregateRoot`, compiler-enforced) |
| `SaveChanges` / commit | on `DbContext`, called from the handler | on `DbContext`, called from the handler | on the repository itself | on the repository itself | on a separate `IUnitOfWork` only |
| Unit-testable without a database | no | no | no | yes | yes |

Four more criteria matter here, but their real answers are full sentences, not short table cells, so here they are per option instead of wrapped into a narrow column:

**Option 1: raw `DbContext`**
- Contract lives inline in the LINQ, in the handler.
- Tuning: rewrite the LINQ in place; nothing stops a second use case from copy-pasting a slightly different version.
- What breaks first: nothing structural, just discipline.
- Testability: no, `DbContext` only translates queries against an actual provider.

**Option 2: query objects**
- Contract lives in the query object's `ExecuteAsync`.
- Tuning: same as option 1, but at least isolated to one class.
- What breaks first: nothing structural either.
- Testability: no, same reason as option 1, one level removed.

**Option 3: classic repository**
- Contract lives in the repository interface's method name.
- Tuning: swap the interface's implementation; still ad hoc inside it.
- What breaks first: duplicate near-identical repository methods across aggregates.
- Testability: no. Mocking the interface lets you test the *handler* in isolation, but the query itself still needs EF Core and a real database to prove correct.

**Option 4: spec repository**
- Contract lives in the `ISpecification<T>` object.
- Tuning: swap the generic repository's evaluator for everyone, or bypass the abstraction entirely; there's no hook for "just this one query."
- What breaks first: a new aggregate needs its own hand-written implementation class, since there's no open-generic registration doing that for free; and there's still no hook for tuning just one query.
- Testability: yes. `OpenOrdersForCustomer.Criteria` is a plain `Expression<Func<Order,bool>>`, compiled and run against in-memory objects, no database involved.

**Option 5: constrained hybrid**
- Contract lives in both: `IOrderRepository`'s method **and** `OpenOrdersForCustomer`, pinned equal by a contract test.
- Tuning: add exactly one named method with its own tuned implementation; every other query keeps using the generic spec path.
- What breaks first: the ceremony itself. A new aggregate needs an interface, maybe a derived class, and a DI registration before anyone can query it.
- Testability: yes, same spec and the same test as option 4, plus a contract test proving the tuned path returns identical results.

Notice the row I almost led with and then didn't: "files / lines." Option 5's own code is the smallest in the table, 20 lines. That's a misleading way to frame it. It's sitting on top of 285 lines of shared scaffolding that options 1-4 don't need at all for a single query. If this app only ever needed this one query against this one aggregate, option 1 wins outright, and it isn't close. The honest framing is "cheap once the scaffolding exists, expensive the first time," not "fewer lines." The same shape shows up more clearly as a picture than as a table row:

![Horizontal stacked bar chart of lines of code per option. Option 1, raw DbContext: 24 lines, all its own. Option 2, query objects: 41 lines, all its own. Option 3, classic repository: 49 lines, all its own. Option 4, specification repository: 51 lines of its own plus 106 lines of shared Application scaffolding, 157 total, with no Infrastructure reuse since its repository is a stand-in built for the comparison. Option 5, constrained hybrid: 20 lines of its own plus the same 106 lines of shared Application scaffolding plus 179 lines of shared Infrastructure scaffolding only it reuses, 305 total, since it's the actual production repository.](lines-of-code-comparison.svg)

## The key insight, and its caveat

A repository method, a specification, and a query object are the same idea wearing different clothes: a named contract for a query, with a swappable implementation behind the name. `GetOpenOrdersForCustomerAsync` is a name. `OpenOrdersForCustomer` is a name. `GetOpenOrdersForCustomer.ExecuteAsync` is a name. All three say "here's what this query means," not "here's how to write any query you want."

The caveat worth calling out: **only the specification is unit-testable without a database**, and the reason is structural, not incidental. A query object and a named repository method are both *code that executes a query*; proving they're correct means running them against something that can actually execute LINQ-to-Entities or SQL, which means a real provider and (for anything beyond the most trivial translation) a real database. A specification is different in kind: it's an inert data object whose `Criteria` is a plain `Expression<Func<T, bool>>`. You can `.Compile()` it and run it against in-memory objects, because it's not an executing query at all, it's a description of one:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Tests/Application/SpecificationTests.cs:12-38 -->
```csharp
    [Fact]
    public void OpenOrdersForCustomer_criteria_matches_draft_and_submitted_only()
    {
        var customerId = Guid.NewGuid();
        var spec = new OpenOrdersForCustomer(customerId);
        var predicate = spec.Criteria!.Compile();

        var draft = Order.Create(customerId);
        draft.AddLine("Widget", 1, 9.99m);

        var submitted = Order.Create(customerId);
        submitted.AddLine("Widget", 1, 9.99m);
        submitted.Submit();

        var shipped = Order.Create(customerId);
        shipped.AddLine("Widget", 1, 9.99m);
        shipped.Submit();
        shipped.Ship();

        var otherCustomer = Order.Create(Guid.NewGuid());
        otherCustomer.AddLine("Widget", 1, 9.99m);

        Assert.True(predicate(draft));
        Assert.True(predicate(submitted));
        Assert.False(predicate(shipped));
        Assert.False(predicate(otherCustomer));
    }
```

No database, no `DbContext`, no test container. That's a genuine, narrow advantage specifications have over the other two shapes in the table, and it's the sharpest practical argument for specifications *specifically*, not just for "constrained repositories in general."

The caveat on the caveat: this test proves the *predicate's logic* is right. It says nothing about whether EF Core's query translator turns that same expression into the SQL you think it does. Those are different failure modes, and only an integration test against a real provider catches the second one. The contract tests later in this post exist for exactly that gap.

## The design

Four pieces make this version constrained, not just a differently-named copy of an unconstrained one.

**A marker interface restricts what can get a repository at all.** `OrderLine` is part of the `Order` aggregate, but it never gets its own repository, because repository interfaces are generically constrained to `IAggregateRoot`:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Domain/IAggregateRoot.cs:1-5 -->
```csharp
namespace RepositoryPattern.Domain;

// Marker interface. Repository interfaces constrain T to this, so only
// aggregate roots (Order) can ever get a repository, not their children (OrderLine).
public interface IAggregateRoot;
```

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Application/Abstractions/IReadRepository.cs:1-13 -->
```csharp
using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Application.Abstractions;

public interface IReadRepository<T> where T : class, IAggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken);
    Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken);
    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken);
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken);
}
```

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Application/Abstractions/IRepository.cs:1-9 -->
```csharp
using RepositoryPattern.Domain;

namespace RepositoryPattern.Application.Abstractions;

public interface IRepository<T> : IReadRepository<T> where T : class, IAggregateRoot
{
    void Add(T entity);
    void Remove(T entity);
}
```

No `GetAll()`. No `Update()`, since a tracked aggregate's changes are already staged; calling `Update` on it would be a no-op dressed up as an action. No `SaveChanges()` on the repository itself, that lives on a separate unit of work:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Application/Abstractions/IUnitOfWork.cs:1-9 -->
```csharp
namespace RepositoryPattern.Application.Abstractions;

// Repositories never save; only a unit of work commits. That keeps "stage a
// change" and "commit the transaction" as separate decisions, which matters
// once a use case touches more than one aggregate.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

And no `IQueryable<T>` or `Expression` returned to a caller anywhere on these interfaces: `GetByIdAsync`, `FirstOrDefaultAsync`, `ListAsync`, `AnyAsync`, `CountAsync` all return the aggregate itself, or a primitive. This is the actual difference from the straw-man repository the honest case against repositories is arguing against. A repository with `IQueryable<T> Orders { get; }` isn't a seam, it's `DbContext` with a different name:

```csharp
public interface IOrderRepository
{
    IQueryable<Order> Orders { get; } // leaks EF Core's query provider past the interface
}
```

I'm not implementing that version; nobody I'm aware of actually recommends it. It's here only to show what this post is *not* defending. The problem isn't the style of the method signature, it's that every caller now depends on whatever `DbContext` is behind `Orders`. The interface stops being a contract at all; the real contract becomes "whatever LINQ-to-Entities can translate," which is an EF Core concept wearing an application-layer name.

**The specification is its own small contract**, no projections, criteria and shape only:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Application/Specifications/ISpecification.cs:1-18 -->
```csharp
using System.Linq.Expressions;

namespace RepositoryPattern.Application.Specifications;

// Our own, minimal specification contract: criteria, includes, ordering, paging,
// a tracking flag. No projections - a spec always describes how to fetch an
// aggregate, never a DTO shape.
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Skip { get; }
    int? Take { get; }
    bool IsPagingEnabled { get; }
    bool AsNoTracking { get; }
}
```

Paging requires an ordering, enforced in the base class rather than left as a convention:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Application/Specifications/Specification.cs:31-39 -->
```csharp
    protected void ApplyPaging(int skip, int take)
    {
        if (OrderBy is null && OrderByDescending is null)
            throw new InvalidOperationException("Paging requires an ordering to be applied first.");

        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
```

A named spec is just that base class, specialized:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Application/Specifications/OpenOrdersForCustomer.cs:1-16 -->
```csharp
using RepositoryPattern.Domain;

namespace RepositoryPattern.Application.Specifications;

// The behavioral definition of "a customer's open orders". The repository's
// tuned, hand-written-SQL path is asserted to return exactly the same rows,
// in exactly the same order, as this spec - see the contract tests.
public sealed class OpenOrdersForCustomer : Specification<Order>
{
    public OpenOrdersForCustomer(Guid customerId)
        : base(order => order.CustomerId == customerId && Order.OpenStatuses.Contains(order.Status))
    {
        AddInclude(order => order.Lines);
        ApplyOrderByDescending(order => order.CreatedAt);
    }
}
```

**One evaluator turns a spec into an EF Core query.** This is the only place `Include`, `Where`, `OrderBy`, and `AsNoTracking` get called against a spec; specs themselves never see EF Core:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Infrastructure/Persistence/SpecificationEvaluator.cs:1-31 -->
```csharp
using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Application.Specifications;

namespace RepositoryPattern.Infrastructure.Persistence;

// The one place that knows how to turn an ISpecification<T> into an EF Core
// query. Specs themselves stay free of EF Core concepts; this class is where
// Include(), Where(), OrderBy() and AsNoTracking() actually get called.
internal static class SpecificationEvaluator
{
    public static IQueryable<T> Apply<T>(IQueryable<T> query, ISpecification<T> specification) where T : class
    {
        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
            query = query.OrderBy(specification.OrderBy);
        else if (specification.OrderByDescending is not null)
            query = query.OrderByDescending(specification.OrderByDescending);

        if (specification.IsPagingEnabled)
            query = query.Skip(specification.Skip!.Value).Take(specification.Take!.Value);

        if (specification.AsNoTracking)
            query = query.AsNoTracking();

        return query;
    }
}
```

**`EfRepository<T>` is concrete, not abstract, registered once as an open generic**, so an aggregate with no named queries needs no class of its own:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Infrastructure/Persistence/EfRepository.cs:1-47 -->
```csharp
using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Application.Specifications;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Infrastructure.Persistence;

// Concrete, not abstract: an aggregate with no named queries needs no class
// of its own - it's registered as an open generic (see ServiceCollectionExtensions).
// An aggregate with named methods derives from this and adds them (see OrderRepository).
//
// Deliberately NOT abstract, and deliberately exposing no DbSet/IQueryable/DbContext
// as protected members - see ComposeWithRawSql, the one narrow exception, and
// FINDINGS.md for why.
internal class EfRepository<T>(OrderingDbContext dbContext) : IRepository<T> where T : class, IAggregateRoot
{
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Set<T>().FindAsync([id], cancellationToken);

    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken)
        => await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken)
        => await ApplySpecification(specification).ToListAsync(cancellationToken);

    public async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken)
        => await ApplySpecification(specification).AnyAsync(cancellationToken);

    public async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken)
        => await ApplySpecification(specification).CountAsync(cancellationToken);

    public void Add(T entity) => dbContext.Set<T>().Add(entity);

    public void Remove(T entity) => dbContext.Set<T>().Remove(entity);

    private IQueryable<T> ApplySpecification(ISpecification<T> specification)
        => SpecificationEvaluator.Apply(dbContext.Set<T>(), specification);

    // The one protected member the base class exposes: a derived, named-method
    // repository (OrderRepository) can compose a hand-written SQL fragment with
    // Include()/ordering from a spec, without ever seeing a bare DbSet or IQueryable.
    protected IQueryable<T> ComposeWithRawSql(FormattableString sql, ISpecification<T>? specification = null)
    {
        IQueryable<T> query = dbContext.Set<T>().FromSqlInterpolated(sql);
        return specification is null ? query : SpecificationEvaluator.Apply(query, specification);
    }
}
```

That `ComposeWithRawSql` method is the base class's only extension point, and it earned its current shape the hard way. My first attempt exposed a plain `protected IQueryable<T> ApplySpecification(...)`, spec in, query out, which is just a renamed `DbSet` the moment a derived class wants to start a query from scratch instead of from a spec. `ComposeWithRawSql` forces the one legitimate use (hand-written SQL, optionally finished off with a spec's includes and ordering) and makes a bare "give me the queryable" call structurally impossible without actually supplying a SQL fragment. An architecture test pins both its name and its uniqueness down, so a second escape hatch can't quietly show up later without the build noticing.

**Inheritance versus composition.** `OrderRepository : EfRepository<Order>, IOrderRepository` works cleanly for the one aggregate that needs a named method here. The friction is structural rather than syntactic: the base class can only expose one narrow extension point before "only one protected member" starts eroding, one legitimate exception at a time. A composition-based alternative, a named-method repository that takes `IRepository<Order>` as a constructor dependency and wraps it instead of inheriting from `EfRepository<Order>`, would avoid touching the base class's protected surface at all, at the cost of re-implementing `Add`/`Remove`/`GetByIdAsync` as thin delegating calls in every derived repository. I didn't build that second version; for one aggregate it would have been pure ceremony with nothing to show for it. Inheritance earns its keep here specifically because there's exactly one axis of extension and it's unlikely to need a second one across a small number of aggregates. If a third narrow exception ever shows up, that's the signal to switch to composition, not to add a second protected member.

## Tuning one query

The generic spec path is correct and consistent, but it isn't free to tune per-query; changing `SpecificationEvaluator` changes every query that goes through it. The constrained hybrid's actual tuning story is: add exactly one named method, with its own implementation, while every other query keeps using the generic path. `IOrderRepository` declares the one method this post tunes:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Application/Abstractions/IOrderRepository.cs:1-11 -->
```csharp
using RepositoryPattern.Domain;

namespace RepositoryPattern.Application.Abstractions;

public interface IOrderRepository : IRepository<Order>
{
    // The one named, tuned query this post defends: it has an EF Core
    // FromSqlInterpolated implementation, and a behaviorally identical
    // specification (OpenOrdersForCustomer) that the contract tests pin it to.
    Task<List<Order>> GetOpenOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken);
}
```

`OpenOrdersForCustomer`, shown above, is the method's *behavioral definition*. `OrderRepository` implements the method itself with `FromSqlInterpolated` instead of the LINQ the spec would otherwise generate:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Infrastructure/Persistence/OrderRepository.cs:1-35 -->
```csharp
using Microsoft.EntityFrameworkCore;
using RepositoryPattern.Application.Abstractions;
using RepositoryPattern.Domain;

namespace RepositoryPattern.Infrastructure.Persistence;

internal sealed class OrderRepository(OrderingDbContext dbContext) : EfRepository<Order>(dbContext), IOrderRepository
{
    // The tuned path: hand-written SQL instead of the two-table join EF would
    // otherwise generate from the spec's Include(). OpenOrdersForCustomer (the
    // spec) is this method's behavioral definition - the contract tests assert
    // the two return identical rows in identical order.
    //
    // The ORDER BY deliberately lives here, as a LINQ call after Include(),
    // not inside the raw SQL string above. An ORDER BY written inside a
    // FromSqlInterpolated string is not preserved once Include() composes a
    // join on top of it - EF treats the raw SQL as an unordered subquery for
    // that join, so the embedded ordering is silently dropped from the final
    // query. This contract test caught exactly that: rows came back in
    // whatever order the join happened to produce, not the order the SQL
    // text asked for. ThenBy(order => order.Id) is still a real tiebreaker on
    // top of that - the spec path gets one from EF's own Include() handling,
    // so this needs to match it for two orders tied on CreatedAt.
    public async Task<List<Order>> GetOpenOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var query = ComposeWithRawSql($"""
            SELECT * FROM orders
            WHERE customer_id = {customerId} AND status IN (0, 1)
            """);

        return await query.Include(order => order.Lines)
            .OrderByDescending(order => order.CreatedAt).ThenBy(order => order.Id)
            .ToListAsync(cancellationToken);
    }
}
```

A tuned method and its spec can drift apart silently if nothing checks them against each other, so a contract test runs both against the same seeded data and asserts identical rows in identical order:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/Tests/Infrastructure/OrderRepositoryContractTests.cs:19-63 -->
```csharp
    [Fact]
    public async Task Spec_path_and_named_method_path_return_identical_orders_in_identical_order()
    {
        using var scope = fixture.Services.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var customerId = Guid.NewGuid();

        // A short delay between inserts, purely so these two orders have
        // genuinely different CreatedAt values for a meaningful "newest
        // first" check. Correctness here no longer depends on it: both
        // paths now order by CreatedAt descending then Id as a real LINQ
        // call (see OrderRepository.GetOpenOrdersForCustomerAsync), so even
        // a tied CreatedAt would still agree on order.
        var draft = Order.Create(customerId);
        draft.AddLine("Widget", 1, 9.99m);
        orderRepository.Add(draft);
        await Task.Delay(5);

        var submitted = Order.Create(customerId);
        submitted.AddLine("Gadget", 2, 4.50m);
        submitted.Submit();
        orderRepository.Add(submitted);
        await Task.Delay(5);

        var shipped = Order.Create(customerId);
        shipped.AddLine("Gizmo", 1, 19.99m);
        shipped.Submit();
        shipped.Ship();
        orderRepository.Add(shipped);

        var otherCustomersOrder = Order.Create(Guid.NewGuid());
        otherCustomersOrder.AddLine("Widget", 1, 9.99m);
        orderRepository.Add(otherCustomersOrder);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var viaSpec = await orderRepository.ListAsync(new OpenOrdersForCustomer(customerId), CancellationToken.None);
        var viaNamedMethod = await orderRepository.GetOpenOrdersForCustomerAsync(customerId, CancellationToken.None);

        Assert.Equal(viaSpec.Select(order => order.Id), viaNamedMethod.Select(order => order.Id));
        Assert.Equal(2, viaSpec.Count);
        Assert.All(viaSpec, order => Assert.True(order.IsOpen));
    }
```

This test failed on me, intermittently, with the exact symptom you'd expect from a tie: the two orders coming back in reversed order between the spec path and the tuned path. The obvious theory was that `OpenOrdersForCustomer` only orders by `CreatedAt` and says nothing about what should happen when two rows tie on it.

The theory didn't survive contact with a diagnostic. I wrote a throwaway console app that prints full-precision timestamps for both orders and both query results, and the two `CreatedAt` values weren't close, let alone tied: 400 milliseconds apart, no ambiguity about which came first. The tuned path still returned them in the wrong order anyway. The real bug was in `GetOpenOrdersForCustomerAsync` itself: its original version wrote `ORDER BY created_at DESC` *inside* the `FromSqlInterpolated` string, then chained `.Include(order => order.Lines)` onto the result, and that ordering doesn't survive. EF composes the raw SQL as an unordered subquery to perform the join `Include()` needs, so whatever order the SQL text asks for is advisory at best once a collection include is layered on top of it.

The fix is already in the code above: the `ORDER BY` moved out of the SQL string entirely and became a real LINQ call after `.Include()`, `.OrderByDescending(order => order.CreatedAt).ThenBy(order => order.Id)`, which guarantees EF adds the ordering to the outermost composed query instead of a subquery that's free to be reordered. The `ThenBy(order => order.Id)` earns its place too: the spec path still gets an implicit secondary sort on the primary key from EF's own `Include()` handling, so the tuned path needs the same tiebreaker to agree with it on the days `CreatedAt` really does tie. This is a sharper cost than I expected going in. An `ORDER BY` written inside hand-written SQL that's later composed with `Include()` isn't just missing a tiebreaker, it isn't a trustworthy order at all, and the only real fix is to never rely on it there in the first place.

**The measured numbers.** Seeding, via a standalone Npgsql binary `COPY` with no EF Core in the hot path: 50,000 orders and roughly 100,000 order lines in 0.9 seconds, inside the `migrate-and-seed` container, on this machine. Query timing, for the busiest seeded customer (30 open orders out of 50,000 total orders across 2,000 customers), 50 iterations each, first call discarded as a warm-up, measured from a throwaway console app against the same running Postgres container, not part of the published code:

| Path | Avg per call |
|---|---|
| Spec path (`ListAsync(new OpenOrdersForCustomer(id))`, EF-translated LINQ + `Include`) | 2.87 ms |
| Tuned path (`GetOpenOrdersForCustomerAsync(id)`, `FromSqlInterpolated` + `Include`) | 1.82 ms |

About a 37% reduction, on this query, on this data shape, on one machine. Both paths still run the same `Include`-driven join for `Lines`; the gap comes from EF's LINQ translation and query-plan overhead on the filter and sort, not from avoiding the join. I don't want to oversell this. It's a modest number for a `WHERE` plus `ORDER BY` over an indexed column, and the win from having a named, tunable seam isn't usually going to be dramatic on a query this simple. It should matter more as a query gets more complex than this one (aggregations, window functions, joins EF Core can't express cleanly), which this post doesn't need to demonstrate, because the point of the comparison is the seam existing at all, not this specific number.

## Enforcing it

A design like this is only as good as what stops someone from quietly reaching back into `DbContext` the next time a deadline is tight. Two kinds of rule do that here, for two different kinds of violation.

**Dependency direction, with ArchUnitNET.** Domain depends on nothing else in the solution; Application and Api don't depend on EF Core; nothing outside Infrastructure depends on Infrastructure's persistence internals:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/ArchitectureTests/DependencyRuleTests.cs:12-28 -->
```csharp
    [Fact]
    public void Domain_depends_on_nothing_else_in_the_solution()
    {
        Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Domain")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Application"))
            .AndShould().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Infrastructure"))
            .AndShould().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore"))
            .Check(Architecture);
    }

    [Fact]
    public void Application_does_not_depend_on_entity_framework_core()
    {
        Types().That().ResideInNamespaceOrBelow("RepositoryPattern.Application")
            .Should().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("Microsoft.EntityFrameworkCore"))
            .Check(Architecture);
    }
```

`Comparisons` is deliberately exempt from the EF Core rule, since options 1-4 need to reference it directly to exist at all; that exemption is documented right next to the rule it would otherwise trip. A real app wouldn't need this exemption at all: it ships one implementation, not four kept around for comparison, so nothing outside `Infrastructure` would ever need direct EF Core access.

The rule that always passed is the lesson worth remembering from this project. The first version of `Application_does_not_depend_on_entity_framework_core` reported zero violations from the day I wrote it, and I nearly took that as a sign the rule worked. It didn't; `Microsoft.EntityFrameworkCore.DbContext` wasn't a real node in ArchUnitNET's loaded model yet, just an unresolved external reference nothing could match a namespace selector against. The fix was loading EF Core's own assembly into the architecture explicitly:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/ArchitectureTests/Architectures.cs:7-24 -->
```csharp
internal static class Architectures
{
    // Loaded once and shared by every rule below. Api is a top-level-statement
    // minimal API with no public type of its own to anchor a `typeof`, so it's
    // loaded by assembly name instead.
    public static readonly Architecture Full = new ArchLoader()
        .LoadAssemblies(
            typeof(RepositoryPattern.Domain.IAggregateRoot).Assembly,
            typeof(RepositoryPattern.Application.Abstractions.IRepository<>).Assembly,
            typeof(RepositoryPattern.Infrastructure.ServiceCollectionExtensions).Assembly,
            typeof(RepositoryPattern.Comparisons.Shared.ComparisonDbContext).Assembly,
            Assembly.Load("RepositoryPattern.Api"),
            // Loaded explicitly so EF Core's own types are full members of the
            // model - otherwise a "depends on Microsoft.EntityFrameworkCore"
            // rule has nothing concrete to match against.
            typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly)
        .Build();
}
```

I only caught it because the plan called for a deliberate negative check: temporarily make the Api project call `OrderingDbContext` directly, confirm the rule actually fails, then revert. Running that check is what surfaced the assembly-loading gap; without it, the rule would have shipped green for the wrong reason, which is worse than not having the rule at all, since a rule that always passes still *looks* like protection.

**Structural rules, with plain reflection.** Generic-parameter constraints, access modifiers, and "exactly one protected member of this shape" aren't naturally dependency-direction questions, so they're checked with reflection instead of forcing ArchUnitNET to do a job it isn't built for:

<!-- src: companion-code/repository-pattern-ef-core-specifications/src/ArchitectureTests/StructuralRuleTests.cs:53-80 -->
```csharp
    [Fact]
    public void EfRepository_exposes_no_DbSet_or_DbContext_as_a_protected_member_and_only_one_IQueryable_returning_helper()
    {
        var infrastructureAssembly = typeof(ServiceCollectionExtensions).Assembly;
        var efRepositoryType = infrastructureAssembly.GetType("RepositoryPattern.Infrastructure.Persistence.EfRepository`1")
            ?? throw new InvalidOperationException("EfRepository<T> was not found - did it get renamed or moved?");

        var protectedMembers = efRepositoryType
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.IsFamily)
            .ToList();

        Assert.All(protectedMembers, member =>
        {
            Assert.DoesNotContain("DbSet", member.ReturnType.Name);
            Assert.DoesNotContain("DbContext", member.ReturnType.Name);
        });

        var queryReturningMembers = protectedMembers.Where(member => member.ReturnType.Name.Contains("IQueryable")).ToList();

        // The one narrow exception this post allows: ComposeWithRawSql, which
        // composes a hand-written SQL fragment with an optional spec and
        // returns a query, for a derived named-method repository to finish
        // with Include()/ToListAsync(). Nothing else protected is allowed to
        // hand back a query.
        Assert.Single(queryReturningMembers);
        Assert.Equal("ComposeWithRawSql", queryReturningMembers[0].Name);
    }
```

The generic constraint itself (`where T : class, IAggregateRoot`) is already enforced at compile time; the reflection test against it is really a regression guard against someone *removing* the constraint later, not a runtime check catching something the compiler would have missed.

Two short notes worth knowing before you copy either tool into your own project: running integration tests and comparison tests in parallel against the same real Postgres database is a trap, since parallel test collections each truncating shared tables mid-test is a real, reproducible flaky failure, not a hypothetical one; `[assembly: CollectionBehavior(DisableTestParallelization = true)]` fixed it here. And a minimal API's top-level-statement `Program.cs` leaves no public type in its own namespace to anchor a `typeof()` against, so Api had to be selected by assembly name instead, and even then, an exact assembly-name match failed because ArchUnitNET compares against the full assembly name (version, culture, token included), not the simple one; a pattern match was the fix.

## Where this loses, and when to skip it

The `ScenarioEquivalenceTests` in the companion code prove all five options are behaviorally identical for this one scenario. Nobody on the list above is actually wrong about correctness. The differences are all about longer-term properties: testability without a database, a compiler-enforced aggregate boundary, a named seam for tuning one query. Those properties don't come as a single package, though, so which of the three constrained shapes is worth building depends on which ones you actually need enforced.

One constraint is worth keeping no matter which of these you build: `IAggregateRoot`. It's a marker interface and a generic constraint, nothing more. For a generic repository (options 4 and 5), it lives directly on the interface's own type parameter. For a non-generic shape (a query object or a classic repository, options 2 and 3), the interface has no type parameter to constrain, but the implementation can still derive from a thin, otherwise-empty base class, `AggregateQueryObject<TAggregate>` or `AggregateRepository<TAggregate>`, that exists only to carry it. Either way, it's what stops a query object or a repository method from quietly being written against `OrderLine` instead of `Order`, reaching past the aggregate root the same way a bare `DbSet<OrderLine>` would. Only option 1, with no class or interface of its own to attach anything to, can't have it. Every other shape in this list can, and should.

**Skip the seam entirely when:**
- You have one aggregate, a handful of queries, and no reason to think that changes. Option 1 is 24 lines and nothing else.
- Nobody on the team is asking for unit tests that run without a database. If integration tests against a real, fast-to-spin-up database are an accepted cost already, specification-level testability doesn't move the needle.

Query objects (option 2) and a classic named-method repository (option 3) sit in between, and they're legitimate stopping points of their own, not just steps toward something better. A query object gets you a named, swappable seam without `ISpecification<T>` at all. Reach for it when you want queries extracted into their own classes, one per use case, but don't care about testing the query logic without a database. A classic repository adds an interface worth mocking at the handler level, plus a thin `AggregateRepository<TAggregate>` base to carry the `IAggregateRoot` constraint, without the specification machinery. Reach for it when you want that governance and that kind of handler-level test, but each query is simple enough that hand-writing its one method is less ceremony than building a spec for it.

**A plain specification repository (option 4) is probably enough when:**
- You're fine without every new aggregate automatically getting a working repository implementation. Because `EfRepository<T>` is registered once as an open generic, any type that implements `IAggregateRoot` gets a complete, working `IRepository<T>` the instant it's declared, no new class, no extra registration. A plain specification repository, built without that open-generic registration, doesn't give you that: each aggregate still needs its own small class wiring up the interface, even though it leans on the same `ISpecification<T>` and evaluator underneath.
- You don't yet need a dedicated tuning seam. Tuning one query is still possible (swap the generic evaluator for everyone, or bypass the abstraction for that one case); it's just not as clean as a named method, an acceptable tradeoff if you don't expect to need it often.

**Reach for the full constrained hybrid (option 5) when:**
- You want the dependency-direction rules (no EF Core above Infrastructure, nothing outside Infrastructure touching its internals) and the aggregate-root boundary enforced by a failing build, not a PR comment someone might not leave.
- A second aggregate is coming, and you can already tell some of its queries will need the same kind of tuning this post gave `GetOpenOrdersForCustomerAsync`: a named method with its own implementation, without disturbing the generic path everything else still uses.
- The team is big enough, or turns over enough, that "we don't do that here" needs to be something the compiler and the test suite say, not something a tribal-knowledge reviewer remembers to catch.

The moment an aggregate needs even one named method, it needs its own interface, its own `internal` class deriving from `EfRepository<T>`, and a DI registration line. That's real, per-aggregate ceremony, and most systems that live long enough end up with more than one aggregate with special needs. Whether that ceremony is worth paying for, and whether the `IAggregateRoot` boundary on top of it is worth paying for too, depends entirely on which of these properties you actually expect to need, not on how the five code samples compare side by side for the one query this post explored.

<!-- YOUR VOICE -->
