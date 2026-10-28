// Scenario 7: CPU-heavy work inside an async method, called directly (no
// Task.Run). .NET has no single-threaded event loop -- async/await runs on the
// thread pool, which has multiple worker threads by default. Calling CPU-bound
// work directly blocks only the ONE thread that called it; a heartbeat running
// as a separate Task should keep ticking on a different thread.
const long iterations = 300_000_000; // same workload across every language in this scenario

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

Console.WriteLine("starting CPU-bound work directly (no Task.Run)...");
var sw = System.Diagnostics.Stopwatch.StartNew();
double result = CpuBound(iterations); // runs synchronously on the calling thread
sw.Stop();
cts.Cancel();

Console.WriteLine($"CPU-bound work finished after {sw.Elapsed.TotalMilliseconds:F0}ms, heartbeat ticks seen: {ticks}, result: {result}");
