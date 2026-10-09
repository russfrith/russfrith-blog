# Spanning the Gap with an Array of Options — companion code

Benchmarks for the post ["Spanning the Gap with an Array of Options: C# 14's Implicit Span Conversions"](../../content/posts/spanning-the-gap-with-an-array-of-options/index.md).

There are two projects, deliberately kept separate because they need different `LangVersion` settings:

- **`SpanBenchmarks.CSharp14`** (`LangVersion 14`) — the main suite. Contains:
  - `OverviewBenchmarks.cs` — the Span<T> fundamentals, independent of C# 14: `SubstringVsSpanBenchmarks` / `SkipTakeVsSpanBenchmarks` (allocating APIs vs. their `Span`-based equivalents), `SpanVsListBenchmarks` (per-element iteration overhead, no slicing involved), and `StackallocVsHeapBenchmarks` (a small scratch buffer on the stack vs. the heap).
  - `ImplicitVsExplicitBenchmarks.cs` — `ArrayToSpanBenchmarks` / `ArrayToReadOnlySpanBenchmarks` / `CovariantArrayToReadOnlySpanBenchmarks` / `ExtensionReceiverBenchmarks` — implicit conversion vs. manual `.AsSpan()`, to prove the C# 14 conversions cost nothing. Only `ExtensionReceiverBenchmarks` covers a call site that's actually new in C# 14; the others are no-regression checks on conversions that already existed.
- **`SpanBenchmarks.CSharp13`** (`LangVersion 13`) — `PreCSharp14Benchmarks`, the same array-to-span conversions compiled under the older language version, to confirm `LangVersion` itself has no effect on the generated code.

Each class carries `[MemoryDiagnoser]` (allocations) and, where relevant, `[DisassemblyDiagnoser]` (JIT-emitted code size/instructions).

## Run locally

Requires the .NET 10 SDK.

```bash
dotnet run --project SpanBenchmarks.CSharp14/SpanBenchmarks.CSharp14.csproj -c Release
dotnet run --project SpanBenchmarks.CSharp13/SpanBenchmarks.CSharp13.csproj -c Release
```

Pass `--filter '*ClassName*'` to run a single benchmark class. Reports land in `BenchmarkDotNet.Artifacts/results/` under each project.

## Run in Docker

```bash
docker build -t span-benchmarks .
docker run --rm -v "$PWD/results:/out" span-benchmarks
```

The container builds both projects, runs both benchmark suites, and copies the BenchmarkDotNet artifacts (markdown/HTML/CSV reports, plus disassembly output) to `./results` on the host.

Benchmark numbers from inside a container will vary depending on the host's CPU and how much else is running — treat absolute nanosecond figures as illustrative. The allocation counts (0 B) and code-size comparisons are the numbers that should hold regardless of hardware.
