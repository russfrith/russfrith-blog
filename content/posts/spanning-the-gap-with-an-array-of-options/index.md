---
title: "Spanning the Gap with an Array of Options: C# 14's Implicit Span Conversions"
description: "A Span<T> refresher, where it actually pays off over arrays and List<T>, and what C# 14's first-class span conversions change (and don't)"
date: 2026-10-08T21:04:39-04:00
draft: true
cover:
  image: anders-jilden--N2UXcPBIYI-unsplash.jpg
  alt: "TODO alt text"
  caption: "Photo by [Anders Jildén](https://unsplash.com/@andersjilden?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on [Unsplash](https://unsplash.com/photos/time-lapse-photo-of-cable-bridge-during-golden-hour--N2UXcPBIYI?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText)"
tags: ["csharp", "dotnet"]
categories: ["programming"]
---

This post answers two separate questions. First: why reach for `Span<T>` and `ReadOnlySpan<T>` at all? They've been performance powerhouses since C# 7.2 / .NET Core 2.1: zero-allocation, type-safe views over memory that show up all over modern .NET's hot paths. Second: what does C# 14 change about getting into one? Shipping with .NET 10, it makes spans first-class language citizens, with new implicit conversions that cut ceremony and let the compiler infer types in places it used to give up. That second change is about ergonomics, not speed; it doesn't make `Span<T>` any faster than it already was, it just makes it easier to reach for.

The companion code for this post (the full BenchmarkDotNet suite behind every number below) is on GitHub: [companion-code/spanning-the-gap-with-an-array-of-options](https://github.com/russfrith/russfrith-blog/tree/master/companion-code/spanning-the-gap-with-an-array-of-options).

## Why Span<T> still isn't the default

Despite those performance wins, `Span<T>` and `ReadOnlySpan<T>` remain underused in everyday application code. Library authors and the .NET runtime itself lean on them heavily for parsers, serializers, and high-throughput APIs, but most application developers still reach for arrays, strings, and `List<T>` out of habit, or because the old conversion rules felt awkward enough to just avoid. C# 14's first-class implicit span conversions lower that friction: arrays and strings now flow into span-based parameters without ceremony, overload resolution favors the span overloads more often, and type inference works without extra type arguments. The practical effect is that allocation-free techniques that used to live mostly in framework internals become realistic for ordinary business logic too, which is the thread running through the rest of this post.

> Donald Knuth's famous line is usually trimmed to "premature optimization is the root of all evil," but the fuller quote is the useful part: he's describing programmers who burn time worrying about the speed of code that was never going to be a bottleneck, while the small fraction of cases that actually matter go unmeasured. It's a trap worth naming here because "the compiler can now do this conversion automatically" and "therefore it's faster" are two different claims, and it's easy to let the first slide into the second. `List<T>` is the right default precisely because it's clear and well-understood. Whether `Span<T>` is worth reaching for is a question a profiler answers, not one you decide in advance. The question isn't whether it's faster, but whether this particular call site is one where that speed is worth the reach.

## Overview of Span<T>

`Span<T>` is a `ref struct` representing a contiguous region of memory: a managed array, a `stackalloc`'d stack buffer, a slice of a string, or unmanaged memory. It gives you bounds-checked, type-safe access to that memory without copying it and without a heap allocation.

**What that buys you:**

- **Zero-allocation slicing.** `array.AsSpan(2, 5)` is a view into the same memory. Compare that to `array.Skip(2).Take(5).ToArray()`, which allocates a new array, or `str.Substring(2, 5)`, which allocates a new string every call.
- **Safe stack access.** A span can point at `stackalloc`'d memory, and the compiler enforces that it never outlives the stack frame it came from; something you can't do safely with an array or `Memory<T>`.
- **One signature instead of many.** A method taking `ReadOnlySpan<char>` accepts a string, a `char[]`, a slice of either, or a `stackalloc` buffer: no overloads needed for each.
- **Already everywhere.** `int.TryParse`, `string.Split` (span overloads), `System.Text.Json`, and most modern I/O and parsing APIs lean on spans for exactly this reason.

**The historical friction:** getting into a span meant sprinkling `.AsSpan()` calls everywhere, and library authors had to hand-write parallel array/`Span`/`ReadOnlySpan` overloads to make it convenient for callers. That friction is what C# 14's first-class span types remove (more on that below).

**Span vs. Memory\<T\> vs. ArraySegment\<T\>:** `Span<T>` is a `ref struct`, so it can't be stored in a field of a class, boxed, or used across an `await`. `Memory<T>` is the heap-friendly cousin: it can live in fields and cross async boundaries, and you call `.Span` on it to get a `Span<T>` for the actual work. `ArraySegment<T>` predates both; it's just an array/offset/count triple with none of `Span<T>`'s memory-source flexibility (no `stackalloc`, no string backing). Default to `Span<T>`/`ReadOnlySpan<T>` for synchronous, stack-scoped work; reach for `Memory<T>` when the slice needs to survive an `await` or live in a field.

### Performance: where Span actually wins

Below are four scenarios run with BenchmarkDotNet (`[MemoryDiagnoser]`, 5 measured iterations, 3 warmup, in Docker on a 12th Gen Intel i5-12600K); treat the absolute nanosecond figures as illustrative, but the allocation columns should hold regardless of hardware:

| Scenario | Array/collection-based | Span-based | Allocated (before → after) |
|---|---|---|---|
| String slice: `Substring` vs. `AsSpan` | 5.41 ns | 0.12 ns | 56 B → 0 B |
| Array slice: `Skip().Take().ToArray()` vs. `AsSpan` | 28.03 ns | 0.13 ns | 320 B → 0 B |
| Sum 1000 ints: `List<int>` vs. `Span<int>` | 397.6 ns | 227.3 ns (0.57×) | 0 B both |
| Small scratch buffer: heap array vs. `stackalloc` | 7.74 ns | 7.77 ns (~1.00×) | 0 B both (see note) |

The first two rows are the classic story and the reason `Span<T>` exists: slicing a string or array the traditional way allocates a new copy every time, and `AsSpan()` just doesn't. That's a real, large win, and it has nothing to do with C# 14.

The `List<int>` row isolates a second, smaller effect: even with no slicing or allocation involved, summing through a `Span<int>` beats summing through a `List<int>` by close to 2×. `List<T>`'s indexer goes through a property getter with a bounds check against `Count`, and its enumerator is a bit heavier than a flat memory walk; `Span<T>` indexing is a direct offset the JIT can reason about more aggressively.

The `stackalloc` row is the one with a caveat worth stating plainly: it shows heap and stack allocation costing the same, with **zero bytes allocated for the heap array**. That's not `Span<T>` losing its stack-access advantage; it's the JIT's own stack-allocation-of-non-escaping-objects optimization (available since .NET 7/8) kicking in and promoting `new int[16]` to the stack on its own, because the array never escapes the method. On older runtimes, or with an array that does escape (returned, stored, passed to a method the JIT can't see through), the heap version would show a real allocation and `stackalloc`'s advantage would show up in the numbers. The `Span<T>`-over-`stackalloc` pattern is still the right tool when you need a guarantee that doesn't depend on the JIT noticing: the compiler enforces it, while the JIT optimization is merely opportunistic.

Here's the `List<T>` comparison, since it's the one most likely to be new to readers:

```csharp
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class SpanVsListBenchmarks
{
    private static readonly List<int> NumbersList = Enumerable.Range(0, 1000).ToList();
    private static readonly int[] NumbersArray = NumbersList.ToArray();

    [Benchmark(Baseline = true)]
    public long Sum_List()
    {
        long sum = 0;
        foreach (var value in NumbersList) sum += value;
        return sum;
    }

    [Benchmark]
    public long Sum_Span()
    {
        ReadOnlySpan<int> span = NumbersArray;
        long sum = 0;
        foreach (var value in span) sum += value;
        return sum;
    }
}
```

The full suite, including the `Substring`/`Skip().Take()`/`stackalloc` benchmarks, is in the [companion code](https://github.com/russfrith/russfrith-blog/tree/master/companion-code/spanning-the-gap-with-an-array-of-options).

## Why user-defined conversions never solved the ergonomics problem

Span conversions have existed since C# 7.2, but as ordinary user-defined implicit operators. That matters because user-defined conversions have real limits in the language: the compiler doesn't consider them for extension method receivers, and it doesn't feed them into generic type inference. So you could write `Span<int> s = array;` directly, but the moment an array needed to flow through an extension method or a generic parameter expecting a span, the conversion quietly stopped applying and you were back to typing `.AsSpan()` by hand.

## What's new in C# 14

This section is entirely about compile-time ergonomics; it doesn't change any of the runtime numbers above. The benchmarks later in this post exist to prove exactly that: zero added cost, not extra speed.

Here's the same call site, before and after C# 14:

```csharp
// Before: the extension method has a ReadOnlySpan<T> receiver, so an
// array had to be converted by hand before it could call it.
int[] data = GetData();
long total = data.AsSpan().SumValues();

static class SpanExtensions
{
    public static long SumValues(this ReadOnlySpan<int> values) { /* ... */ }
}
```

```csharp
// After: the array binds to the ReadOnlySpan<T> receiver directly.
int[] data = GetData();
long total = data.SumValues();
```

That second snippet didn't just get shorter: under C# 13 it flat out doesn't compile. `int[]` has no `SumValues` method, and extension lookup has never considered user-defined conversions on the receiver. That's the actual gap C# 14's "first-class span types" feature closes, and it's narrower (and more interesting) than the feature name suggests.

What C# 14 adds is making these conversions participate in **extension method receiver resolution** and **generic type inference**, two places the language has always refused to apply user-defined conversions:

- **Extension method receivers:** an array-typed value can now bind directly to a `this ReadOnlySpan<T>` extension method. Before C# 14, `array.SomeSpanExtension()` simply didn't compile; you had to write `array.AsSpan().SomeSpanExtension()`.
- **Generic type inference:** passing an array to a generic method like `T Length<T>(ReadOnlySpan<T> span)` can now infer `T` from the array's element type in cases that previously failed with "type arguments cannot be inferred." Before C# 14 you had to call `Length<int>(numbers)` explicitly.
- **`Span<T>` → `ReadOnlySpan<T>`:** still implicit, as it's been since C# 7.2; unchanged by this feature.

Plain assignment (`Span<int> s = array;`, and even `ReadOnlySpan<object> s = someStringArray;` via covariance) already compiled under C# 13, through the existing user-defined operators. That part of the story hasn't changed; the new ground is specifically extension receivers and generic inference.

Where C# 14 does add a genuinely new *conversion* at the language level is the plain, non-covariant array case: the spec now defines `T[]` → `Span<T>`/`ReadOnlySpan<T>` as a first-class implicit span conversion rather than relying on the BCL happening to ship the right operator. In practice that's not observable from user code today, since the operator has existed since .NET Core 2.1, but it means the conversion no longer depends on the BCL; it would work even against a hypothetical span-like type with no hand-written operators.

### Proving the new conversions cost nothing

The benchmark goal here is different from the Section 2 numbers: not "is this faster," but "is this free." `[DisassemblyDiagnoser]` confirms the JIT emits identical code for the implicit conversion and the manual `.AsSpan()` call; two separate projects, one on `LangVersion 14` and one on `13`, confirm `LangVersion` itself has no codegen effect.

```csharp
public static class SpanExtensions
{
    public static long SumValues(this ReadOnlySpan<int> values)
    {
        long sum = 0;
        foreach (var value in values) sum += value;
        return sum;
    }
}

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class ExtensionReceiverBenchmarks
{
    private static readonly int[] Numbers = Enumerable.Range(0, 1000).ToArray();

    [Benchmark(Baseline = true)]
    public long Explicit_AsSpan() => Numbers.AsSpan().SumValues();

    [Benchmark]
    public long Implicit_Conversion() => Numbers.SumValues();
}
```

Results from the same Docker run as above:

| Benchmark | Explicit / PreCSharp14 | Implicit (C# 14) | Code Size | Allocated (both) |
|---|---|---|---|---|
| Array → `Span<T>` | 0.0010 ns | 0.0031 ns | 23 B / 23 B | 0 B |
| Array → `ReadOnlySpan<T>` | 0.0 ns | 0.0 ns | 23 B / 23 B | 0 B |
| Covariant array → `ReadOnlySpan<object>` | 0.1315 ns | 0.0012 ns | 23 B / 23 B | 0 B |
| Extension receiver (`SumValues()`) | 229.3 ns | 229.7 ns (1.00×) | 52 B / 52 B | 0 B |
| Pre-C# 14 baseline (`LangVersion 13`): array → `Span<T>` | 0.0000 ns | n/a | 23 B | 0 B |
| Pre-C# 14 baseline (`LangVersion 13`): array → `ReadOnlySpan<T>` | 0.0015 ns | n/a | 23 B | 0 B |

The nanosecond figures on most rows are noise: BenchmarkDotNet flags them as `ZeroMeasurement` because the JIT optimizes the whole method away when the result is unused in a way it can prove. That's the point: there's nothing left to measure. The number that matters is **Code Size**, identical byte-for-byte across implicit vs. explicit and across `LangVersion 14` vs. `13`. The extension-receiver case is the one with real work in it (a 1000-element sum), and it lands within noise of parity (1.00×) with identical code size; the newly-unlocked call site costs nothing extra either.

One surprise from actually running this: the covariant-array benchmark's "explicit" side can't be written as `((object[])Words).AsSpan()`: that throws `ArrayTypeMismatchException` at runtime. `.AsSpan()` returns `Span<T>`, and `Span<T>`'s constructor runtime-checks covariant reference arrays, because a writable `Span<object>` over a `string[]` would let you store a non-string object into a `string[]` and corrupt the heap. `ReadOnlySpan<T>` has no such restriction, but only if you construct it directly (`new ReadOnlySpan<object>(array)`) rather than routing through `Span<T>` first.

## Benefits and impact

- Cleaner, more readable call sites with less `.AsSpan()` ceremony.
- Library authors no longer need a parallel array extension method purely to spare callers that ceremony; the span-receiving extension now binds directly.
- No new runtime cost: the benchmarks above show identical code size and allocations.
- Encourages adoption of span-centric APIs generally, which is where the real performance wins in Section 2's table live.

## Considerations and caveats

A few places this doesn't just work cleanly:

- **Inference isn't universal.** Some generic call sites still can't infer the span type argument from an array argument alone, and you'll need to specify it explicitly.
- **Overload resolution can shift, and this is the real-world risk worth testing for explicitly.** Because array-typed arguments now bind to `Span<T>`/`ReadOnlySpan<T>` overloads and extension methods in more scenarios, Microsoft documents this as an official .NET 10 breaking change [[1]](#references). Two concrete failure modes are worth knowing about: inside an `Expression<TDelegate>` compiled with interpretation, a lambda like `array.Contains(num)` now silently binds to `MemoryExtensions.Contains` instead of `Enumerable.Contains`, which throws at runtime instead of just working [[1]](#references); and calls like a unit-test assertion `Assert.Equal([2], x)` can become genuinely ambiguous between an array overload and a span overload where before there was only one candidate [[2]](#references). Microsoft's documented fix is to force the non-span overload explicitly: cast to `IEnumerable<T>`, call `.AsEnumerable()`, or invoke the extension method as a static call [[1]](#references). A community write-up covering these and two more patterns (covariant-array tie-breaking changing which span type wins, and `MemoryMarshal.Cast` returning an unexpected type) also recommends `OverloadResolutionPriorityAttribute` as a targeted fix for library authors [[2]](#references).
- **Direction matters.** `ReadOnlySpan<T>` → `Span<T>` is still not implicit, for the obvious safety reason: a read-only view can't silently become a writable one.
- **Still a `ref struct`.** Span's usual limitations apply regardless of C# 14: it can't be boxed, stored in a field of an ordinary class, or captured across an `await`.
- **The stackalloc benchmark's caveat applies generally.** Don't assume a heap allocation "doesn't count" just because a non-escaping array happens to get stack-promoted by the JIT in a given case; that's an optimization, not a guarantee, and it can stop applying if the code around it changes.

## Migration guidance

- If you're maintaining a library with a parallel array *extension method* purely so array callers don't have to write `.AsSpan()` first, you can likely drop it once your minimum supported `LangVersion` is 14: the `Span`/`ReadOnlySpan` extension will bind directly. (Plain array/`Span` method overloads on ordinary methods don't need this; arrays already converted implicitly there.)
- If you're an application author, this is mostly a "delete some now-unnecessary `.AsSpan()` calls before extension method invocations" pass: not urgent, but a nice cleanup.
- Before bumping `LangVersion` to 14 on a large existing codebase, run the overload-resolution regression pass described below. It's the one part of this feature that can actually break a build rather than just simplify it.
- Separately from any C# 14 migration: if a hot path is iterating a `List<T>` without needing its growable-collection behavior, converting to an array and summing/iterating via `Span<T>` is worth measuring. Section 2's numbers suggest it's a real, if modest, win on its own.

## Testing methodology

- **Conversion matrix.** Test every claimed implicit path: `T[]`→`Span<T>`, `T[]`→`ReadOnlySpan<T>`, covariant array→`ReadOnlySpan<U>`, `Span<T>`→`ReadOnlySpan<T>`, `string`→`ReadOnlySpan<char>`, both that each compiles without a cast, and that the resulting span references the original backing memory rather than a copy. Worth noting which of these actually require `LangVersion 14`: in my own testing, only the extension-receiver and generic-inference cases did. Direct assignment of an array to a `Span<T>`/`ReadOnlySpan<T>` (including the covariant case) already compiled under `LangVersion 13`.
- **Negative cases.** Confirm conversions that should not be implicit still produce compile errors, such as `ReadOnlySpan<T>`→`Span<T>` and disallowed value-type covariance.
- **Overload resolution regression suite.** Compile an existing codebase under `LangVersion 14` vs. `13` and diff which overloads get picked, especially for extension methods with array/`Span`/`ReadOnlySpan` overloads. Pay particular attention to `Expression<TDelegate>` lambdas compiled with interpretation and any test-assertion helpers that take both array and span overloads [[1]](#references)[[2]](#references).
- **Generic inference edge cases.** Pass arrays and strings directly to generic methods expecting `Span<T>`/`ReadOnlySpan<T>` and confirm which cases infer automatically vs. need an explicit type argument.
- **Cross-target validation.** Run the same test matrix across every TFM you support, including ones on `LangVersion 14` and older ones, to confirm no breakage on downlevel builds.

## Wrap-up

`Span<T>` earns its place by avoiding allocations when slicing strings and arrays, and by being a leaner way to walk a buffer than `List<T>` even when nothing is being sliced. That's the case for reaching for it at all, and it predates C# 14 entirely. First-class span types don't change what `Span<T>` and `ReadOnlySpan<T>` can do; they change how often you have to think about getting into one. The conversions were always logically valid: C# 14 just makes the compiler apply them in the places (extension receivers, generic inference) where the old user-defined-operator approach fell short, at zero added runtime cost.

## References
1. Genevieve Warren. [Breaking change: C# 14 overload resolution with span parameters](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution), _Microsoft Learn_, updated December 3, 2025.
2. Marius Bughiu. [Fix C# 14 Overload Resolution Breaking Change with Spans](https://startdebugging.net/2026/05/fix-csharp-14-overload-resolution-breaking-change-with-spans/), _Start Debugging_, May 19, 2026.

## Further reading
- dotnet/csharplang. [First-class Span Types](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/first-class-span-types.md), the language design proposal behind this feature.
- Bill Wagner. [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14), _Microsoft Learn_.
- Bill Wagner. [Introducing C# 14](https://devblogs.microsoft.com/dotnet/introducing-csharp-14/), _.NET Blog_.
