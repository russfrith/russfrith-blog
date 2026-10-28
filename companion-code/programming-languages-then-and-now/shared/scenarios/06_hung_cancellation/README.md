# Scenario 6 -- hung request and cancellation

Unlike every other scenario in this post, this one has no broken/fixed pair. All
six `wordfreq` implementations wire `--timeout` through to their HTTP client
correctly as part of the baseline CLI contract from the start -- there was never a
separate "broken" version written for this scenario. What's captured in
`output.txt` is the baseline CLI itself, pointed at the mock server's `/hang`
endpoint (sleeps 3600s, never responds), run with `--timeout 2`.

## How each language's cancellation is wired (from the existing `wordfreq` source, no verdict)

- **Python** (`python/wordfreq.py`): `urllib.request.urlopen(url, timeout=timeout)` -- the stdlib HTTP client's own `timeout` parameter, enforced per-connection.
- **Go** (`go/wordfreq.go`): `http.Client{Timeout: ...}`, a client-level timeout covering the whole request/response round trip.
- **Rust** (`rust/src/main.rs`): `ureq::AgentBuilder::new().timeout(timeout)`, an agent-level timeout applied per request.
- **TypeScript** (`typescript/src/wordfreq.ts`): an `AbortController` plus `setTimeout(() => controller.abort(), timeoutMs)`, passed as `fetch(url, { signal: controller.signal })` -- the standard Web/Node cancellation primitive.
- **Kotlin** (`kotlin/src/main/kotlin/Wordfreq.kt`): `HttpClient.newBuilder().connectTimeout(...)` plus a per-request `HttpRequest.newBuilder(...).timeout(...)`, both from `java.net.http`.
- **C#** (`csharp/wordfreq.csproj` source): `CancellationTokenSource` with a timeout, passed through `HttpClient.GetAsync(url, cancellationToken)` -- the BCL's standard cooperative-cancellation primitive.

All six cancel at roughly the configured timeout (see `output.txt`) rather than
hanging for the mock server's full sleep duration.
