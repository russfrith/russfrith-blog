// Scenario 2 fixed: Interlocked.Increment makes the increment atomic.
const int iterations = 200_000;
long counter = 0;

var t1 = Task.Run(() =>
{
    for (int i = 0; i < iterations; i++) Interlocked.Increment(ref counter);
});
var t2 = Task.Run(() =>
{
    for (int i = 0; i < iterations; i++) Interlocked.Increment(ref counter);
});
await Task.WhenAll(t1, t2);

Console.WriteLine($"counter = {counter} (expected {2 * iterations})");
