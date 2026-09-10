---
title: "Cracking Polly"
description: "Polly v8 replaced policies with resilience pipelines. A practical look at retry, circuit breaker, hedging, and chaos strategies in .NET for 2026 — plus what's changed in the license."
date: 2026-09-09T17:17:44-04:00
draft: false
cover:
  image: kaitlin-dowis-3YnT86K0CdE-unsplash.jpg
  alt: "Parrot"
  caption: "Photo by [Kaitlin Dowis](https://unsplash.com/@kaitlindowis?utm_content=creditCopyText&utm_medium=referral&utm_source=unsplash) on [Unsplash](https://unsplash.com/photos/red-yellow-and-green-parrot-3YnT86K0CdE?utm_content=creditCopyText&utm_medium=referral&utm_source=unsplash)"
tags: ["dotnet", "aspnet", "csharp", "polly"]
categories: ["programming"]
---
## Introduction

Polly is a .NET resilience and transient-fault-handling library. It lets you express strategies like retry, circuit breaker, timeout, rate limiting, hedging, and fallback around any piece of code, so a flaky downstream dependency doesn't automatically become an outage in your own service.

If you used Polly a few years ago and haven't looked at it since, the library you remember is gone. Polly v8 was a ground-up rewrite, built in collaboration with Microsoft, and it changes both the API and the mental model. This post is a practical, current tour of what Polly looks like today: pipelines instead of policies, the built-in HTTP resilience handlers, hedging, chaos engineering support, and the one thing that's changed about the license terms that maintainers of paid products should know about.

## Policies are gone — pipelines are the new unit of composition

In the old Polly (v7 and earlier), you built individual `Policy` objects and combined them with `PolicyWrap`. In v8, that's replaced entirely by the **resilience pipeline**: a single, ordered composition of strategies built with `ResiliencePipelineBuilder`.

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500),
        BackoffType = DelayBackoffType.Exponential
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions())
    .AddTimeout(TimeSpan.FromSeconds(10))
    .Build();

await pipeline.ExecuteAsync(async token =>
{
    // your call to a downstream dependency
}, cancellationToken);
```

Ordering is explicit and semantic: the first strategy added is the outermost one, and moving a timeout across a retry changes what your pipeline actually promises. A pipeline is also meant to be built once and reused: pipelines are thread-safe and designed to be cached, and building a new one per request just throws away the allocation-free execution path v8 was designed around.

The old policy names map fairly directly onto the new strategy builders, which makes migration mostly mechanical: `WaitAndRetryAsync` becomes `AddRetry`, `AdvancedCircuitBreakerAsync` becomes `AddCircuitBreaker` with failure-ratio options, `TimeoutAsync` becomes `AddTimeout`, `BulkheadAsync` becomes `AddConcurrencyLimiter`, `FallbackAsync` becomes `AddFallback`, and `PolicyWrap` disappears entirely because the pipeline itself is the composition. The legacy API still ships in the v8 package, so you don't have to migrate everything in one sitting.

## Dependency injection and named pipelines

For anything beyond a toy example, you'll want to register pipelines at startup rather than build them inline. Polly integrates with `IServiceCollection` for exactly this:

```csharp
services.AddResiliencePipeline("my-pipeline", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions())
        .AddTimeout(TimeSpan.FromSeconds(10));
});

var pipelineProvider = serviceProvider
    .GetRequiredService<ResiliencePipelineProvider<string>>();

ResiliencePipeline pipeline = pipelineProvider.GetPipeline("my-pipeline");
```

This gives you a `ResiliencePipelineProvider` that creates and caches pipelines by name, which is a much more testable pattern than reaching for a static policy instance scattered across your codebase.

## HttpClient gets resilience for free

If most of your Polly usage has always been "wrap an `HttpClient` call so it retries," you don't need to hand-roll a pipeline for that anymore. `Microsoft.Extensions.Http.Resilience`, built on top of Polly v8, ships ready-made resilience handlers for `HttpClient`:

```csharp
services.AddHttpClient("orders-api")
    .AddStandardResilienceHandler();
```

That single line installs the recommended composition for service-to-service HTTP: a rate limiter, a 30-second total timeout, retry with three attempts using exponential jittered backoff that honors `Retry-After`, a circuit breaker, and a 10-second per-attempt timeout in exactly that outermost-to-innermost order. When the defaults don't fit your service, you can override the standard handler's options or drop down to `AddResilienceHandler` to compose something custom.

There's also a hedging variant for latency-sensitive calls. Hedging, introduced in Polly v8, improves request latency by issuing multiple concurrent requests instead of waiting for one to fail before retrying. The standard hedging handler swaps the retry strategy out for a hedging strategy: by default it fires a second request if no response comes back within two seconds, then takes whichever request finishes first, and it uses a pool of circuit breakers, typically keyed by URL authority, so requests don't keep getting hedged against an endpoint that's already unhealthy.

```csharp
services.AddHttpClient("search-api")
    .AddStandardHedgingHandler();
```

Hedging is a genuinely different tool from retry, it trades extra load for lower tail latency, so reach for it on read paths where a duplicate request is cheap and safe, not on anything with side effects.

## Generic pipelines for result-based conditions

Not every failure is an exception. Sometimes "failure" means an `HttpResponseMessage` with a 503, or a domain object indicating a downstream system is unavailable. For that, Polly has generic pipelines:

```csharp
var pipeline = new ResiliencePipelineBuilder<OrderStatus>()
    .AddFallback(new FallbackStrategyOptions<OrderStatus>
    {
        ShouldHandle = new PredicateBuilder<OrderStatus>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutRejectedException>(),
        FallbackAction = _ => Outcome.FromResultAsValueTask(
            new OrderStatus { Status = "Unknown" })
    })
    .AddRetry(new RetryStrategyOptions<OrderStatus>
    {
        MaxRetryAttempts = 3
    })
    .Build();
```

`PredicateBuilder<T>` is the mechanism for saying "treat this exception, or this particular result shape, as something worth retrying or falling back on". It's the same idea as the old `Handle<T>().OrResult(...)` chains, just consolidated into one type.

## Chaos engineering, built in

One of the more interesting additions in the Polly v8 line is first-class support for chaos engineering strategies, deliberately injecting faults, latency, or outcome substitution into a pipeline so you can find out whether your resilience configuration actually works before production finds out for you. Rather than writing throwaway code to simulate a slow dependency, you can add a chaos strategy directly into a pipeline (typically gated to non-production environments or a small traffic percentage) and watch your retries, circuit breakers, and fallbacks respond to real, controlled failure. It's a natural complement to the rest of the pipeline model: the same composition mechanism you use for defense now doubles as your test harness for that defense.

## A licensing note for 2026

One change worth flagging if you maintain a commercial product on top of Polly: the project has adopted the Open Source Maintenance Fee model. Starting November 16, 2026, companies earning at least $20,000 USD from a product or project that uses Polly are asked to pay a $20/month maintenance fee, regardless of how many projects use it, to help fund the ongoing work of keeping the project secure and maintained; the source code itself stays free and openly licensed. Individuals, students, hobbyists, and organizations under that revenue threshold owe nothing. It's a low bar as these things go, but worth putting on your radar if you're doing a license/dependency audit this year.

## Migrating an existing codebase

If you're sitting on a codebase full of `Policy.Handle<T>().WaitAndRetryAsync(...)`, you don't need to rewrite everything at once. A reasonable path:

1. **Add `Polly.Core`** alongside your existing `Polly` reference; the legacy policy API and the new pipeline API can coexist.
2. **Start with your `HttpClient` calls.** Swap `AddPolicyHandler` usages for `AddStandardResilienceHandler()` where the defaults are close enough, and a hand-built `AddResilienceHandler` where they're not.
3. **Migrate non-HTTP policies pipeline-by-pipeline**, using the mapping above (`WaitAndRetryAsync` → `AddRetry`, and so on), rather than trying to preserve `PolicyWrap` structures, the pipeline builder's ordering already gives you that composition.
4. **Register pipelines centrally** with `AddResiliencePipeline` so they're easy to find, test, and eventually replace the last of the legacy policy code.

## Wrapping up

The core promise of Polly hasn't changed since it first showed up in .NET projects: don't let a transient failure in one dependency become a permanent failure in yours. What's changed in v8 is that this is no longer something you bolt on with ad-hoc policy objects; it's a composable pipeline with a clear execution order, first-class DI support, ready-made HTTP defaults from Microsoft, hedging for latency-sensitive paths, and chaos strategies to verify the whole thing actually works. If you've been putting off the v7-to-v8 migration, 2026 is a good year to make the switch. The legacy API isn't getting the new features, and the standard resilience handlers alone are usually worth the move for HTTP-heavy services.

## References

- [Migration guide from v7 to v8 — Polly official docs](https://www.pollydocs.org/migration-v8.html)
- [Polly Resilience (Polly v8): Timeouts, Retries, Circuits, Bulkheads, Hedging](https://www.dotnet-guide.com/tutorials/cloud-native/polly-resilience/)
