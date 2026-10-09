using BenchmarkDotNet.Attributes;

namespace SpanBenchmarks;

/// <summary>
/// The real speedup story: allocating APIs (Substring) vs. their
/// zero-allocation Span-based equivalents. This has nothing to do with
/// C# 14 specifically -- it's the reason Span existed in the first place,
/// since C# 7.2 / .NET Core 2.1.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class SubstringVsSpanBenchmarks
{
    private const string Text = "The quick brown fox jumps over the lazy dog";

    [Benchmark(Baseline = true)]
    public string Substring_Traditional()
    {
        return Text.Substring(4, 15);
    }

    [Benchmark]
    public ReadOnlySpan<char> Substring_Span()
    {
        return Text.AsSpan(4, 15);
    }
}

/// <summary>
/// Same story for arrays: Skip().Take().ToArray() allocates a new array;
/// AsSpan() just slices the existing one.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class SkipTakeVsSpanBenchmarks
{
    private static readonly int[] Numbers = Enumerable.Range(0, 1000).ToArray();

    [Benchmark(Baseline = true)]
    public int[] SkipTake_Traditional()
    {
        return Numbers.Skip(100).Take(50).ToArray();
    }

    [Benchmark]
    public ReadOnlySpan<int> SkipTake_Span()
    {
        return Numbers.AsSpan(100, 50);
    }
}

/// <summary>
/// Span vs. List{T} for straight-line iteration over the same data, with no
/// slicing involved. List{T} indexer access goes through a property getter
/// with a bounds check against the list's Count, plus an interface-shaped
/// enumerator when used in a foreach; Span{T} indexing is a direct offset
/// into the backing memory the JIT can reason about more aggressively.
/// This isolates that per-element overhead from the allocation-avoidance
/// story above.
/// </summary>
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
        foreach (var value in NumbersList)
        {
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public long Sum_Span()
    {
        ReadOnlySpan<int> span = NumbersArray;
        long sum = 0;
        foreach (var value in span)
        {
            sum += value;
        }

        return sum;
    }
}

/// <summary>
/// The stack-access benefit: a small, short-lived buffer that doesn't need
/// to survive the method call is a good candidate for stackalloc, which
/// Span{T} can point at safely (the compiler enforces it can't escape the
/// stack frame). Compares a stackalloc'd Span{int} against the equivalent
/// heap-allocated int[] for a small fixed-size scratch buffer -- the kind
/// of thing a parser might use to stage a handful of values before
/// combining them.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class StackallocVsHeapBenchmarks
{
    private const int BufferSize = 16;

    [Benchmark(Baseline = true)]
    public int Heap_Array()
    {
        var buffer = new int[BufferSize];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i * i;
        }

        var sum = 0;
        foreach (var value in buffer)
        {
            sum += value;
        }

        return sum;
    }

    [Benchmark]
    public int Stackalloc_Span()
    {
        Span<int> buffer = stackalloc int[BufferSize];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i * i;
        }

        var sum = 0;
        foreach (var value in buffer)
        {
            sum += value;
        }

        return sum;
    }
}
