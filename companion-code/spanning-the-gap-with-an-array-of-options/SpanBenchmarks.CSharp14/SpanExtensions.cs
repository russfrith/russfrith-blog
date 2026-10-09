namespace SpanBenchmarks;

/// <summary>
/// An extension method with a ReadOnlySpan{T} receiver. Under C# 13 this
/// could not bind to an array argument at all -- callers had to write
/// `array.AsSpan().SumValues()`. Under C# 14 the implicit array-to-span
/// conversion applies to extension receivers, so `array.SumValues()`
/// compiles directly.
/// </summary>
public static class SpanExtensions
{
    public static long SumValues(this ReadOnlySpan<int> values)
    {
        long sum = 0;
        foreach (var value in values)
        {
            sum += value;
        }

        return sum;
    }
}
