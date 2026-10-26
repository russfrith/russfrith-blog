var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var defaultLatencyMs = int.TryParse(Environment.GetEnvironmentVariable("STUB_DEFAULT_LATENCY_MS"), out var v) ? v : 50;

app.MapGet("/delay", async (HttpRequest request) =>
{
    var latencyMs = defaultLatencyMs;

    if (request.Headers.TryGetValue("X-Stub-Latency-Ms", out var headerValue) &&
        int.TryParse(headerValue, out var headerLatency))
    {
        latencyMs = headerLatency;
    }
    else if (request.Query.TryGetValue("latencyMs", out var queryValue) &&
        int.TryParse(queryValue, out var queryLatency))
    {
        latencyMs = queryLatency;
    }

    await Task.Delay(latencyMs);
    return Results.Ok(new { latencyMs });
});

app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();
