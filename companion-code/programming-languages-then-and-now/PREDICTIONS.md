# Predictions

Written before running each scenario, per the post's results rules. Actual outcomes and mismatches are recorded under each after the run.

## Shared CLI (wordfreq) acceptance

**Prediction:** all five core languages produce byte-identical stdout against `shared/expected-output/*.txt` for the happy-path fixture set, and match on exit codes 0/1/2 for the partial-failure and bad-argument cases.

**Actual:** recorded in `shared/acceptance/RESULTS.md` after running `docker compose run --rm acceptance`.

## Scenario 1 — absent/null value

**Prediction:** Go silently zero-fills a missing JSON field (no error); TypeScript's compile-time type says the field is present even though it's missing at runtime (no runtime error until something dereferences it); Python/Rust/Kotlin raise/return an explicit absent-value signal (`KeyError`/`None` check, `Option`, nullable type check) when the fixed version is written defensively.

**Actual:** see `<lang>/scenarios/01-absent-null/output.txt` for each language.

## Scenario 2 — data race

**Prediction:** Go and Kotlin's broken versions produce a wrong/inconsistent counter total under `-race` (Go) or just a wrong total under load (Kotlin, JVM has no race detector in the box); Rust's broken version refuses to compile when the race is expressed as shared mutable state across threads without synchronization (borrow checker/`Send`/`Sync`), so Rust's "broken" example is instead `unsafe` code or `Rc<RefCell<_>>` shared across threads to actually get a race. Python's GIL build won't show the race for simple counter increments; the free-threaded build might.

**Actual:** see `<lang>/scenarios/02-data-race/output.txt`.

## Scenario 3 — integer overflow

**Prediction:** Go wraps silently in a release build (two's complement) with no panic; Rust panics in debug builds on overflow and wraps in release unless `checked_add`/overflow-checks are enabled; Python has arbitrary-precision ints so there is no overflow to show (documented as a non-event, not "Python wins"); Kotlin/JVM `Int` wraps like Java; TypeScript's `number` silently loses precision past 2^53 (not classic wraparound, a different failure mode, called out as such).

**Actual:** see `<lang>/scenarios/03-integer-overflow/output.txt`.

## Scenario 4 — ignored errors

**Prediction:** Go's broken version ignores an `error` return value and proceeds with a zero-value result; Rust's broken version uses `.unwrap()` or ignores a `Result` with `let _ =`; TypeScript ignores a rejected promise (unhandled rejection) when `await` is dropped; Python ignores an exception only if it's actively swallowed by a bare `except: pass`; Kotlin ignores a checked-like situation only via a swallowed exception too, since Kotlin (like Java) doesn't force handling.

**Actual:** see `<lang>/scenarios/04-ignored-errors/output.txt`.

## Scenario 5 — resource cleanup and leaks

**Prediction:** C baseline leaks heap memory with no `free()` (confirmed via a leak-detection run); Python/Go/Kotlin/TypeScript (GC'd) never truly "leak" in the C sense but can retain memory via long-lived references (e.g., an ever-growing cache); Rust's broken `Rc`-cycle example leaks (memory never freed) but this is explicitly NOT a memory-safety violation — Rust guarantees deterministic `Drop` and no use-after-free, not leak-freedom. This is the specific point the post must state accurately.

**Actual:** see `<lang>/scenarios/05-resource-cleanup/output.txt` and `c/scenarios/05-resource-cleanup/output.txt`.

## Scenario 6 — hung request and cancellation

**Prediction:** every core language can cancel the hung mock-server request given a timeout/cancellation token used correctly; broken versions (no timeout wired through) hang until the mock server itself would need killing, so broken runs are capped with an outer test-harness timeout rather than run to true completion.

**Actual:** see `<lang>/scenarios/06-hung-cancellation/output.txt`.

## Scenario 7 — CPU-heavy work blocking an event loop

**Prediction:** Node and Python asyncio (GIL build) both show the CPU-bound broken version stalling concurrent I/O-bound requests on the same event loop/thread; Go, Kotlin (coroutines on a dispatcher backed by multiple OS threads) and Rust async (unless a blocking call is placed directly on an async executor thread without `spawn_blocking`) do not stall in the same way. Python's free-threaded build is included as a comparison point for the CPU-bound case specifically, not as a general asyncio replacement.

**Actual:** see `<lang>/scenarios/07-cpu-heavy-blocking/output.txt`.

## Scenario 8 — parsing untrusted JSON

**Prediction:** TypeScript's declared type for the mock server's malformed-JSON endpoint is accepted by the compiler but wrong at runtime (type erasure); Go's `encoding/json` silently zero-fills the missing field into the struct with no error; Python/Rust/Kotlin's strict decoders (`dataclass`+manual check, `serde` with `Option`/validation, `kotlinx.serialization`) catch the missing/wrong-typed field when written defensively, and show what the naive version does wrong first.

**Actual:** see `<lang>/scenarios/08-untrusted-json/output.txt`.

## Phase 1b additions

### A1 — independent expected-output verification

**Prediction:** a coreutils-only pipeline (curl/tr/sort/uniq/head) reading the same
mock-server fixtures, with the same tie-break rule, will match the existing
`shared/expected-output/*.txt` files exactly, since those files were generated
from the actual mock-server content, not invented.

**Actual:** matched exactly for both fixtures. See `shared/acceptance/README.md`.
No changes were made to the expected-output files.

### A3 — scenario 7 workload equalization

**Prediction:** once Node, Python, and Go all run the identical iteration count
with the result printed, Go and Node (both compiled-to-native-code JIT paths: V8's
JIT and Go's static compiler) should land in the same order of magnitude; Python,
pure-interpreted, should be substantially slower (expect at least 10x).

**Actual:** Go (392ms) and Node (404ms) landed within about 3% of each other at
300,000,000 iterations -- closer than predicted. Python took 13.2s, about 33x
slower than Go/Node, more than the predicted 10x+. The ORIGINAL Phase 1 Go result
(332ms for 2,000,000,000 iterations, an apparent 8x Go-vs-Node advantage) was
investigated and found to be invalid: the loop's result was never used, so Go's
compiler eliminated it as dead code. Re-run with the result printed, the same
2,000,000,000-iteration workload took 2.74s, matching Node's ~2.6-2.7s for the
same count. See go/scenarios/07_cpu_heavy_blocking/output.txt for the full
investigation.

### A4 — Rust scenario 7 (tokio)

**Prediction:** a blocking (synchronous CPU-bound or `std::thread::sleep`-style)
call placed directly inside an async task will stall a heartbeat task on a
`current_thread` runtime completely (only one OS thread services all tasks, so a
blocking call starves everything else). On the default multi-thread runtime, a
single blocked worker thread may still let the heartbeat run on a different
worker thread, UNLESS all worker threads get occupied by blocking calls or tokio's
cooperative scheduling assumptions are violated badly enough to delay the
heartbeat anyway. `spawn_blocking` should restore heartbeat ticking in both cases
by moving the blocking work to tokio's dedicated blocking thread pool.

### A5 — Kotlin scenario 7 (coroutines)

**Prediction:** a single-threaded dispatcher (or `runBlocking` on one thread) will
stall the heartbeat completely while CPU-bound work runs, the same qualitative
result as Node/Python's broken case. `Dispatchers.Default` (a multi-threaded pool
sized to available cores) should let the heartbeat keep ticking, the same way
Go's goroutines did, since the CPU-bound coroutine and the heartbeat coroutine can
run on different OS threads concurrently.

### A6 — scenario 8 for Rust and Kotlin

**Prediction:** `serde_json` with a plain (non-`Option`) field will error on
deserialize for BOTH the missing-field and wrong-type cases (serde is strict about
required fields by default, unlike Go). Adding `#[serde(default)]` will make the
missing-field case silently zero-fill, mirroring Go's behavior -- a deliberate
opt-in counterpoint, not a strictness failure. Kotlin's `kotlinx.serialization`
should behave the same way: strict by default, with an explicit default value
annotation needed to opt into zero-filling.

### A7 — Rust scenario 2, safe-Rust compiler rejections

**Prediction:** capturing a plain `&mut i64` (or owned `i64`) in two
`thread::spawn` closures without `Arc`/synchronization will fail to compile with a
lifetime/ownership error (the closures would outlive the borrowed stack frame, or
Rust will demand `move`, after which both closures can't own the same value
without `Clone`, which would make them independent counters, not a shared race).
Sharing a plain `Rc<RefCell<i64>>` across `thread::spawn` will fail to compile with
a trait-bound error, because `Rc` is not `Send` (it's not thread-safe by design,
unlike `Arc`). Both should be genuine compile-time rejections, not runtime
failures.

### A8 — version/provenance checks (no prediction; a direct audit)

**Actual:** the Temurin JDK version recorded in Phase 1's VERSIONS.md ("25.0.4.7",
"2026-08-21") was wrong on both the patch number and the date -- it was apparently
copied from a secondary aggregator without checking the actual pulled image.
Direct `java -version` against the pinned `eclipse-temurin:25-jdk`/`25-jre` images
resolves to 25.0.4.1+1, dated 2026-08-18. Corrected in VERSIONS.md. Separately,
Phase 1's VERSIONS.md described a free-threaded (`python3.14t`) build and Docker
step that was never actually built -- every Phase 1 Python scenario ran on the
default GIL build. There is also no OFFICIAL free-threaded Python Docker image as
of this lookup. Every Python scenario output now prints `sys.version` and
`sys._is_gil_enabled()` so this can't silently drift again; see each
`python/scenarios/*/output.txt`.

## C# (Part B) — predictions before running

**1 absent value:** a fresh .NET 10 console project template enables nullable
reference types as a compiler WARNING, not an error -- a null dereference where
the compiler warned should still build and run, then throw a
`NullReferenceException` at runtime. `Dictionary` indexing on a missing key should
throw `KeyNotFoundException`; `TryGetValue` should return `false` without
throwing.

**2 data race:** an unsynchronized `counter++` across `Task.Run`/`Thread`
instances should lose updates under load (same risk as Go/Kotlin/Java); expect it
may need the same kind of artificial delay Python/Rust needed to reproduce
reliably, since this is the same class of race. `Interlocked.Increment` or `lock`
should fix it.

**3 integer overflow:** C#'s default arithmetic context is `unchecked` -- overflow
should wrap silently, like Go/Kotlin. An explicit `checked` block, or the
project-level `<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>`
setting, should make it throw `OverflowException` instead.

**4 ignored errors:** a swallowed `catch {}` should behave like Python/Kotlin's
equivalent. A "fire-and-forget" `Task` (started and not awaited, not stored, not
observed) whose body throws should NOT crash the process the way an unobserved
Node promise rejection does by default -- .NET's default unobserved-task-exception
behavior differs from Node's unhandled-rejection-is-fatal default; expect either a
silently swallowed exception or (pre-.NET 4.5-era behavior, now changed) a
finalizer-thread crash. This needs to be checked against the actual runtime, not
assumed.

**5 resource cleanup:** `IDisposable` without `using` should leave a resource
(file handle) open longer than necessary, same risk as every other GC'd language
here; `using`/`await using` should close it deterministically when the block
exits.

**6 hung request:** `CancellationTokenSource` with a timeout, passed to
`HttpClient.GetAsync`, should cancel at the configured timeout, same as every
other language.

**7 CPU-heavy work:** .NET does not have a single-threaded event loop the way
Node or Python asyncio does -- `async`/`await` is built on the thread pool, which
has multiple worker threads by default. A CPU-bound loop called directly inside
an `async` method without `Task.Run` should run synchronously on whichever thread
called it (blocking that one caller), but should NOT stall a heartbeat running as
a separate `Task`/`async` operation the way Node/Python's single-threaded case
does, because the thread pool has other threads available -- expected to pattern-
match Go/Rust's multi-thread-runtime/Kotlin's Dispatchers.Default result (heartbeat
keeps ticking) rather than Node/Python's single-threaded stall. This needs
verification, not assumption, per the prompt's own instruction.

**8 untrusted JSON:** `System.Text.Json` with a plain (non-nullable, no
`required`) field should likely zero-fill a MISSING field silently (System.Text.Json
is documented as lenient about missing members by default, closer to Go than to
serde/kotlinx.serialization) while still erroring on a WRONG-TYPED field (a JSON
string where a number is expected is a hard type mismatch). The `required`
modifier (C# 11+) should turn the missing-field case into a thrown
`JsonException` instead.

**Footgun:** `async void` is the strongest candidate -- exceptions thrown inside an
`async void` method are NOT caught by a surrounding `try`/`catch` around the call
site (they're rethrown on the `SynchronizationContext` instead), unlike
`async Task`. This is justified over the other candidates (nullable warnings not
breaking the build -- already covered in scenario 1; `HttpClient` created per
request -- a performance/socket-exhaustion footgun, not a correctness-crash one)
because it reproduces a clean, surprising, incorrect-looking-correct crash in a
few lines.

### C# — actual results (all in `csharp/scenarios/*/output.txt`)

- **1**: confirmed exactly. CS8603 warning, build succeeds, `NullReferenceException` at runtime. Indexer throws `KeyNotFoundException`, `TryGetValue` doesn't.
- **2**: confirmed. Raced reliably on the first run (no induced delay needed), matching Kotlin/TypeScript rather than Python/Rust's tight-loop case.
- **3**: confirmed exactly, both the `checked()` expression and the project-level setting throw `OverflowException` for identical source.
- **4**: confirmed for the fire-and-forget Task -- no crash, silently absorbed, a genuine difference from Node's default. One deviation: `/no/such/path.txt` threw `DirectoryNotFoundException`, not `FileNotFoundException` as first assumed (the parent directory is also missing); fixed by catching the common `IOException` base class.
- **5**: confirmed, same pattern as every other GC'd language.
- **6**: confirmed, cancels at ~2s via `CancellationTokenSource`.
- **7**: confirmed -- no single-threaded event loop, heartbeat unaffected whether or not `Task.Run` wraps the CPU-bound call, in both cases. ~1000ms for the same 300M-iteration workload Go/Node did in ~400ms -- noted as an observation, not investigated further (not an order-of-magnitude gap, A3's bar for investigation).
- **8**: confirmed exactly -- plain property zero-fills on absence, throws on wrong type; `required` makes both throw.
- **Footgun**: confirmed the try/catch doesn't catch it; one correction -- the result is a process-crashing unhandled exception, not a silent swallow as the broken file's comment originally implied.

## Scenarios 9-11 — CLI size/deps, build/rebuild time, image size

**Prediction:** Go and Rust produce the smallest final images (static-ish binaries, multi-stage builds); Python needs the full interpreter + stdlib (or a slim variant) in the final image; Kotlin's final image carries a JRE; Rust has the slowest clean build and a much faster incremental rebuild; Go has both the fastest clean build and fastest incremental rebuild.

**Actual:** see `results/productivity.md` and `results/productivity.json` for measured (not benchmarked) figures with the exact commands used. **Mismatch, not a confirmation:** Go is the smallest final image, as predicted, and Kotlin is confirmed slowest clean build and largest image -- but Go is NOT the fastest clean build or the fastest incremental rebuild. Real ranking, clean build (fastest to slowest): Python (1.26s) < C# (4.27s) < TypeScript (4.38s) < Go (6.75s) < Rust (11.59s) < Kotlin (69.1s). Real ranking, incremental rebuild (fastest to slowest): TypeScript (0.95s) < Rust (0.98s) < Python (1.0s) < Kotlin (1.07s) < C# (1.25s) < Go (1.32s) -- Go is in fact the *slowest* to rebuild incrementally of the six, despite having the smallest final image. C# added in Phase 1b: zero third-party CLI dependencies (matching Go/Python), a mid-pack clean build time (4.27s median), and three image-size variants -- framework-dependent (287.4MB) and self-contained (286.2MB) landed almost identical, which was not predicted in advance; Native AOT (197.8MB) was the smallest of the three and built successfully with only `clang`/`zlib1g-dev` added to the build stage, no external toolchain install needed.
