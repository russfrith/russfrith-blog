namespace Api;

/// <summary>
/// Wraps the single downstream call every endpoint makes, so the only difference
/// between "good" and "bad" endpoints is how they call this, not what it does.
/// </summary>
public class DownstreamClient(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("stub");

    public Task<HttpResponseMessage> GetAsync(int latencyMs, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/delay");
        request.Headers.Add("X-Stub-Latency-Ms", latencyMs.ToString());
        return Client.SendAsync(request, cancellationToken);
    }

    public HttpResponseMessage GetSync(int latencyMs)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/delay");
        request.Headers.Add("X-Stub-Latency-Ms", latencyMs.ToString());
        // Deliberately synchronous network call -- the single flaw /blocking-sync demonstrates.
        return Client.Send(request);
    }
}
