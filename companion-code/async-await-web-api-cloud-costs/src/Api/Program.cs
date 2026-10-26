using Api;

var builder = WebApplication.CreateBuilder(args);

var stubBaseUrl = Environment.GetEnvironmentVariable("STUB_BASE_URL") ?? "http://stub:8080";

builder.Services.AddHttpClient("stub", c => c.BaseAddress = new Uri(stubBaseUrl));
builder.Services.AddSingleton<DownstreamClient>();

var app = builder.Build();

static int LatencyMs(HttpRequest request) =>
    request.Query.TryGetValue("latencyMs", out var v) && int.TryParse(v, out var ms) ? ms : 50;

static int FanOutCount(HttpRequest request) =>
    request.Query.TryGetValue("n", out var v) && int.TryParse(v, out var n) ? n : 10;

const int BoundedMaxDegree = 4;

// 1. Fully async, awaits the downstream call. No blocking.
app.MapGet("/async-correct", async (HttpRequest request, DownstreamClient downstream, CancellationToken ct) =>
{
    using var response = await downstream.GetAsync(LatencyMs(request), ct);
    return Results.StatusCode((int)response.StatusCode);
});

// 2. Calls the async client but blocks the thread waiting on it.
app.MapGet("/sync-over-async", (HttpRequest request, DownstreamClient downstream) =>
{
    using var response = downstream.GetAsync(LatencyMs(request), CancellationToken.None).GetAwaiter().GetResult();
    return Results.StatusCode((int)response.StatusCode);
});

// 3. Synchronous call path end to end (HttpClient.Send, not SendAsync).
app.MapGet("/blocking-sync", (HttpRequest request, DownstreamClient downstream) =>
{
    using var response = downstream.GetSync(LatencyMs(request));
    return Results.StatusCode((int)response.StatusCode);
});

// 4. Async, fans out N downstream calls with unbounded Task.WhenAll.
app.MapGet("/fanout-unbounded", async (HttpRequest request, DownstreamClient downstream, CancellationToken ct) =>
{
    var latencyMs = LatencyMs(request);
    var tasks = Enumerable.Range(0, FanOutCount(request))
        .Select(_ => downstream.GetAsync(latencyMs, ct));
    var responses = await Task.WhenAll(tasks);
    foreach (var r in responses) r.Dispose();
    return Results.Ok();
});

// 5. Same fan-out work, bounded concurrency via SemaphoreSlim.
app.MapGet("/fanout-bounded", async (HttpRequest request, DownstreamClient downstream, CancellationToken ct) =>
{
    var latencyMs = LatencyMs(request);
    var count = FanOutCount(request);
    using var gate = new SemaphoreSlim(BoundedMaxDegree);

    async Task<HttpResponseMessage> CallOne()
    {
        await gate.WaitAsync(ct);
        try
        {
            return await downstream.GetAsync(latencyMs, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    var tasks = Enumerable.Range(0, count).Select(_ => CallOne());
    var responses = await Task.WhenAll(tasks);
    foreach (var r in responses) r.Dispose();
    return Results.Ok();
});

// 6. Async work that ignores the incoming cancellation token on the downstream call.
app.MapGet("/async-no-cancellation", async (HttpRequest request, DownstreamClient downstream) =>
{
    using var response = await downstream.GetAsync(LatencyMs(request), CancellationToken.None);
    return Results.StatusCode((int)response.StatusCode);
});

// 7. Same async work, correctly threading RequestAborted through to the downstream call.
app.MapGet("/async-with-cancellation", async (HttpRequest request, DownstreamClient downstream) =>
{
    using var response = await downstream.GetAsync(LatencyMs(request), request.HttpContext.RequestAborted);
    return Results.StatusCode((int)response.StatusCode);
});

// 8. Control endpoint: fixed CPU-bound work, no I/O. Async does not help here.
app.MapGet("/cpu-bound", () =>
{
    var result = CpuWork.HashLoop();
    return Results.Ok(new { result });
});

app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();
