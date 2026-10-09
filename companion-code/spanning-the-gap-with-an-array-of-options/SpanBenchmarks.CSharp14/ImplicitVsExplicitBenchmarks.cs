using BenchmarkDotNet.Attributes;

namespace SpanBenchmarks;

/// <summary>
/// The "no regression" proof: array-to-Span assignment vs. the equivalent
/// hand-written .AsSpan() call. Note this conversion already worked under
/// LangVersion 13 too (it's the pre-existing Span{T} operator) -- this
/// benchmark exists to confirm C# 14 didn't change its codegen, not because
/// the conversion itself is new. SpanBenchmarks.CSharp13 repeats the
/// explicit side under LangVersion 13 for the same comparison.
/// </summary>
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class ArrayToSpanBenchmarks
{
    private static readonly int[] Numbers = Enumerable.Range(0, 1000).ToArray();

    [Benchmark(Baseline = true)]
    public Span<int> Explicit_AsSpan()
    {
        Span<int> span = Numbers.AsSpan();
        return span;
    }

    [Benchmark]
    public Span<int> Implicit_Conversion()
    {
        Span<int> span = Numbers;
        return span;
    }
}

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class ArrayToReadOnlySpanBenchmarks
{
    private static readonly int[] Numbers = Enumerable.Range(0, 1000).ToArray();

    [Benchmark(Baseline = true)]
    public ReadOnlySpan<int> Explicit_AsSpan()
    {
        ReadOnlySpan<int> span = Numbers.AsSpan();
        return span;
    }

    [Benchmark]
    public ReadOnlySpan<int> Implicit_Conversion()
    {
        ReadOnlySpan<int> span = Numbers;
        return span;
    }
}

/// <summary>
/// Covariant array -> ReadOnlySpan{U}. string[] is covariant with object[]
/// at the array level, and that composes with the existing Span operator to
/// reach ReadOnlySpan{object} without an explicit cast. Like the plain array
/// conversion above, this already compiled under LangVersion 13 -- it's a
/// composition of two pre-existing conversions, not a new one.
///
/// Gotcha found while writing this: the "obvious" manual translation,
/// ((object[])Words).AsSpan(), throws ArrayTypeMismatchException at runtime.
/// .AsSpan() returns Span{T}, and Span{T}'s constructor runtime-checks
/// covariant reference arrays (a Span{object} over a string[] would let you
/// write a non-string object into a string[] and corrupt the heap, so the
/// CLR forbids it). ReadOnlySpan{T} can't write, so it has no such check --
/// but only if you construct it directly rather than routing through
/// Span{T} first. The correct pre-C# 14 manual equivalent is the
/// ReadOnlySpan{T} constructor, not .AsSpan().
/// </summary>
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class CovariantArrayToReadOnlySpanBenchmarks
{
    private static readonly string[] Words = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToArray();

    [Benchmark(Baseline = true)]
    public ReadOnlySpan<object> Explicit_Constructor()
    {
        ReadOnlySpan<object> span = new ReadOnlySpan<object>((object[])Words);
        return span;
    }

    [Benchmark]
    public ReadOnlySpan<object> Implicit_Conversion()
    {
        ReadOnlySpan<object> span = Words;
        return span;
    }
}

/// <summary>
/// Extension-receiver resolution. Prior to C# 14, `Numbers.SumValues()`
/// simply did not compile -- there was no conversion path from int[] to
/// the `this ReadOnlySpan{int}` receiver. The comparison here is against
/// the manual `Numbers.AsSpan().SumValues()` call that used to be required.
/// </summary>
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class ExtensionReceiverBenchmarks
{
    private static readonly int[] Numbers = Enumerable.Range(0, 1000).ToArray();

    [Benchmark(Baseline = true)]
    public long Explicit_AsSpan()
    {
        return Numbers.AsSpan().SumValues();
    }

    [Benchmark]
    public long Implicit_Conversion()
    {
        return Numbers.SumValues();
    }
}
