---
title: "Vertical Slice vs. Onion Architecture: Let the Requirements Decide"
description: "Onion and vertical slice architecture optimize for different things. A worked product-catalogue and discount-code example, with real file and line counts, shows where each one actually fits."
date: 2026-11-02T09:00:00-04:00
draft: true
cover:
  image: soham-banerjee-h-Cp-u0J-gc-unsplash-1900x1280.jpg
  alt: "Looking up at a large yellow sphere through modern architecture"
  caption: "Photo by [Soham Banerjee](https://unsplash.com/@soham1991?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on [Unsplash](https://unsplash.com/photos/looking-up-at-a-large-yellow-sphere-through-modern-architecture-h-Cp-u0J-gc?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText)"
ShowToc: true
tags: ["dotnet", "csharp", "aspnetcore", "architecture", "efcore"]
categories: ["programming"]
---

The choice between onion architecture and vertical slice architecture usually gets made before anyone's written a line of business logic. You pick a template, or you copy the last project's folder structure, or someone on the team has strong feelings from a conference talk. Nobody sits down and asks what the system actually needs first.

In practice, I've watched vertical slices decay the same way more than once: five slices, five slightly different ways of loading the same `Order`. One slice uses `.Include()` for the lines it needs, another projects straight to a DTO, a third filters out cancelled orders and the other two forget to. Nothing catches that drift, because nothing owns the query except the slice itself. Onion architecture's single repository is tedious to route every read and write through, but it's also the one place that particular kind of inconsistency can't happen by accident. [Jimmy Bogard](https://jimmybogard.com/vertical-slice-architecture/), who popularized the vertical-slice label in .NET in the first place, puts a real condition on the whole style: it "does assume that your team understands code smells and refactoring. If your team does not understand when a 'service' is doing too much to push logic to the domain, this pattern is likely not for you." The slice-duplication problem above is exactly the kind of smell he means: whether a team notices five almost-identical queries and promotes one to shared logic, or lets them drift, is a team-maturity question as much as an architecture one.

MediatR gets a pass in a lot of codebases it hasn't earned, in my experience. I've used it on projects where the endpoint-to-handler mapping was strictly 1:1, there were no pipeline behaviors in sight, and the only thing it bought was an extra file and a `Send()` call standing in for what would have been an equally readable method call. The mistake isn't "small project, skip it; big project, use it," though: plenty of large, well-architected systems never touch a mediator at all, and project size doesn't actually predict whether you need one. The real question is narrower: do you have enough handlers that need the same cross-cutting behavior applied the same way, consistently enough that you'd rather the framework enforce it than trust every PR to remember? If not, the mediator is ceremony no matter how big the codebase around it gets.

That order matters, because both styles go wrong in predictable, opposite ways when they don't fit. Take a thin CRUD app, something that mostly validates input and writes a row. Wrap it in onion architecture and you get an interface for every repository, a DTO for every entity, a mapping step between each layer, and a feature that used to be one file now living across four projects. None of that ceremony protects anything, because there's nothing complex enough to protect. Now take a system with a genuinely rule-heavy domain, pricing, eligibility, stacking limits, the kind of logic that has real invariants. Organize it purely by feature folder, and the same validation logic starts showing up slightly differently in three different slices, because nothing forced it into one place.

Neither failure mode is about one architecture being worse. They're both about using a tool for the wrong job. This post's thesis is that neither onion nor vertical slice architecture is a sensible default. The right choice depends on how much domain complexity you actually have, how your team is organized, how unevenly your features vary in complexity, how concentrated your change is, and whether you expect to pull pieces out into their own services later. Get specific about those five things first, and the architecture question mostly answers itself.

My own leaning, for what it's worth, is that for a typical system I favor organizing at the top level by feature, and reaching for an onion-style structure only inside the features whose domain complexity actually earns it. That's a judgment call I make on individual systems, not a rule I'd apply everywhere, and I'm not claiming most systems should do it this way. Everything below is meant to help you make that call for your own system, not to talk you into mine.

## The two axes: grouping and dependency direction

Here's the distinction that's easy to blur: horizontal architectures group code by technical role, and vertical slices group code by feature. Those are the two options on one axis, grouping. There's a second, separate axis: dependency direction, which is about which layers are allowed to depend on which.

Onion architecture is my example of a horizontal architecture, horizontal (onion) going forward, with clean architecture and hexagonal architecture as close cousins that make broadly the same trade-offs; onion stands in for all three here. It groups by role, domain in the center, then application logic, then infrastructure, then the outermost layer that talks to the outside world, and its defining rule is that dependencies only point inward: infrastructure depends on application, application depends on domain, and domain depends on nothing. That inward-pointing rule is the real difference from old-style n-tier (Data/Business/Presentation) design, which also groups by role but doesn't enforce a particular dependency direction between those layers; n-tier is out of scope for this post otherwise.

Vertical slice architecture groups by feature instead of by role. Each feature, a use case, an endpoint, whatever grain you pick, owns everything it needs: its own request shape, its own validation, its own data access, often in one folder. Grouping and dependency direction are genuinely separate questions, which is why a vertical slice can still have a rich, protected domain model with its own inward dependencies sitting inside the folder; it's just that the folder boundary is drawn by feature, not by role. I'll come back to exactly that choice later.

Picture one feature, say "apply a discount code to an order." In onion architecture, that feature's code is smeared across four rings: a method on the `Order` aggregate in the domain project, a command and handler in the application project, a repository implementation in infrastructure, and an endpoint in the API project. In vertical slice architecture, most of that same feature lives in one folder:

![Diagram comparing the same apply-discount-code feature under both styles. In Onion architecture, the feature is spread across four rings: Api (POST /discount-code), Infrastructure (OrderRepository.cs), Application (ApplyDiscountCodeHandler.cs), and Domain (Order.ApplyDiscountCode()). In vertical slice architecture, the same five files, Endpoint.cs, Request.cs, Validator.cs, Handler.cs, and DiscountRules.cs, sit together in one folder, Features/Orders/ApplyDiscountCode/.](onion-vs-slice-diagram.svg)

Onion optimizes for an isolated, independently testable domain core, consistent conventions applied the same way everywhere, and infrastructure you can swap (a different database, a different message broker) without touching business logic. Vertical slice architecture optimizes for change locality (touch one feature, edit one folder), features that can evolve independently of each other's conventions, and low ceremony for the features that don't need much. Neither of those is "the clean one." They're trade-offs, and which set of trade-offs you want depends on what your system actually looks like.

## The questions that actually decide this

These are the inputs that matter. None of them has a universally right answer; they're about your system.

**How much real domain logic and how many invariants does the system have?** A system where "an order can't be discounted twice with a non-stackable code" is one of a dozen similar rules benefits from a place those rules live and get tested once. A system where most operations are "validate the input, write the row" doesn't have invariants to protect in the first place, and building a home for them anyway is building for problems you don't have.

**How many teams are working on this, and do they need to avoid stepping on each other?** Vertical slices give each feature (and often each team) its own folder, its own blast radius. If one team owns checkout and another owns catalog, slices mean they rarely touch the same files. Onion architecture tends to concentrate everyone in the same handful of projects, which is fine for one team and a source of merge conflicts for several.

**Do your features differ widely in complexity?** A system with both a five-field CRUD form and a multi-step approval workflow has features that want different amounts of structure. Vertical slices let the simple feature stay simple and the complex one carry its own weight, without forcing one global shape on both. Onion architecture tends to apply the same four-project structure to every feature regardless of how much that feature actually needs.

**Is change concentrated in individual features, or does it cut across the whole domain?** If most of your change requests land entirely inside one feature ("add a field to the discount code form"), slices keep that change in one folder. If change tends to ripple across many features at once ("we're renaming `Order` to `Purchase` everywhere"), a shared domain model in one place, which onion architecture gives you by construction, means you make that change once instead of once per slice.

**Is service extraction likely?** A vertical slice is already close to the shape of a standalone service: its own endpoint, its own handler, its own data access. If you expect to pull a feature out into its own deployable later, slices make that extraction closer to a copy-paste. Onion architecture's shared domain and application layers are exactly what makes that kind of split harder, because the thing you're extracting doesn't have a clean boundary yet.

**How familiar is the team with each style?** This is real, but weaker than the others: a team that's deeply fluent in one style will produce better code in that style, at least in the short term, than a team learning a new one under deadline pressure. It's worth weighing, but it shouldn't override what the system actually needs; familiarity is a reason to budget time for ramp-up, not a reason to pick the wrong shape permanently.

## Where each one fits

Vertical slices tend to fit CRUD-heavy systems and workflow-style systems: admin tools, back-office CRUD, orchestration-heavy processes where each step is its own handler. The appeal is that a feature which is genuinely simple gets to stay simple, instead of inheriting four projects' worth of structure it doesn't need.

Onion architecture tends to fit domains where the rules are the hard part: pricing engines, eligibility and compliance logic, anything where a bug in a business rule is expensive and the rule itself is complicated enough to deserve isolated, fast, framework-free tests.

Be honest about the costs on both sides. Vertical slices risk duplicated logic across folders (the same validation rule reimplemented three slightly different ways) and per-slice convention drift, where every feature ends up with its own idea of how a handler should be shaped because nothing's enforcing consistency. Onion architecture risks the ceremony this post opened with: interfaces and mappings for things that don't need them, one feature spread so thin across projects that understanding it means opening four files, and heavy structure wrapped around what's really a simple operation.

## The hybrid, and when it's an answer vs. an excuse

The hybrid is feature-oriented organization at the top level, with an onion-style domain core inside the specific features whose complexity earns it. Most features stay flat, request in, handler, data access, done. The one or two features with real invariants get their own little domain model nested inside their folder.

This is a pragmatic answer when complexity in your system is genuinely lumpy, when most of what you're building is simple and a small number of features are not, and you don't want to pay onion's structural cost everywhere just to get it where you need it. It's a way of avoiding the decision when you reach for it because you can't tell which features need the rigor and which don't, and you're hoping a compromise architecture will paper over not having done that analysis. The hybrid rewards knowing where your complexity actually lives; it doesn't substitute for finding out.

## Vertical slices don't require MediatR

A vertical slice is a folder-and-ownership decision: which files live together, who owns them, where the boundary is drawn. A mediator (MediatR being the common one in .NET) is a dispatch mechanism: how a request gets routed to its handler. Those are separate choices, and much vertical-slice writing treats them as a package deal when they aren't. The companion solution behind this post uses vertical slices with no mediator at all.

Here's a slice with a plain handler class and a minimal API endpoint, from the companion code's product-catalogue feature:

```csharp
// Features/Products/CreateProduct/Handler.cs
public class Handler
{
    private readonly AppDbContext _context;

    public Handler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> HandleAsync(Request request, CancellationToken cancellationToken = default)
    {
        if (await _context.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken))
        {
            throw new DomainException($"A product with SKU {request.Sku} already exists.");
        }

        var product = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Sku = request.Sku,
            Name = request.Name,
            Price = request.Price
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
```

```csharp
// Features/Products/CreateProduct/Endpoint.cs
public static class Endpoint
{
    public static void MapCreateProduct(this IEndpointRouteBuilder app)
    {
        app.MapPost("/products", async (Request request, Handler handler, CancellationToken ct) =>
            {
                var id = await handler.HandleAsync(request, ct);
                return Results.Created($"/products/{id}", new { id });
            })
            .WithValidation(new Validator());
    }
}
```

No `IRequest<T>`, no `IRequestHandler<T, TResponse>`, no runtime dispatch through a mediator pipeline. The endpoint calls the handler directly, through ordinary constructor injection, same as it would call any other service.

What MediatR actually buys you is real: pipeline behaviors (logging, validation, transactions) applied uniformly across every handler without each one repeating the plumbing, and a consistent request/handler shape across a whole codebase, which helps when a team is large enough that consistency has to be enforced by a convention rather than remembered. What it costs is also real: an extra layer of indirection between "I called something" and "here's what ran," a navigation cost in large codebases (jumping from a call site to its handler isn't a direct "go to definition" anymore), and, as of version 13, a license. MediatR 12.x and earlier remain MIT-licensed; starting with version 13, launched July 2, 2025, it's under a commercial license from Lucky Penny Software, the company its creator, Jimmy Bogard, formed to hold it. I'm not attributing any opinion about the library's merits, or about architecture generally, to Bogard here, only the facts of who built it and who owns the commercial edition now; check current terms yourself before budgeting for it.

The companion code gets MediatR's main cross-cutting benefit, request validation applied consistently, through a minimal API endpoint filter instead:

```csharp
// Common/ValidationFilter.cs
public class ValidationFilter<TRequest> : IEndpointFilter
{
    private readonly IValidator<TRequest> _validator;

    public ValidationFilter(IValidator<TRequest> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context);
        }

        var result = await _validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
```

Every endpoint opts in with `.WithValidation(new Validator())`, the same uniform shape MediatR's pipeline behaviors would give you, without a mediator in the middle. A decorator around a handler interface would get you the same result if you preferred that shape. The point isn't that endpoint filters are strictly better. It's that "I want consistent cross-cutting behavior" and "I want a mediator" are two different wants, and you can satisfy the first without the second.

Endpoint filters, decorators, and middleware aren't interchangeable, even though all three can carry a cross-cutting concern, and picking between them is worth doing deliberately rather than by habit. An endpoint filter runs per route, scoped to whichever handlers you explicitly attach it to, which is why it fit validation here. A decorator wraps one specific handler interface and everything that implementation does, which fits a concern (retry logic around one slow dependency, say) that belongs to that handler alone, not the whole app. Middleware runs for every request that reaches the pipeline regardless of route, the right layer for something like request logging or authentication, and the wrong layer for anything that needs to know which handler is about to run. Match the mechanism to the concern's actual scope, not to whichever one happens to be idiomatic in the template you started from.

None of this is an argument for swapping MediatR for a smaller, hand-rolled mediator either. A thinner library still asks the same question an endpoint filter or a decorator answers directly: does this codebase have enough handlers that need the same behavior applied the same way to justify any dispatch indirection at all? If not, trading one mediator for another doesn't remove the ceremony, it just changes whose name is on it. Vertical slice architecture is an organizational decision: which files live together, who owns them. A mediator is an implementation decision: how a call gets from a caller to a handler. The first shouldn't automatically dictate the second.

## Side by side: two features, two styles

The companion solution implements two contrasting features, each one twice: a CRUD-style product catalogue, and a rule-heavy discount-code feature (eligibility, stacking, expiry, usage limits), once in an onion-style layout and once as vertical slices. The counts below come directly from that code, excluding shared infrastructure both styles need regardless (the EF `DbContext`, DI wiring, `Program.cs` endpoint registration).

### Product catalogue (CRUD)

| | Files | Lines | Where the logic lives |
|---|---|---|---|
| Onion | 15 | 197 | `Product` invariants in Domain; `CreateProductCommand`/`Handler` pairs (each with its own interface) in Application; `IProductRepository` implementation and `IProductQueries` in Infrastructure |
| Vertical Slice | 13 | 161 | Each of `CreateProduct`/`UpdateProduct`/`DeleteProduct`/`ListProducts` owns its `Request`, `Validator`, `Handler`, `Endpoint`; EF calls go straight against `AppDbContext` in the handler |

For a feature this simple, the gap is real but modest: two fewer files, 36 fewer lines. Most of onion's extra files here are the `I*Handler` interfaces paired with each concrete handler, interfaces that exist to satisfy the dependency-inversion convention, not because anything needs to be swapped. Onion's ceremony tax on CRUD is real, but it isn't the whole story, which the next feature shows.

### Discount code (eligibility, stacking, expiry, usage limits)

| | Files | Lines | Where the logic lives |
|---|---|---|---|
| Onion | 14 | 316 | `Order.ApplyDiscountCode()` and `DiscountCode` in Domain, with a full aggregate lifecycle (`Draft`/`Submitted` status, `EnsureModifiable`); `ApplyDiscountCodeHandler` in Application; `OrderRepository`/`DiscountCodeRepository` in Infrastructure |
| Vertical Slice | 6 | 118 | `DiscountRules.Apply()`, a static method against plain EF entities, holding the same five rules; `Handler` loads the order and discount code directly via `AppDbContext` |

Here's the Onion version's rule, as a method on the aggregate:

```csharp
// Shop.Onion.Domain/Order.cs
public void ApplyDiscountCode(DiscountCode discountCode, DateTimeOffset nowUtc)
{
    EnsureModifiable();

    if (_appliedDiscounts.Any(d => d.DiscountCodeId == discountCode.Id))
    {
        throw new DomainException($"Discount code {discountCode.Code} has already been applied to this order.");
    }

    if (discountCode.IsExpired(nowUtc))
    {
        throw new DomainException($"Discount code {discountCode.Code} expired on {discountCode.ExpiresAtUtc:u}.");
    }

    if (!discountCode.HasRedemptionsRemaining())
    {
        throw new DomainException($"Discount code {discountCode.Code} has reached its redemption limit.");
    }

    if (Subtotal < discountCode.MinimumOrderAmount)
    {
        throw new DomainException(
            $"Discount code {discountCode.Code} requires a minimum order of {discountCode.MinimumOrderAmount:C}.");
    }

    if (!discountCode.IsStackable && _appliedDiscounts.Count > 0)
    {
        throw new DomainException($"Discount code {discountCode.Code} cannot be combined with another discount.");
    }

    if (_appliedDiscounts.Any(d => !d.IsStackable))
    {
        throw new DomainException("This order already has a non-stackable discount applied.");
    }

    var amount = discountCode.CalculateDiscount(Subtotal);
    _appliedDiscounts.Add(new AppliedDiscount(discountCode.Id, discountCode.Code, discountCode.IsStackable, amount));
}
```

And the vertical-slice version, the same five rules against plain EF entities instead of a domain aggregate:

```csharp
// Features/Orders/ApplyDiscountCode/DiscountRules.cs
public static decimal Apply(OrderEntity order, DiscountCodeEntity discountCode, DateTimeOffset nowUtc)
{
    if (order.Status != OrderStatus.Draft)
    {
        throw new DomainException($"Order {order.Id} cannot be modified once it has been submitted ({order.Status}).");
    }

    if (order.AppliedDiscounts.Any(d => d.DiscountCodeId == discountCode.Id))
    {
        throw new DomainException($"Discount code {discountCode.Code} has already been applied to this order.");
    }

    // ... expiry, usage limit, minimum order amount, stacking checks follow the same shape

    return discountCode.Type switch
    {
        DiscountType.Percentage => Math.Round(subtotal * (discountCode.Value / 100m), 2, MidpointRounding.AwayFromZero),
        DiscountType.FixedAmount => Math.Min(discountCode.Value, subtotal),
        _ => throw new DomainException($"Unknown discount type: {discountCode.Type}.")
    };
}
```

The rules read almost identically. What's different is what's wrapped around them. I put the vertical-slice version's rules in a small static type scoped to that feature folder, not a shared domain project, which is the trade-off this post's brief asked me to make explicit: the Onion version's `Order` carries a full aggregate lifecycle (`Draft`/`Submitted` status transitions, `Submit()`, the `EnsureModifiable` guard reused by every mutating method) that nothing outside this one discount check uses yet. That's a real domain model with real invariants protected independent of any one feature. The vertical-slice version has exactly the invariants this one slice needs and nothing more; if a second feature later needed to enforce "an order can't be touched after submission," that rule would need to be copied into that slice too, or the two slices would need to agree on sharing `DiscountRules` (or a slightly larger shared type), at which point you're partway back to a shared domain model. The file and line counts here aren't purely ceremony versus no ceremony: part of the gap is a genuinely richer, more reusable domain model on one side, and a leaner, feature-scoped one on the other. Which of those you want depends on whether you expect more features to need the same invariants, which is really the first question from this post's checklist again. How much domain logic does the system actually have, and is it concentrated in one feature or shared across several?

{{< aside >}}
**On the if-statement chain:** Both versions above enforce their five rules as a sequence of guard clauses, not a specification pattern or a rules engine. That's deliberate, not an oversight: the rules are fixed at compile time, there are only five of them, and nothing here needs to combine or explain them dynamically. A specification object per rule (`IDiscountRule` with an `IsSatisfiedBy(order, code)` method, composed into a list) would buy independent unit tests per rule and an easier spot to add a sixth, at the cost of a file and an interface per rule, the same ceremony-versus-clarity trade-off this whole post is about. A real rules engine earns its keep when rules are numerous, change without a deploy (business users editing them in a UI, say), or need to be evaluated and explained in combination; none of that is true for "has this discount expired yet."
{{< /aside >}}

## Decision checklist

Answer these for your system. A clean "yes" across most of them points one way; a mixed set points at the hybrid.

1. **Does the system have real invariants worth protecting in code** (not just a database constraint), and are there several of them? Mostly yes → onion, or an onion-style core inside the features that need it.
2. **Are multiple teams working on this concurrently, and do they need to avoid colliding on the same files?** Mostly yes → vertical slices.
3. **Do your features vary widely in complexity** (some are five-field CRUD, others are multi-step workflows)? Mostly yes → vertical slices, so the simple ones stay simple.
4. **Is most change requests concentrated inside single features, rather than rippling across the whole domain?** Mostly yes → vertical slices. Mostly no (change tends to touch many features at once) → onion's shared domain model pays for itself.
5. **Do you expect to extract individual features into their own services later?** Mostly yes → vertical slices, which already look like the shape you'd extract.
6. **Is the team deeply fluent in one style already?** Weight this one lightly against the others, but don't ignore it under a deadline.

Mixed results, some questions pointing one way and some the other, aren't a failure of the checklist. They're the actual signal for the hybrid: organize by feature at the top level, and build an onion-style core inside the specific slices where questions 1 and 4 came back strongly in favor of it.

## Conclusion

Neither architecture is the safe default, and the file-count tables above should make that concrete rather than abstract: onion's extra ceremony on a simple CRUD feature was real but modest, while on a rule-heavy feature the gap was large, and part of that gap was a genuinely richer domain model, not pure overhead. Let your system's actual complexity, team shape, and extraction plans answer the question, and revisit the answer when those things change, because a system's requirements at month one aren't its requirements at year two. Keep the mediator decision separate from both: vertical slices don't require MediatR, onion doesn't forbid it, and "do I want a mediator" is worth asking on its own terms rather than inheriting whichever answer came bundled with the architecture template you started from.

Related reading: [When EF Core Isn't Enough: Adding Dapper Without Breaking Your Domain Model](/posts/ef-core-dapper-light-cqrs/) walks through a worked example of plain handler classes with no mediator, in a different architectural context.

The full companion solution, both the onion and vertical-slice versions of both features, with their tests, is in `companion-code/when-to-use-vertical-slice-architecture/` in this blog's repository.
