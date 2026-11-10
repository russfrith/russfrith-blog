---
title: "When 11 Breaks 8"
description: "What you gain, and what quietly changes, when you leave .NET 8 for .NET 11"
date: 2026-11-10T14:09:29-05:00
draft: false
cover:
  image: 11-ball-and-8-balls.jpg
  alt: "A rack of fourteen 8-balls with a single red 11 in the middle"
  caption: "Image created by author using [Grok](https://grok.com/)"
ShowToc: true
tags: ["dotnet", "csharp", "aspnetcore"]
categories: ["programming"]
---

.NET 8 and .NET 9 reached the end of support today. .NET 11 shipped today too, alongside .NET 10 as the new LTS. If you're still on 8 or 9, you no longer have a choice about moving, only a choice of where you land.

Some of what hurt you on 8 gets fixed in 11. Some of what worked fine on 8 quietly stops working the way you expect, and that second kind is worth more of your attention than the changelog gives it: a few of those changes break the build and tell you immediately, a few crash on startup, and a few compile, pass your tests, and just behave differently. That last group is the one that costs you money, because nothing tells you it happened.

Everything below uses a small orders-service sample I built for this post. It isn't a real production app, and I'm not pretending it is; it exists to carry realistic-looking code through a migration, nothing more. I compiled and ran every clip for real, in Docker, against the official `mcr.microsoft.com/dotnet/sdk` images for 8.0, 10.0, and 11.0.

{{< aside >}}
The public `11.0` SDK image was still tagged RC3 (`11.0.100-rc.3.26522.148`, built 2026-11-06) when I ran these clips, even though today is the official GA date. I'm noting it here rather than pretending otherwise. Everything below should carry over to the GA bits unchanged, but if you see different behavior on the final release, that's why.
{{< /aside >}}

## The class migration

Start with the part of the orders service that decides how much a payment costs to process.

### Closed hierarchies: the exhaustiveness you don't get for free

C# 15 added closed hierarchies, which let a base type declare that it owns the complete list of subtypes. I covered the broader motivation for this, exhaustive switches and the kind of 2 a.m. page that an unhandled case produces, in [The Case You Forgot to Handle](/posts/the-case-you-forgot-to-handle/), so I won't re-explain it here. The short version for this migration: it's one of the few features in this post with no .NET 8 equivalent at all, because the compiler support for it doesn't exist before .NET 11.

```csharp
decimal Fee(Payment payment) => payment switch
{
    Card c         => c.Amount * 0.029m,
    PayPal p       => p.Amount * 0.034m,
    BankTransfer b => b.Amount * 0.008m,
};

closed record Payment(decimal Amount);
record Card(decimal Amount) : Payment(Amount);
record PayPal(decimal Amount) : Payment(Amount);
record BankTransfer(decimal Amount) : Payment(Amount);
```

One thing worth knowing before you lean on this: dropping a case doesn't fail the build. It's a warning (`CS8509`), not an error, and the switch still throws `SwitchExpressionException` at runtime if you hit the missing case. Closed hierarchies get you a compiler that *knows* your switch is incomplete; they don't automatically turn that into a hard stop unless you also treat warnings as errors.

**On .NET 8:** not feasible. There's no workaround that gives you the same compiler-enforced exhaustiveness; you're back to the discipline of a `_ => throw` arm and hoping someone notices.

### Extension indexers

The order line lookup used to be a method. Now it can be `[]`.

```csharp
var lines = new List<OrderLine>
{
    new("SKU-1", 2),
    new("SKU-2", 5),
    new("SKU-3", 1),
};

Console.WriteLine(lines["SKU-2"].Quantity); // 5

record OrderLine(string Sku, int Quantity);

static class OrderLineExtensions
{
    extension(List<OrderLine> lines)
    {
        public OrderLine this[string sku] =>
            lines.Find(l => l.Sku == sku)
            ?? throw new KeyNotFoundException($"No line for {sku}");
    }
}
```

**On .NET 8:** workaround. You get the same lookup as an extension *method* (`lines.BySku("SKU-2")`), just not the indexer syntax. Nobody's blocked, it just reads a little worse.

### Labeled break and continue

The old way to escape two nested loops is a flag variable. I actually wrote and ran both versions to see whether the new IDE0410 analyzer catches the flag-variable pattern the way the docs suggest. In my test, it didn't; it only fired on a `goto`-based version of the same logic, with exactly this diagnostic:

```
Program.cs(16,13): warning IDE0410: Use labeled jump statement (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0410)
```

Old version, no warning from the analyzer in my test:

```csharp
bool found = false;
foreach (var batch in batches)
{
    foreach (var sku in batch)
    {
        if (sku == "BLOCKED-SKU") { found = true; break; }
    }
    if (found) break;
}
```

New version:

```csharp
scan:
foreach (var batch in batches)
{
    foreach (var sku in batch)
    {
        if (sku == "BLOCKED-SKU") { found = true; break scan; }
    }
}
```

Both print the same result. The new version just says what it means instead of coordinating two loops through a side variable.

**On .NET 8:** workaround. Flag variable or `goto`, same as always.

### Collection expression arguments

Collection expressions got a way to pass constructor-style arguments inline, using a `with(...)` clause as the first element:

```csharp
string[] skuBatch = ["SKU-1", "SKU-2", "SKU-3"];

List<string> pending = [with(capacity: skuBatch.Length * 2), .. skuBatch];
HashSet<string> orderCodes = [with(StringComparer.OrdinalIgnoreCase), "ORD-1", "ord-1"];

Console.WriteLine(pending.Capacity);   // 6
Console.WriteLine(orderCodes.Count);   // 1
```

That's a capacity hint for a batch you know the size of in advance, and a case-insensitive set for order codes, both built inline instead of through a separate constructor call.

**On .NET 8:** workaround. `new List<string>(skuBatch.Length * 2) { ... }` and `new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ... }` do the same job, just not inline with the collection literal.

### Can the .NET 11 SDK compile this against net8.0?

I tried targeting `net8.0` with `<LangVersion>latest</LangVersion>` under the .NET 11 SDK, for every feature above. The syntax-only features compiled clean: extension indexers, labeled break/continue, and collection expression arguments all build fine against `net8.0`. Closed hierarchies and union types don't, because they need new BCL members that only ship in `net11.0`:

```
error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsClosedTypeAttribute..ctor'
error CS0518: Predefined type 'System.Runtime.CompilerServices.IUnion' is not defined or imported
error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.UnionAttribute..ctor'
```

So if you're not ready to move the runtime yet but want the newer C# syntax in your editor, some of it is available early. The two features that actually change what the type system can express are not.

## ASP.NET Core

The orders API gets two new validation hooks in .NET 11, plus a response type that didn't exist before.

### Async validation on a single property

Validating that a SKU exists used to mean either a synchronous `ValidationAttribute` faking an async call, or doing the check by hand inside the handler. `AsyncValidationAttribute` lets the attribute do it properly:

```csharp
public sealed class SkuExistsAttribute : AsyncValidationAttribute
{
    private static readonly HashSet<string> KnownSkus = new() { "SKU-1", "SKU-2", "SKU-3" };

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
        => throw new InvalidOperationException("Use IsValidAsync.");

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken); // stand-in for a real async catalog lookup
        var sku = (string?)value;
        return KnownSkus.Contains(sku ?? "")
            ? ValidationResult.Success
            : new ValidationResult($"SKU '{sku}' does not exist.");
    }
}

public record OrderRequest([SkuExists] string Sku, int Quantity);
```

With `builder.Services.AddValidation()` wired up, posting an unknown SKU returns a real 400:

```json
{"title":"One or more validation errors occurred.","status":400,"errors":{"Sku":["SKU 'SKU-999' does not exist."]}}
```

**On .NET 8 and 10:** workaround. You can do the same async lookup by hand inside the minimal API handler before calling `Results.Ok`; you just don't get it as a reusable, declarative attribute.

### Async validation across an object

`IAsyncValidatableObject` does the same thing for checks that need more than one property, like an order total against its line items:

```csharp
public record OrderRequest(List<OrderLine> Lines, decimal Total) : IAsyncValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
        => throw new InvalidOperationException("Use ValidateAsync.");

    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // stand-in for a real async price/tax lookup
        var computed = Lines.Sum(l => l.Quantity * l.UnitPrice);
        if (computed != Total)
        {
            yield return new ValidationResult(
                $"Total {Total} does not match line items, which sum to {computed}.",
                [nameof(Total)]);
        }
    }
}
```

One gotcha I hit writing this: `IAsyncValidatableObject` extends `IValidatableObject`, so you still have to implement the synchronous `Validate` method, even if all it does is throw. A mismatched total comes back as a 400 with a message naming exactly which field is wrong.

**On .NET 8 and 10:** workaround, same as the attribute case: do the check by hand in the handler.

### A union as a response type

C# 15's union types flow straight into minimal API responses:

```csharp
public record OrderDto(int Id, string Sku);
public record NotFoundError(string Message);
public union OrderLookupResult(OrderDto, NotFoundError);

app.MapGet("/orders/{id}", (int id) =>
    id == 1
        ? new OrderLookupResult(new OrderDto(1, "SKU-1"))
        : new OrderLookupResult(new NotFoundError($"Order {id} not found")));
```

`GET /orders/1` returns `{"id":1,"sku":"SKU-1"}`; `GET /orders/99` returns `{"message":"Order 99 not found"}`. The generated OpenAPI document (3.2 by default in .NET 11) describes it as an `anyOf`:

```json
"OrderLookupResult": {
  "type": "object",
  "anyOf": [
    { "$ref": "#/components/schemas/OrderDto" },
    { "$ref": "#/components/schemas/NotFoundError" }
  ]
}
```

**On .NET 8 and 10:** workaround for the mechanism (`Results<Ok<OrderDto>, NotFound>` via `TypedResults` already existed), but the `anyOf` schema and the union-typed convenience are new to 11.

Two clips I'd planned didn't make it in. The validation source generator is documented as discovering types by walking from your minimal API handlers, which raises a real risk: a DTO defined in a separate assembly from the one that calls `AddValidation()` could be skipped. I built a two-project solution to show that gap, and couldn't reproduce it: both a top-level DTO and a nested record validated correctly regardless of which assembly they lived in. Rather than publish a claim I couldn't back up with real output, I'm cutting it. The other casualty is validation localization: the source for it exists, but there's no published package yet, so there's nothing to demonstrate.

## Tripwires

Same code, before and after, sorted by how each one actually fails. LOUD means the build breaks. CRASH means it compiles but fails at startup or runtime. SILENT means it compiles, passes your tests, and quietly does something different. In pool terms: pocketed, knocked off the table, or just nudged a few inches while the table looks the same.

### LOUD: an obsolete Blazor API is gone

Wiring up Blazor form validation the pre-.NET-6 way still compiles today, with a warning:

```csharp
var editContext = new EditContext(model);
editContext.AddDataAnnotationsValidation();
```

On .NET 10 that's `warning CS0618: ... is obsolete: 'Use EnableDataAnnotationsValidation instead.'` and a successful build. On .NET 11, the obsolete overload is gone entirely:

```
error CS1061: 'EditContext' does not contain a definition for 'AddDataAnnotationsValidation' and no accessible extension method 'AddDataAnnotationsValidation' accepting a first argument of type 'EditContext' could be found
```

The fix is a one-line swap to `EnableDataAnnotationsValidation(editContext, serviceProvider)`. This is the cheap kind of breaking change, the compiler stops you before anything ships.

**On .NET 8 and 10:** same (works, with an obsolete warning on 10). **Only on 11:** the build fails.

### CRASH: a background service failure actually crashes the host now

A `BackgroundService` that throws used to fail quietly. The exact same code behaves differently depending only on the target framework:

```csharp
builder.Services.AddHostedService<FailingWorker>();
var host = builder.Build();
await host.RunAsync();
Console.WriteLine("RunAsync completed without throwing.");

class FailingWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(500, stoppingToken);
        throw new InvalidOperationException("Order batch processor crashed.");
    }
}
```

Both versions log the same failure:

```
fail: Microsoft.Extensions.Hosting.Internal.Host[9]
      BackgroundService failed
      System.InvalidOperationException: Order batch processor crashed.
crit: Microsoft.Extensions.Hosting.Internal.Host[10]
      The HostOptions.BackgroundServiceExceptionBehavior is configured to StopHost. ...
```

On .NET 10, that's the end of it: `Application is shutting down...`, then `RunAsync completed without throwing.`, and the process exits **0**. On .NET 11, `RunAsync` rethrows instead, the process exits **134**, and nothing prints that success line. If anything downstream of your service (a container orchestrator, a deploy pipeline, a health check) was trusting a clean exit code to mean "it worked," this is the change that tells it the truth.

**On .NET 8 and 10:** same (failure is swallowed, exit code 0). **Only on 11:** the process actually reports the failure.

### SILENT: EF Core's compatibility-level default changes what SQL you get

EF Core 11 defaults SQL Server's compatibility level to 160 instead of 150. I covered the broader shift in [Upgrading to EF Core 11](/posts/upgrading-to-ef-core-11/); the specific case that surprised me here is `Math.Max`:

```csharp
defaultCtx.Orders.Select(o => Math.Max(o.Id, 100)).ToQueryString();
```

At the new default (160), that translates cleanly to SQL Server 2022's `GREATEST`:

```sql
SELECT GREATEST([o].[Id], 100)
FROM [Orders] AS [o]
```

Pin the old compatibility level explicitly (`UseCompatibilityLevel(150)`) and the translation doesn't downgrade to older SQL, it disappears. EF Core silently returns the raw column instead:

```sql
SELECT [o].[Id]
FROM [Orders] AS [o]
```

No warning, no exception, no log line even with warning-level logging turned on. The query still runs. It just stops doing what the LINQ said.

**On .NET 8 and 10:** not applicable in the same way (older EF Core defaults to the lower compatibility level, so this translation doesn't exist to lose). **Only on 11:** the default itself changes, and pinning the old level has this side effect.

### SILENT: converting a double to a byte

Floating-point-to-integer conversions got saturating behavior for `int`/`uint`/`long`/`ulong` back in .NET 9, which is worth knowing if you're coming from 8. What I didn't expect is that `byte` doesn't follow the same timeline:

```csharp
double[] values = [256001.0, 2147483647.0, double.NaN, -5.0, 200.0];
foreach (var v in values)
{
    byte b = (byte)v;
    Console.WriteLine($"{v} -> {b}");
}
```

| value | .NET 8 | .NET 10 | .NET 11 |
|---|---|---|---|
| 256001 | 1 | 1 | 255 |
| 2147483647 | 255 | 255 | 255 |
| NaN | 0 | 0 | 0 |
| -5 | 251 | 251 | 0 |
| 200 | 200 | 200 | 200 |

.NET 8 and .NET 10 are identical here; both wrap the out-of-range values. Only .NET 11 saturates a `double` down to `byte`. If you convert external input, sensor readings, scores, anything that can land outside 0-255, straight to a `byte` without checking the range first, the correct-looking value you get back depends on which runtime you're standing on.

**On .NET 8 and 10:** same (wraps/truncates). **Only on 11:** saturates.

### Described, not demonstrated

Two more are real but I couldn't put them in a container the way the others are. .NET 11 raises the minimum CPU baseline from x86-64-v1 to x86-64-v2, guaranteeing instruction sets like SSE4.2 and POPCNT that weren't guaranteed before. If your hardware predates that baseline, the runtime refuses to start at all rather than just running slower. Everything running the clips above is well past that line, so there's no failure to show, just the docs' own description of where it applies.

The other is ASP.NET Core 11 emitting OpenTelemetry's HTTP semantic-convention tags by default, where previously you had to opt in. It's a real, documented behavioral change; I just didn't find a specific incident behind it the way I did for the others, so I'm giving it one sentence instead of a clip: if you have dashboards or alerts built against the old tag names, check them before you upgrade, not after.

## LTS or STS

I used to be a straightforward LTS person, and that's the cadence I followed on my own projects for years. That changed when Microsoft extended STS support from 18 months to 24 months, starting with .NET 9. The new rule ties an STS release's end-of-support date to the release after it, STS support now runs for a year past its successor's ship date, which is why .NET 9 ended up sharing its end-of-support date with .NET 8, and .NET 11 shares its end-of-support date with .NET 10 LTS. With STS no longer meaning "less support than LTS," there's less reason to default to LTS just for the support window. These days, I usually start a new solution on whatever's latest rather than defaulting to LTS, and I'll stay on an LTS version for an existing system unless there's a feature on the newer release I actually need.

That's not a universal rule, it depends on who's running the thing. A greenfield project has nothing to lose by starting on 11: no migration cost, and you get support out to the same date as 10 LTS anyway, just with a shorter total window before you have to move again. A production system you're actively maintaining is the case where I lean LTS, because the cost of staying put on 10 is "do nothing for two more years," and the cost of moving to 11 is "do this migration again sooner than you'd like." If you're publishing a library for other people to consume, LTS is close to mandatory; your users are on a mix of versions and you don't get to pick which one for them. And if you're constrained, hardware you can't control, a platform team that moves slowly, an air-gapped environment, the x86-64-v2 baseline above is reason enough on its own to confirm your target hardware before you plan around 11 at all.

## What's covered, and what it costs to stay on 8

| Feature | On .NET 8 | On 10 LTS | Only on 11 |
|---|---|---|---|
| Closed hierarchies | Not feasible | Not feasible | Compiler-enforced exhaustive switch |
| Extension indexers | Workaround (extension method) | Workaround (same) | Indexer syntax |
| Labeled break/continue | Workaround (flag variable/`goto`) | Workaround (same) | Named jump target |
| Collection expression arguments | Workaround (explicit constructor) | Workaround (same) | Inline `with(...)` |
| Union types | Not feasible | Not feasible | New type-system construct |
| Async validation (attribute/object) | Workaround (manual check in handler) | Workaround (same) | Declarative hook via `AddValidation()` |
| Union return type + OpenAPI `anyOf` | Workaround (`Results<T1,T2>` manually) | Workaround (same) | Union-typed convenience, `anyOf` schema |
| OpenAPI 3.2 output | Not feasible (3.1 only) | Not feasible (3.1 only) | 3.2 by default |
| BackgroundService failure visibility | Same (failure swallowed, exit 0) | Same | Host actually reports the failure |
| EF Core SQL Server compat level | N/A (older default, no `GREATEST`) | N/A (same older default) | Default moves to 160 |
| Float-to-`byte` conversion | Same (truncates/wraps) | Same | Saturates |
| x86-64-v2 CPU baseline | Same (runs on v1 hardware) | Same | Required |
| Obsolete Blazor validation API | Same (works, warns on 10) | Warns, still compiles | Removed, compile error |

### What to gate on in CI

A few of the items above won't show up until something is already wrong in production, so check for them before you merge, not after:

- Pin `UseCompatibilityLevel` explicitly if your EF Core queries rely on specific SQL translations, and add a test that asserts `ToQueryString()` for your important queries rather than trusting the default.
- If you convert floating-point values to `byte` or `sbyte` anywhere near a trust boundary (external input, sensor data, scores), add a boundary-value test; don't assume saturating behavior without checking it on your actual target runtime.
- Turn on `-warnaserror` for obsolete-API warnings before you bump your target framework, so a Blazor-style removal shows up as a build failure in CI instead of after the TFM change.
- Add an integration test that asserts a failing `BackgroundService` produces a non-zero exit code, so you catch it if you're relying on the old swallow-the-exception behavior anywhere.
- Run `dotnet build` with analyzers enabled in CI, not just in the IDE; IDE0410 and similar diagnostics won't appear in a CI log otherwise.

## Sources

- [What's new in C# 15](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15)
- [What's new in .NET 11](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview)
- [.NET 11 minimum hardware requirements](https://learn.microsoft.com/en-us/dotnet/core/compatibility/jit/11/minimum-hardware-requirements)
- [ASP.NET Core 11 breaking changes](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/overview)
- [Obsolete Blazor APIs removed](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/11/blazor-obsolete-apis-removed)
- [ASP.NET Core 11 release notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11)
- [IHost.RunAsync/StopAsync now throw on BackgroundService failure](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/11/ihost-runasync-stopasync-throw-backgroundservice-failure)
- [EF Core 11 breaking changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/breaking-changes)
- [dotnet/efcore #30163](https://github.com/dotnet/efcore/issues/30163)
- [Floating-point to integer conversions](https://learn.microsoft.com/en-us/dotnet/core/compatibility/jit/9.0/fp-to-integer)
- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [.NET STS releases supported for 24 months](https://devblogs.microsoft.com/dotnet/dotnet-sts-releases-supported-for-24-months/)
