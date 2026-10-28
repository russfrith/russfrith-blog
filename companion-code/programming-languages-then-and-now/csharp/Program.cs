// wordfreq --top N --concurrency N --timeout SECONDS urls.txt
//
// Fetches URLs concurrently (bounded concurrency), tokenizes on non-ASCII-letter
// boundaries, lowercases, and prints the top N words by count (desc), then
// alphabetically, to stdout. Timing and per-URL failures go to stderr.
//
// Exit codes: 0 all succeeded, 1 partial failure, 2 bad arguments / missing input file.
using System.Diagnostics;
using System.Text.RegularExpressions;

int? top = null;
int? concurrency = null;
double? timeoutSeconds = null;
string? urlsFile = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--top": top = int.Parse(args[++i]); break;
        case "--concurrency": concurrency = int.Parse(args[++i]); break;
        case "--timeout": timeoutSeconds = double.Parse(args[++i]); break;
        default: urlsFile = args[i]; break;
    }
}

if (top is null || concurrency is null || timeoutSeconds is null || urlsFile is null)
{
    Console.Error.WriteLine("usage: wordfreq --top N --concurrency N --timeout SECONDS urls.txt");
    return 2;
}

string[] urls;
try
{
    urls = File.ReadAllLines(urlsFile)
        .Select(l => l.Trim())
        .Where(l => l.Length > 0)
        .ToArray();
}
catch (IOException e)
{
    Console.Error.WriteLine($"wordfreq: cannot read {urlsFile}: {e.Message}");
    return 2;
}

var counts = new Dictionary<string, int>();
var countsLock = new object();
var hadFailure = false;
var semaphore = new SemaphoreSlim(concurrency.Value);
var wordPattern = new Regex("[^a-zA-Z]+", RegexOptions.Compiled);

using var client = new HttpClient();
var stopwatch = Stopwatch.StartNew();

var tasks = urls.Select(async url =>
{
    await semaphore.WaitAsync();
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds.Value));
        var response = await client.GetAsync(url, cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
        }
        var body = await response.Content.ReadAsStringAsync(cts.Token);
        var words = wordPattern.Split(body.ToLowerInvariant()).Where(w => w.Length > 0);
        lock (countsLock)
        {
            foreach (var w in words)
            {
                counts[w] = counts.GetValueOrDefault(w) + 1;
            }
        }
    }
    catch (Exception e)
    {
        hadFailure = true;
        var message = e is OperationCanceledException ? "timed out" : e.Message;
        Console.Error.WriteLine($"wordfreq: fetch failed: {url}: {message}");
    }
    finally
    {
        semaphore.Release();
    }
});

await Task.WhenAll(tasks);

Console.Error.WriteLine($"wordfreq: fetched {urls.Length} url(s) in {stopwatch.Elapsed.TotalSeconds:F3}s");

var sorted = counts
    .OrderByDescending(kv => kv.Value)
    .ThenBy(kv => kv.Key, StringComparer.Ordinal)
    .Take(top.Value);

foreach (var (word, count) in sorted)
{
    Console.WriteLine($"{word} {count}");
}

return hadFailure ? 1 : 0;
