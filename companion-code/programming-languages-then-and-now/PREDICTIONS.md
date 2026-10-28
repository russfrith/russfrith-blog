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

## Scenarios 9-11 — CLI size/deps, build/rebuild time, image size

**Prediction:** Go and Rust produce the smallest final images (static-ish binaries, multi-stage builds); Python needs the full interpreter + stdlib (or a slim variant) in the final image; Kotlin's final image carries a JRE; Rust has the slowest clean build and a much faster incremental rebuild; Go has both the fastest clean build and fastest incremental rebuild.

**Actual:** see `results/productivity.md` for measured (not benchmarked) figures with the exact commands used.
