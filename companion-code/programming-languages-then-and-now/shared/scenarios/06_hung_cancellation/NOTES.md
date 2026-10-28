Scenario 6: hung request and cancellation. All six wordfreq implementations
already wire the `--timeout` flag through to their HTTP client correctly (that was
part of the baseline CLI contract), so there is no separate broken/fixed pair here
-- this is the baseline CLI itself, pointed at the mock server's /hang endpoint
(sleeps 3600s, never responds), run with `--timeout 2`.

All six cancel at ~2s (the configured timeout) rather than hanging for the
mock server's full 3600s sleep. The "broken" counterpart in every language is simply
constructing the HTTP client WITHOUT a timeout (e.g. Go's `http.Get` using
`http.DefaultClient`, which has no timeout at all) -- that version would hang for
the full 3600s, which is impractical to capture verbatim in a committed output file,
so it is described rather than run to completion.
