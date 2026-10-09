using BenchmarkDotNet.Attributes;

namespace SpanBenchmarks;

/// <summary>
/// The pre-C# 14 baseline: this project is compiled with LangVersion 13, so
/// the implicit array-to-span conversion is not available at all -- every
/// path here is the hand-written .AsSpan() call that C# 14 makes optional.
/// Run this alongside SpanBenchmarks.CSharp14's Explicit_AsSpan /
/// Implicit_Conversion pairs to confirm LangVersion has no effect on the
/// generated code -- it's a compile-time-only construct.
/// </summary>
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class PreCSharp14Benchmarks
{
    private static readonly int[] Numbers = Enumerable.Range(0, 1000).ToArray();

    [Benchmark]
    public Span<int> PreCSharp14_ArrayToSpan()
    {
        Span<int> span = Numbers.AsSpan();
        return span;
    }

    [Benchmark]
    public ReadOnlySpan<int> PreCSharp14_ArrayToReadOnlySpan()
    {
        ReadOnlySpan<int> span = Numbers.AsSpan();
        return span;
    }
}
