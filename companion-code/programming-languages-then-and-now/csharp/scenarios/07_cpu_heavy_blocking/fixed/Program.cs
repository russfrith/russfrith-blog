// Scenario 7 fixed (idiomatic): Task.Run offloads the CPU-bound work to the
// thread pool explicitly and the caller awaits it, freeing the calling thread
// (and, by extension, any UI/synchronization-context thread it might have been)
// while it runs.
const long iterations = 300_000_000;

double CpuBound(long n)
{
    double total = 0;
    for (long i = 0; i < n; i++) total += Math.Sqrt(i);
    return total;
}

int ticks = 0;
using var cts = new CancellationTokenSource();
var heartbeat = Task.Run(async () =>
{
    while (!cts.IsCancellationRequested)
    {
        await Task.Delay(100);
        ticks++;
        Console.WriteLine($"heartbeat tick {ticks}");
    }
});

Console.WriteLine("starting CPU-bound work via Task.Run...");
var sw = System.Diagnostics.Stopwatch.StartNew();
double result = await Task.Run(() => CpuBound(iterations));
sw.Stop();
cts.Cancel();

Console.WriteLine($"CPU-bound work finished after {sw.Elapsed.TotalMilliseconds:F0}ms, heartbeat ticks seen: {ticks}, result: {result}");
