---
title: "Programming Languages, Then and Now"
description: "A hands-on comparison of Python, Go, Rust, TypeScript, Kotlin, and C# (plus a C safety baseline) on the same small CLI task, and a look at why the language landscape looks the way it does."
date: 2026-10-27T20:00:00-04:00
draft: true
tags: ["python", "go", "rust", "typescript", "kotlin", "csharp", "dotnet", "programming-languages"]
categories: ["programming"]
ShowToc: true
---

<!-- AUTHOR-TODO: opening anecdote -- my start in the 1980s, and my professor's claim
that reading a language's specification teaches you new languages. This post is,
among other things, a test of that claim: see the "what was familiar / what
surprised me" notes under each language below. -->

## Why languages exist

Most early general-purpose languages were built to solve one specific problem, not to be general-purpose in the way we'd use that word today.

- **Fortran** (1957) existed to make scientific and engineering numerical computation practical without hand-written assembly, on IBM's own hardware; its name is a contraction of "FORmula TRANslation." See [IBM Research's listing for "The History of Fortran I, II, and III"](https://research.ibm.com/publications/the-history-of-fortran-i-ii-and-iii--2), written by John Backus, who led the original project.
<!-- AUTHOR-TODO: verify in a normal browser that this page supports: Fortran's name is a contraction of "FORmula TRANslation," and it existed to make scientific/engineering computation practical on IBM's own hardware -->
- **COBOL** (1959) existed to make business data processing (payroll, inventory, billing) readable by non-programmers reviewing the code. [NIST's NBS Handbook 106](https://nvlpubs.nist.gov/nistpubs/Legacy/hb/nbshandbook106.pdf) archives the CODASYL committee's early specification work.
<!-- AUTHOR-TODO: verify in a normal browser that this page supports: COBOL existed to make business data processing readable by non-programmers, and this handbook documents CODASYL's early specification work -->
- **BASIC** (1964) existed to make programming accessible to students outside engineering and science departments at Dartmouth, on a time-sharing system, deliberately simplified versus Fortran. Source: [Dartmouth's own account, "BASIC Begins at Dartmouth"](https://www.dartmouth.edu/basicfifty/basic.html).
- **Pascal** (1970) existed to teach structured programming systematically, as a reaction against languages Niklaus Wirth considered too large and complex for students to understand in their entirety. [Wirth's 1984 ACM Turing Award lecture, "From Programming Language Design to Computer Construction"](https://informatik.uni-bremen.de/agbkb/lehre/programmiersprachen/artikel/wirth-Turing-Award.pdf).
<!-- AUTHOR-TODO: verify in a normal browser that this page supports: Pascal existed to teach structured programming systematically, as a reaction against languages Wirth considered too large and complex for students to understand in their entirety -->
- **C** (1972) existed to write an operating system (Unix) and its tools efficiently, portably, and close to the hardware, replacing the assembly Unix had been written in -- Dennis Ritchie's own account, [&ldquo;The Development of the C Language&rdquo;](https://www.nokia.com/bell-labs/about/dennis-m-ritchie/chist.html).
<!-- AUTHOR-TODO: verify in a normal browser that this page supports: C existed to write Unix and its tools efficiently, portably, and close to the hardware, replacing the assembly Unix had been written in -->
- **Ada** (1980) existed because the U.S. Department of Defense was funding hundreds of incompatible languages across embedded and safety-critical defense systems and wanted one language with built-in support for strong typing, concurrency, and real-time constraints -- per the DoD's own ["Rationale for the Design of the Ada Programming Language"](https://apps.dtic.mil/sti/pdfs/ADA090709.pdf) (Ichbiah et al.).
<!-- AUTHOR-TODO: verify in a normal browser that this page supports: Ada existed because the DoD was funding hundreds of incompatible languages and wanted one with built-in strong typing, concurrency, and real-time support -->

<!-- AUTHOR-TODO: candidate observation from writing this section -- how many of
these "one problem" origin stories map cleanly onto scenarios later in this post
(Ada's concurrency focus vs. scenario 6/7; Pascal's structure vs. the footguns). -->

## Eras and pivot points

The 1980s-1990s object-oriented era (C++, Java, C#) reframed "what problem does this solve" around managing complexity in large codebases through encapsulation and inheritance, not around a single computing domain the way Fortran or COBOL had been. [Bjarne Stroustrup's papers page](https://stroustrup.com/papers.html) links his own HOPL retrospectives on C++'s design history.
<!-- AUTHOR-TODO: verify in a normal browser that this page supports: the 1980s-1990s object-oriented era (C++, Java, C#) reframed language design around managing complexity in large codebases through encapsulation and inheritance -->

Web scripting in the mid-1990s to 2000s (Perl, PHP, then Python and Ruby) optimized for a different thing entirely: get a dynamic page in front of a browser quickly, with the language's job being to glue together HTML, a database, and some logic. Ruby on Rails (2004) was a pivot point within that era specifically because it bundled opinionated conventions (routing, an ORM, scaffolding) into the framework itself -- see [Rails' own doctrine page](https://rubyonrails.org/doctrine), particularly its "Convention over Configuration" and "The Menu is Omakase" sections, which state this design philosophy directly.

Python's rise to its current dominance is widely attributed less to language design changes than to its library ecosystem, particularly NumPy, pandas, and scikit-learn, which made it a default glue language for data science and machine learning. [The SciPy 1.0 paper](https://arxiv.org/pdf/1907.10121), written by the maintainers of that ecosystem, documents one piece of that ecosystem's own growth and interdependencies directly -- it's evidence of the ecosystem's scale, not a claim about Python's overall dominance, which is this post's own broader framing of what that growth enabled. The language itself changed relatively incrementally over that period; the ecosystem around it is what changed the calculation of "which language should I use for this."

## What changed after the 2000s

A cluster of new systems and application languages appeared from the mid-2000s onward -- Go (2009), Rust (2010 announced, 2015 stable 1.0), Swift (2014), Kotlin (2011), Zig (2016), Mojo (2023) -- at a pace that looks different from the preceding couple of decades. A few commonly cited, partial explanations, stated carefully because this is a contested, multi-causal historical question:

- **LLVM lowering the cost of building a compiler backend.** Writing a production-quality optimizing backend from scratch is a multi-year undertaking; LLVM has become a common way for new language projects to focus their effort on the frontend and type system instead. [Lattner and Adve's original 2004 LLVM paper](https://llvm.org/pubs/2004-01-30-CGO-LLVM.html) describes a different original purpose -- lifelong program analysis and transformation -- not reuse by new-language frontends specifically; that reuse is a real, later pattern (Rust, Swift, Zig, Mojo all use it) rather than something the founding paper itself claims. This is a commonly cited factor, not a universal one: **Go is an explicit counterexample** -- it uses a hand-written backend, not LLVM, and still shipped a usable, reasonably fast compiler.
- **Package managers built into the language or its official tooling.** Cargo shipped with Rust from day one. Go is a partial counterexample here too: it launched in 2009 with no built-in module system, and didn't get one until Go modules arrived in Go 1.11, nine years later, in 2018. Older languages like C and C++ still don't have one standard, built-in answer to "how do I pull in a dependency."
- **Corporate sponsors with a platform to push**: Go ([Google, per its 2009 announcement](https://developers.googleblog.com/2009/11/go-new-programming-language.html)), Rust (originally Mozilla), Swift (Apple, for its own platforms -- see the Swift section below), Kotlin (JetBrains, with Google [officially announcing Android support for Kotlin](https://android-developers.googleblog.com/2017/05/android-announces-support-for-kotlin.html) on 17 May 2017). Having a company fund full-time compiler engineers for years is a different resourcing model than earlier languages often had.
- **Hardware and platform shifts**: multi-core becoming the default (making "safe concurrency" a sellable language feature in a way it wasn't when most CPUs were single-core), and mobile platforms (iOS, Android) creating a reason for platform owners to want their own first-party language.

<!-- AUTHOR-TODO: this section was drafted carefully to avoid overclaiming a single
cause. Candidate observation: worth reading the REVIEW.md items before publishing,
since at least one (Kotlin server adoption figures) might need a decision about
whether to just omit the number entirely rather than hedge it in the post. -->

## How the comparison works

The shared task across every core language is a small CLI, `wordfreq`:

```
wordfreq --top N --concurrency N --timeout SECONDS urls.txt
```

It fetches the listed URLs concurrently (bounded by `--concurrency`), lowercases the combined text, splits on runs of non-ASCII-letter characters, drops empty tokens, and prints the top N words by count (descending, then alphabetical) to stdout. Per-URL failures and timing go to stderr. Exit codes: `0` everything succeeded, `1` partial failure, `2` bad arguments or a missing input file.

Everything runs in Docker containers pinned to specific versions, looked up 2026-10-28 (full detail and sources in `companion-code/programming-languages-then-and-now/VERSIONS.md`):

| Language | Version | Base image |
|---|---|---|
| Python | 3.14.8 | `python:3.14.8-slim` |
| Go | 1.26.4 | `golang:1.26.4-bookworm` |
| Rust | 1.96.1 | `rust:1.96.1-slim-bookworm` |
| TypeScript (on Node) | Node 26 LTS | `node:26-bookworm-slim` |
| Kotlin | 2.4.0 (JVM: Eclipse Temurin 25, build 25.0.4.1+1) | compiled on JDK |
| C# | .NET 10 SDK 10.0.402 (LTS) | `mcr.microsoft.com/dotnet/sdk:10.0` |
| C (baseline) | GCC 16.2 | `gcc:16.2` |

A shared mock HTTP server (Python stdlib `http.server`, no third-party dependencies) serves fixed pages, a 404, an endpoint that hangs, a large page, and two JSON endpoints with a missing field and a wrong-typed field, respectively. A connection-refused case is tested by pointing at an unused port. Nothing ever touches the live internet. A shared acceptance script runs every implementation against the same fixtures and diffs stdout against one expected-output file per fixture; all six core languages (Python, Go, Rust, TypeScript, Kotlin, C#) pass it as of this writing. That expected output was also recomputed independently, with a coreutils-only pipeline (`curl`/`tr`/`sort`/`uniq`/`head`) that shares no code with any of the six implementations, and matched exactly -- see `shared/acceptance/README.md`.

<!-- AUTHOR-TODO: disclosure that C# is my daily language and my notes on it are the baseline, not a neutral review -->

**Honest limits of this comparison**: this is a small, I/O-bound task. It says very little about CPU-bound throughput, large-codebase maintainability, or how any of these languages feel after a year of real production use. The numbers under "productivity" below are from three runs each, median and range reported, not controlled benchmarks -- no isolated hardware, no larger sample. Treat everything quantitative in this post as "this is what happened on my machine," not "this is the true relative performance of these languages."

## Scenarios

Ten fixed scenarios, grouped by theme, plus one signature footgun per language. C serves as a baseline for the five safety scenarios only (no full CLI). For each scenario, languages where something notable happened get full detail; the rest get a line or a table row.

{{< aside >}}
**Verification note:** two claims in this post were specifically checked, not assumed. First, the expected CLI output was independently recomputed with a coreutils-only pipeline sharing no code with any implementation, and matched exactly (`shared/acceptance/README.md`). Second, scenario 7's original ~8x Go-vs-Node gap was investigated by inspecting the compiler's actual assembly output, not just re-run -- it turned out to be dead-store elimination removing unused work, not a real language difference (full investigation under scenario 7 below).
{{< /aside >}}

### Safety

#### 1. Absent or null value

C has no language-level concept of "field absent" -- the baseline is an unchecked NULL pointer:

{{< snippet file="c/scenarios/01-absent-null/broken.c" >}}

{{< snippet file="c/scenarios/01-absent-null/output.txt" lang="text" >}}

Go's `encoding/json` silently zero-fills a missing field into the struct, with no error at all:

{{< snippet file="go/scenarios/01_absent_null/broken.go" >}}

{{< snippet file="go/scenarios/01_absent_null/output.txt" lang="text" >}}

The fix in Go is to decode into a pointer field, so "absent" (nil) is distinguishable from "present and zero":

{{< snippet file="go/scenarios/01_absent_null/fixed.go" >}}

Rust's direct `HashMap` indexing panics loudly on a missing key -- a full process abort for what might be routine missing data:

{{< snippet file="rust/scenarios/01_absent_null/output.txt" lang="text" >}}

Kotlin's absence is already visible in the type -- `Map.get()` returns `V?`, not `V` -- so the compiler already knows "count" might not be there; `!!` is an explicit, deliberate opt-in to crash if that type turns out to be wrong at runtime, not a gap in the type system:

{{< snippet file="kotlin/scenarios/01_absent_null/output.txt" lang="text" >}}

Python's direct dict indexing raises a specific `KeyError`, and TypeScript's declared type lies silently (`count: undefined` at runtime despite a `number` annotation) -- both detailed fully under scenario 8, where the same pattern shows up against real untrusted JSON from the mock server.

C# splits the same way Go does, but on a different axis: a fresh project template's nullable-reference-types annotation makes violating a non-null return type a **build warning**, not an error, and the program still compiles and crashes at the point of use; separately, `Dictionary`'s indexer throws `KeyNotFoundException` while `TryGetValue` returns `false` with no exception -- the same two-styles-of-absence split as Python's `d[k]` vs. `d.get(k)`:

{{< snippet file="csharp/scenarios/01_absent_null/broken/Program.cs" >}}

{{< snippet file="csharp/scenarios/01_absent_null/output.txt" lang="text" >}}

| Language | Behavior on absent field |
|---|---|
| C | undefined behavior (segfault here) |
| Go | silent zero-fill, no error |
| Rust | panic (`HashMap` index) or `None` (`.get()`) |
| Kotlin | `V?` from `Map.get()` by default; `!!` is an explicit opt-in crash |
| C# | nullable-reference-types violation is a build *warning*, not an error; `Dictionary` indexer throws, `TryGetValue` doesn't |
| Python | `KeyError` |
| TypeScript | `undefined`, type system says otherwise |

#### 2. Data race

The C baseline: a plain `gcc -O0 -g -Wall broken.c -o broken -lpthread` build (no sanitizer involved) with two pthreads incrementing an unguarded shared counter -- the race is visible directly in the wrong final count, no detector tool needed to see it:

{{< snippet file="c/scenarios/02-data-race/output.txt" lang="text" >}}

Go's race detector (`go run -race`) catches the race explicitly, naming the exact conflicting accesses:

{{< snippet file="go/scenarios/02_data_race/output.txt" lang="text" >}}

Rust's type system makes the naive version (plain shared mutable state across threads, no synchronization) a **compile error** -- there's no safe way to even write the C/Go-style mistake. Two concrete attempts confirm it: capturing a plain mutable counter in two `thread::spawn` closures fails the borrow checker, and sharing an `Rc<RefCell<i64>>` (instead of `Arc<Mutex<i64>>`) across threads fails a `Send` trait bound, because `Rc` is deliberately not thread-safe:

{{< snippet file="rust/scenarios/02_data_race/compile_reject_output.txt" lang="text" >}}

Getting an actual Rust data race requires opting into `unsafe`:

{{< snippet file="rust/scenarios/02_data_race/broken.rs" >}}

{{< snippet file="rust/scenarios/02_data_race/output.txt" lang="text" >}}

**Precise claim, not a looser one:** safe Rust prevents *data races* (unsynchronized concurrent access to shared mutable memory) at compile time. It does not prevent all *race conditions* (e.g., two threads racing to acquire a lock first, with an order-dependent result, still compiles and runs) or deadlocks (two threads each waiting on a lock the other holds also compiles fine and can hang forever) -- those remain logic errors the type system doesn't catch.

Python's and Rust's plain tight-loop increments (200,000 iterations, no induced delay) did **not** reproduce a lost update on an initial attempt -- a mismatch with the prediction, recorded in PREDICTIONS.md. Inserting an explicit yield between the read and the write (simulating the gap a slower real-world read-modify-write would have) fixed that: both lost an update in every one of 5 runs once the yield was added. Python's final version also reduced the loop to 200 iterations per thread to keep the capture practical (its own output shows `expected 400`, not 400,000); Rust kept the full 200,000:

{{< snippet file="python/scenarios/02_data_race/output.txt" lang="text" >}}

C, Kotlin, and Go (below) lost an update in every one of 5 runs with no induced delay needed. TypeScript lost one in 4 of 5 runs, and C# in 3 of 5 -- still a clear, repeated demonstration of the race, just not as consistent as the others. A 5-run sample is enough to show a race exists and roughly how often it showed up on this machine on this day; it is not a precise measurement of the true failure rate, and these exact counts aren't guaranteed to repeat elsewhere:

{{< snippet file="kotlin/scenarios/02_data_race/output.txt" lang="text" >}}

{{< snippet file="typescript/scenarios/02_data_race/output.txt" lang="text" >}}

{{< snippet file="csharp/scenarios/02_data_race/output.txt" lang="text" >}}

#### 3. Integer overflow

C: signed overflow is undefined behavior, not merely "wraps" -- GCC happened to wrap at `-O0`, but that's not a guarantee:

{{< snippet file="c/scenarios/03-integer-overflow/output.txt" lang="text" >}}

Go wraps silently and **by specification** (two's complement, defined behavior, still usually a bug):

{{< snippet file="go/scenarios/03_integer_overflow/output.txt" lang="text" >}}

Rust surprised the prediction here: a statically-known overflow (`let x: i32 = i32::MAX; x + 1`) is now rejected at **compile time** by rustc's deny-by-default `arithmetic_overflow` lint, not just caught at runtime in debug builds:

{{< snippet file="rust/scenarios/03_integer_overflow/output.txt" lang="text" >}}

Kotlin's `Int` wraps like Java's (no exception, two's complement):

{{< snippet file="kotlin/scenarios/03_integer_overflow/output.txt" lang="text" >}}

Python has arbitrary-precision integers, so there is nothing to overflow -- a fact, not a win, since it trades overflow bugs for the cost of boxed, slower integer arithmetic:

{{< snippet file="python/scenarios/03_integer_overflow/output.txt" lang="text" >}}

TypeScript's `number` is a 64-bit float; past `Number.MAX_SAFE_INTEGER` it silently loses precision, a different failure mode from classic wraparound but still a silent wrong answer:

{{< snippet file="typescript/scenarios/03_integer_overflow/output.txt" lang="text" >}}

C#'s default is also silent, defined wraparound (like Go/Kotlin) -- but the SAME plain source line throws `OverflowException` under either an explicit `checked(...)` expression or the project-level `CheckForOverflowUnderflow` MSBuild setting, with no `checked` keyword anywhere in the source for the latter:

{{< snippet file="csharp/scenarios/03_integer_overflow/output.txt" lang="text" >}}

| Language | Overflow behavior |
|---|---|
| C | undefined behavior |
| Go | silent, defined wraparound |
| Rust | compile-time error (const-evaluable case) / debug panic / release wrap |
| Kotlin | silent, defined wraparound (like Java) |
| C# | silent, defined wraparound by default; `checked` or a project setting throws `OverflowException` |
| Python | no overflow (arbitrary precision) |
| TypeScript | silent precision loss past 2^53 (not wraparound) |

#### 4. Ignored errors

C: `fopen`'s NULL return is never checked, so the program dereferences a NULL `FILE*` on the next line:

{{< snippet file="c/scenarios/04-ignored-errors/output.txt" lang="text" >}}

Go: the error return is discarded with `_`, and the program proceeds with a zero-value result as if it were real data:

{{< snippet file="go/scenarios/04_ignored_errors/output.txt" lang="text" >}}

Rust: discarding a `#[must_use]` `Result` by itself produces a compiler **warning**, not an error -- it still compiles and runs:

{{< snippet file="rust/scenarios/04_ignored_errors/output.txt" lang="text" >}}

Python and Kotlin both require an active, deliberate swallow (`except: pass`, or an empty `catch` block) to ignore an error -- neither language will let you discard an error "by accident" the way Go's `_` or Rust's bare statement does:

{{< snippet file="python/scenarios/04_ignored_errors/output.txt" lang="text" >}}

{{< snippet file="kotlin/scenarios/04_ignored_errors/output.txt" lang="text" >}}

TypeScript: calling an async function without `await` or `.catch` lets execution continue past the error immediately, and current Node's default behavior crashes the process asynchronously once the unhandled rejection surfaces -- a deviation from the initial prediction that it would just log and continue indefinitely:

{{< snippet file="typescript/scenarios/04_ignored_errors/output.txt" lang="text" >}}

C#: a swallowed `catch {}` behaves like Python/Kotlin's equivalent. The more interesting case is a "fire-and-forget" `Task` -- started, never awaited, never stored, never observed -- whose body throws: unlike Node's unhandled-promise-rejection-is-fatal default, .NET does **not** crash the process; the exception is silently absorbed:

{{< snippet file="csharp/scenarios/04_ignored_errors/output.txt" lang="text" >}}

#### 5. Resource cleanup and leaks

C, built with `-fsanitize=address` to get a leak detector: an unfreed `malloc` is a straightforward, detectable leak:

{{< snippet file="c/scenarios/05-resource-cleanup/output.txt" lang="text" >}}

Go, Python, Kotlin, TypeScript, and C# are all garbage collected and don't leak in the C sense, but none of them guarantee *when* a resource (a file descriptor, here) gets released without an explicit `defer`/`with`/`use`/`finally`/`using`:

{{< snippet file="go/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

{{< snippet file="python/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

{{< snippet file="kotlin/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

{{< snippet file="typescript/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

{{< snippet file="csharp/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

Rust's case is the one worth stating precisely. An `Rc` reference cycle (two nodes each holding a strong `Rc` to the other) leaks -- neither node's `Drop` ever runs, because neither's strong count ever reaches zero:

{{< snippet file="rust/scenarios/05_resource_cleanup/broken.rs" >}}

{{< snippet file="rust/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

**This is memory-safe. It is not leak-free.** Rust's guarantee is deterministic `Drop` for values that *do* drop, plus no use-after-free and no double-free; it does not promise leak-freedom against reference cycles. The [Rust book's chapter on reference cycles](https://doc.rust-lang.org/book/ch15-06-reference-cycles.html) states plainly that preventing memory leaks is not one of Rust's guarantees, and that an `Rc<T>`/`RefCell<T>` cycle is exactly the mechanism that causes one (the reference count never reaches zero, so the values never drop) -- `Weak<T>` for the back-reference breaks the cycle, which is the fix here:

{{< snippet file="rust/scenarios/05_resource_cleanup/fixed.rs" >}}

### Concurrency

#### 6. Hung request and cancellation

Unlike every other scenario in this post, **this one has no broken/fixed pair at all.** All six core `wordfreq` implementations already wire `--timeout` through to their HTTP client correctly -- timeout handling was part of the baseline CLI contract from the start, not something fixed for this post. What follows is the baseline CLI itself, pointed at the mock server's `/hang` endpoint (which sleeps for an hour and never responds) with `--timeout 2`:

{{< snippet file="shared/scenarios/06_hung_cancellation/output.txt" lang="text" >}}

All six cancel at roughly the configured timeout rather than hanging. Each language's cancellation mechanism, read from the existing CLI source rather than written new for this scenario: Python's `urlopen(..., timeout=...)`, Go's `http.Client{Timeout: ...}`, Rust's `ureq` agent-level timeout, TypeScript's `AbortController` + `fetch(..., { signal })`, Kotlin's `java.net.http` per-request/connect timeouts, and C#'s `CancellationTokenSource` passed to `HttpClient.GetAsync`. Full detail in `shared/scenarios/06_hung_cancellation/README.md`. The "broken" counterpart in every language would simply be constructing the HTTP client *without* a timeout at all (e.g., Go's `http.DefaultClient`), which would hang for the mock server's full sleep duration -- described rather than run to completion, since capturing that verbatim isn't practical.

#### 7. CPU-heavy work blocking an event loop

Every language in this scenario runs the identical workload (300,000,000 `sqrt` additions) with the result printed, so no compiler can eliminate the loop as dead code and the comparison is apples-to-apples -- an equalization pass that caught a real mistake in the first draft: see the Go callout below.

Node: a synchronous CPU-bound loop on the single JS thread blocks a 100ms heartbeat completely:

{{< snippet file="typescript/scenarios/07_cpu_heavy_blocking/broken.ts" >}}

{{< snippet file="typescript/scenarios/07_cpu_heavy_blocking/output.txt" lang="text" >}}

Offloading the same work to a `worker_thread` (its own OS thread) frees the event loop:

{{< snippet file="typescript/scenarios/07_cpu_heavy_blocking/fixed.ts" >}}

Python asyncio shows the identical pattern: a synchronous CPU-bound call inside an async function with no `await` points blocks the event loop completely; moving it to `loop.run_in_executor` frees it, though that's scheduling around the blocking call, not true parallelism -- the executor thread still contends for the GIL with the event loop thread in the default (non-free-threaded) build:

{{< snippet file="python/scenarios/07_cpu_heavy_blocking/output.txt" lang="text" >}}

Go's counterexample needed no fix at all: a heartbeat goroutine keeps ticking on schedule *while* a CPU-bound loop runs concurrently, because Go's scheduler runs goroutines across real OS threads rather than cooperatively on one:

{{< snippet file="go/scenarios/07_cpu_heavy_blocking/output.txt" lang="text" >}}

**Investigation, not just a result:** an early run of this scenario reported Go finishing the same workload roughly 8x faster than Node, which turned out to be invalid. Inspecting `go build -gcflags=-S` on a reconstruction of that version confirms the exact mechanism: the compiler inlined the loop into `main`, proved the accumulated sum was never read, and deleted the floating-point work entirely, leaving only an empty counting loop -- not a measurement bug, and not unique to this run:

{{< snippet file="go/scenarios/07_cpu_heavy_blocking/dead_code_evidence_output.txt" lang="text" >}}

Printing the result (as every language here now does, so no compiler can prove it unused) made Go and Node land within about 3% of each other in this run. Full notes in `go/scenarios/07_cpu_heavy_blocking/NOTES.md`.

Rust (tokio) and Kotlin (coroutines) both show the same two-part result once actually run, which only partly matched the prediction: a single-threaded runtime/dispatcher stalls completely (0 ticks, matching Node/Python), but simply running the SAME unfixed code on the default multi-thread runtime / `Dispatchers.Default` did **not** stall either -- this machine's thread pool had a spare thread for the heartbeat even without `spawn_blocking`/`withContext` applied. That is not evidence the mistake is harmless in general; both ecosystems' own documentation recommends the explicit fix specifically because a saturated pool would show the stall this single demo didn't:

{{< snippet file="rust/scenarios/07_cpu_heavy_blocking/output.txt" lang="text" >}}

{{< snippet file="kotlin/scenarios/07_cpu_heavy_blocking/output.txt" lang="text" >}}

C# needed no fix either, for a related but distinct reason: there is no single-threaded event loop to block in the first place -- `async`/`await` runs on the thread pool, which has multiple worker threads by default, so a heartbeat `Task` keeps ticking regardless of whether the CPU-bound call is wrapped in `Task.Run` or called directly. The first run used `dotnet run`'s default Debug configuration and took roughly 1000ms, versus Go/Node's ~400ms -- a gap that, like the Go case above, turned out to be a build-configuration artifact rather than a real language difference. Re-running with `dotnet run -c Release` dropped it to ~400-410ms, matching Go and Node closely:

{{< snippet file="csharp/scenarios/07_cpu_heavy_blocking/output.txt" lang="text" >}}

<!-- AUTHOR-TODO: candidate observation -- Python's officially-supported
free-threaded build (3.14t, PEP 779) was looked up for this scenario but never
actually built or run (see VERSIONS.md, corrected after an earlier pass) -- every Python
result in this post is the default GIL build, confirmed by the printed
sys._is_gil_enabled() in each output. A genuine free-threaded run against this
scenario is a real candidate for "what I'd learn next," not something to assume. -->

### Data handling

#### 8. Parsing untrusted JSON

The mock server's `/json/missing-field` and `/json/wrong-type` endpoints, hit directly (not through the `wordfreq` CLI) by small parse-only programs in each language.

Go's `encoding/json` splits its behavior in a way that's easy to miss if you only test one case: silent zero-fill for the **absent** field, but a real decode **error** for the **wrong-typed** field:

{{< snippet file="go/scenarios/08_untrusted_json/broken.go" >}}

{{< snippet file="go/scenarios/08_untrusted_json/output.txt" lang="text" >}}

TypeScript's `as Item` assertion lies cleanly through both cases -- `tsc` reports zero errors on either call, and the wrong type only surfaces once the value is actually used:

{{< snippet file="typescript/scenarios/08_untrusted_json/broken.ts" >}}

{{< snippet file="typescript/scenarios/08_untrusted_json/output.txt" lang="text" >}}

Python's direct dict indexing crashes on the **first** bad payload (missing-field) and never even reaches the second -- an unhandled `KeyError` takes down the whole batch, a different failure mode from Go's silent per-item zero-fill:

{{< snippet file="python/scenarios/08_untrusted_json/output.txt" lang="text" >}}

Written defensively -- explicit validation at the boundary instead of trusting the declared/inferred type -- all three converge on the same behavior: reject the payload and say why.

{{< snippet file="typescript/scenarios/08_untrusted_json/fixed.ts" >}}

Rust (`serde_json`) and Kotlin (`kotlinx.serialization`) both land in the same place as each other once actually run: a plain, non-defaulted field errors on **both** the missing-field and wrong-type payloads -- strict by default, unlike Go. Giving the field an explicit default value makes **only** the missing-field case silently zero-fill (a deliberate opt-in counterpoint, mirroring Go); the wrong-type case still correctly errors either way, because a default only fills an absent field, it doesn't relax the type of a field that's present but wrong-typed:

{{< snippet file="rust/scenarios/08_untrusted_json/output.txt" lang="text" >}}

{{< snippet file="kotlin/scenarios/08_untrusted_json/output.txt" lang="text" >}}

C#'s `System.Text.Json` splits exactly like Go for a plain property: silent zero-fill on the **missing** field, a thrown `JsonException` on the **wrong-typed** one. The `required` modifier (C# 11+) closes the gap, making both cases throw:

{{< snippet file="csharp/scenarios/08_untrusted_json/broken/Program.cs" >}}

{{< snippet file="csharp/scenarios/08_untrusted_json/output.txt" lang="text" >}}

### Productivity

Measured three times each, back to back, on one development machine, 2026-10-28; median reported, range in parentheses. Full methodology and the "reproduce this" commands are in `companion-code/programming-languages-then-and-now/results/productivity.md`.

#### 9. Baseline CLI size and dependency count

| Language | Lines of code (wordfreq only) | Direct third-party dependencies |
|---|---|---|
| Python | 56 | 0 (stdlib only) |
| Kotlin | 85 | 1 (`kotlinx-coroutines-core`) + its transitive deps |
| Rust | 101 | 2 (`ureq`, `clap`) -- ~30 transitive crates |
| TypeScript | 113 | 2 dev-only (`typescript`, `@types/node`); 0 runtime deps |
| Go | 137 | 0 (stdlib only) |
| C# | 91 | 0 (BCL only) |

#### 10. Clean build vs. incremental rebuild time

| Language | Clean build (`--no-cache`) | Incremental rebuild (layer cache warm) |
|---|---|---|
| Python | 1.6s (1.5-1.7s) | 1.1s (1.1-1.2s) |
| C# | 4.5s (4.4-4.8s) | 1.3s (1.2-1.4s) |
| TypeScript | 4.6s (4.5-5.5s) | 1.0s (0.9-1.0s) |
| Go | 6.2s (6.2-6.9s) | 1.2s (1.2-1.2s) |
| Rust | 14.0s (12.7-14.3s) | 1.1s (1.1-1.2s) |
| Kotlin | 66.4s (66.1-70.6s) | 1.0s (1.0-1.2s) |

Incremental rebuild times converge to roughly the same ~1-1.4s across all six once the Docker layer cache is warm and nothing in the source changed; the spread is in the clean build. Kotlin's clean build downloads Gradle 9.5.0 inside the build stage every time `--no-cache` is used. Rust's clean build compiles ~30 transitive crates from source every time.

#### 11. Final container image size

Idiomatic multi-stage builds.

| Language | Final image size | Base |
|---|---|---|
| Go | 19.5 MB | `gcr.io/distroless/static-debian12` |
| Rust | 129 MB | `debian:bookworm-slim` + `ca-certificates` |
| Python | 178 MB | `python:3.14.8-slim` |
| C# (Native AOT) | 207 MB | `runtime-deps:10.0` + native binary, no managed runtime |
| C# (self-contained) | 300 MB | `runtime-deps:10.0` + bundled private runtime copy |
| C# (framework-dependent, used elsewhere in this post) | 301 MB | `runtime:10.0` (shared runtime) |
| TypeScript | 349 MB | `node:26-bookworm-slim` |
| Kotlin | 481 MB | `eclipse-temurin:25-jre` + shadow jar |

All three C# variants build from the same `Program.cs`/`wordfreq.csproj`, varying only the publish mode (`dotnet publish -c Release`, `... --self-contained true -r linux-x64`, `... -p:PublishAot=true -r linux-x64`). Framework-dependent and self-contained landed almost identical in size -- the bundled private runtime in the self-contained build is roughly the same size as the shared runtime layer it replaces. Native AOT, the smallest of the three, built successfully with only `clang`/`zlib1g-dev` added to the build stage -- no separate toolchain install was needed -- and was verified to produce byte-identical `wordfreq` output to the other two variants.

### One footgun per language

So this post doesn't read as a scoreboard: one commonly-cited surprise per language, each one real, run, and captured.

**Go: the nil-interface gotcha.** An interface holding a nil *pointer* is not itself nil -- the interface value carries a non-nil type descriptor, so `err != nil` is true even when the underlying pointer is nil:

{{< snippet file="go/scenarios/footgun_nil_interface/broken.go" >}}

{{< snippet file="go/scenarios/footgun_nil_interface/output.txt" lang="text" >}}

**Python: the mutable default argument.** A default argument's value is created once, at function-definition time, and shared across every call that doesn't supply its own:

{{< snippet file="python/scenarios/footgun_mutable_default/broken.py" >}}

{{< snippet file="python/scenarios/footgun_mutable_default/output.txt" lang="text" >}}

**Kotlin: platform types from Java interop.** An unannotated Java API (here, `java.util.Properties.getProperty`, which predates Java's nullability annotations entirely) returns a "platform type" that Kotlin lets you treat as non-null with zero warnings -- it compiles clean and crashes at runtime:

{{< snippet file="kotlin/scenarios/footgun_platform_types/Broken.kt" >}}

{{< snippet file="kotlin/scenarios/footgun_platform_types/output.txt" lang="text" >}}

**Rust: the borrow checker rejects correct code.** A helper function that returns `&mut v[i]` can't prove to the compiler that two calls with different indices are disjoint, even though they provably never alias at runtime:

{{< snippet file="rust/scenarios/footgun_borrow_checker/broken.rs" >}}

{{< snippet file="rust/scenarios/footgun_borrow_checker/output.txt" lang="text" >}}

`split_at_mut` proves disjointness by construction and the compiler accepts it.

**TypeScript: type erasure makes `as` a promise, not a check.** A type assertion compiles clean and type-checks clean, then lies at runtime with no exception at the point of the lie:

{{< snippet file="typescript/scenarios/footgun_type_erasure/broken.ts" >}}

{{< snippet file="typescript/scenarios/footgun_type_erasure/output.txt" lang="text" >}}

**C#: `async void` swallows a try/catch that looks like it should work.** An exception thrown inside an `async void` method is NOT caught by a try/catch wrapped directly around the call site -- it's rethrown on the thread pool instead, because `async void` gives the caller no `Task` to await or observe:

{{< snippet file="csharp/scenarios/footgun_async_void/broken/Program.cs" >}}

{{< snippet file="csharp/scenarios/footgun_async_void/output.txt" lang="text" >}}

`async Task` plus `await` lets the exception propagate through the try/catch normally.

## Language notes

### Python

Targets: general-purpose scripting, data science/ML glue, automation. Tooling: pip/venv (or faster modern alternatives like `uv`), a huge PyPI ecosystem. Signature strength: approachable syntax and an unmatched breadth of libraries for a first attempt at almost anything. Signature footgun: mutable default arguments (above). Python 3.14 made the free-threaded (no-GIL) build officially supported via [PEP 779](https://peps.python.org/pep-0779/), though the default build still ships with the GIL and not every C-extension wheel has a free-threaded build yet.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### Go

Targets: networked services, CLI tooling, infrastructure software. Tooling: a single `go` command for building, testing, formatting, and dependency management (`go.mod`), no separate package manager to choose. Signature strength: goroutines and channels make concurrent I/O straightforward to write and reason about, backed by a scheduler that runs them across real OS threads (demonstrated directly in scenario 7). Signature footgun: the nil-interface gotcha (above).

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### Rust

Targets: systems programming, performance-critical services, anywhere C/C++ would traditionally be reached for but memory safety matters. Tooling: Cargo, widely regarded as one of the best package-manager-plus-build-tool combinations of any language, bundled from day one. Signature strength: the borrow checker eliminates whole categories of memory-safety bugs at compile time (no use-after-free, no data races in safe code, demonstrated directly in scenario 2). Signature footgun: the borrow checker rejecting correct code (above). Costs, stated plainly: the second-slowest clean build of the six core languages measured here -- 14.0s median, behind only Kotlin's 66.4s (`results/productivity.md`) -- and real friction when the borrow checker can't see through an abstraction you know is safe.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### TypeScript

Platform-anchored: it's a layer over JavaScript, which means it's anchored to the browser and Node.js ecosystems rather than being a general-purpose language with its own independent runtime story. Tooling: npm/pnpm/yarn, `tsc`, an enormous ecosystem inherited wholesale from JavaScript. Signature strength: a structural type system that catches entire classes of mistakes before runtime, when the types are trustworthy. Signature footgun, and the load-bearing caveat on that strength: types are fully erased at runtime (above) -- a type assertion is a promise, not a check.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### Kotlin

Platform-anchored: built by JetBrains, officially adopted by Google as a first-class Android language, and runs on the JVM (plus Kotlin/Native and Kotlin/JS targets). Tooling: Gradle (or Maven), full interop with the existing Java/Maven Central ecosystem. Signature strength: null safety baked into the type system for Kotlin-native code, concise syntax relative to Java for equivalent logic. Signature footgun: platform types erasing that same null safety at the Java interop boundary (above) -- note this is distinct from scenario 1's absent-value case, where absence IS visible in Kotlin's own types. Server-side adoption: JetBrains' own ["State of Kotlin 2026"](https://kotlinlang.org/state-of-kotlin-2026/) report states 61% of Kotlin backend developers use Spring/Spring Boot and 51% use Ktor -- vendor-reported (JetBrains surveying its own ecosystem), labeled as such here, not independent. JetBrains' [announcement of its strategic partnership with Spring](https://blog.jetbrains.com/kotlin/2025/05/strategic-partnership-with-spring/) (2025-05-22) states 27% of Spring developers have used Kotlin -- also vendor-reported.
<!-- AUTHOR-TODO: verify in a normal browser that this page supports: 27% of Spring developers have used Kotlin, dated 2025-05-22 -->
[RedMonk's Q1 2026 language rankings](https://redmonk.com/sogrady/2026/04/14/language-rankings-1-26/) are independent and rank Kotlin 14th, tied with Scala.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### C#

Platform-anchored in its history (.NET and the CLR, originally Windows-only), general-purpose and genuinely cross-platform today (.NET runs natively on Linux and macOS, and this post's own C# container runs on Linux throughout) -- a useful contrast with Kotlin's JVM anchoring and Swift's Apple anchoring, both still platform-rooted in a way C#/.NET no longer is. Tooling: the `dotnet` CLI covers build/test/publish/run with no separate package manager to choose, NuGet for packages. Signature strength: the base class library covers an unusually wide surface (HTTP, JSON, regex, concurrency primitives) without reaching for a single third-party package, demonstrated directly in this post's own CLI and every C# scenario. Signature footgun: `async void` swallowing exceptions past a try/catch that looks like it should work (above). .NET 10 is the current LTS release; Native AOT publishing produced this post's smallest C# container image (207 MB) with no toolchain beyond `clang`/`zlib1g-dev` in the build stage.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### Swift (prose only -- no project)

Swift (2014) exists because Apple wanted a safer, more modern replacement for Objective-C across its own platforms, keeping Objective-C/C/C++ interop so existing codebases could migrate incrementally. It's the default choice for iOS/macOS/watchOS/visionOS application development.

A short illustrative snippet, **not run as part of this repo** (no Swift project exists here):

```swift
// Not run in this repo -- illustrative only.
func fetchCount(from record: [String: Int]) -> Int? {
    record["count"] // Optional<Int>, absence is in the type, same idea as Rust's Option
}
```

Server-side Swift (Vapor, Hummingbird) is documented by [swift.org's own "Swift on Server" page](https://www.swift.org/documentation/server/), which describes them as "web frameworks ... which provide a variety of tools and libraries to streamline the development process"; adoption outside Apple-centric teams is limited, and the tooling/platform experience on Linux is reported as noticeably different from developing on macOS -- a measured note, not a dismissal.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### Zig (not implemented in this post)

Looked up 2026-10-28: Zig 0.17.0 is current and brought incremental compilation to `x86_64-linux`; Linux x86-64 container support is solid. Implementation (companion code, scenarios, benchmarking) is not done yet. A dated "moving target" note: Zig has not yet reached a 1.0 release, and its standard library (including `std.http`) has a documented history of breaking changes between minor versions.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### Mojo (not implemented in this post)

Looked up 2026-10-28: Mojo reached 1.0 on 2026-08-11, 1.1 on 2026-09-17, and the compiler/stdlib went fully open source under Apache-2.0-with-LLVM-exceptions shortly after 1.0. Official Linux support exists; Windows support is still being added. Implementation is not done yet. If it happens: real HTTP would be attempted first against the mock server, with a documented fallback to reading pre-fetched fixture files if the pinned version's HTTP support isn't there yet, and Zig/Mojo would stay excluded from the scenario metrics comparison regardless. A dated "moving target" note: despite reaching 1.0, Mojo is young enough that its HTTP client/server maturity at the exact pinned version is an open question until actually tried.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

## Results at a glance

A factual summary, drawn only from the recorded results above and in the companion-code `results/` and `PREDICTIONS.md` files. No rankings, no verdicts -- those are mine to draw separately, not baked into this table.

| Scenario | Notable finding |
|---|---|
| 1. Absent/null | Go zero-fills silently; Rust/Python panic/raise loudly; Kotlin's `Map.get()` is already nullable, `!!` is an opt-in crash; C#'s nullable-warning is a build warning not an error, Dictionary indexer throws but TryGetValue doesn't; TypeScript's type lies |
| 2. Data race | Go's `-race` catches it directly; Rust requires `unsafe` to even write it (two safe-Rust attempts confirmed as compile-time rejections, not runtime races) -- precise claim: safe Rust prevents data races, not all race conditions or deadlocks; plain tight loops in Python/Rust needed an induced delay before they'd lose an update at all (mismatch vs. prediction), after which both lost one in 5 of 5 runs; C, Kotlin, and Go also lost one in 5 of 5 runs with no induced delay; TypeScript did in 4 of 5, C# in 3 of 5 |
| 3. Integer overflow | C: UB; Go/Kotlin/C# (default): silent defined wrap; Rust: compile-time error for the const case (mismatch vs. prediction), else debug-panic/release-wrap; C#'s `checked`/project setting throws `OverflowException`; Python: no overflow; TS: silent precision loss, not wraparound |
| 4. Ignored errors | Go/Rust let it through (warning only in Rust); Python/Kotlin require an active swallow; TS's unhandled rejection crashes the process asynchronously (mismatch vs. prediction); C#'s unobserved fire-and-forget Task exception is silently absorbed, no crash |
| 5. Resource cleanup | C: detected leak via ASan; Rust's `Rc` cycle leaks but is memory-safe (deterministic Drop, not leak-freedom); every GC'd language (Go, Python, Kotlin, TypeScript, C#) needs explicit cleanup for deterministic timing |
| 6. Hung request | All six core CLIs cancel correctly at the configured timeout; no broken/fixed pair exists for this scenario (stated plainly, not implied) |
| 7. CPU-heavy blocking | Node and Python asyncio block completely (0 heartbeat ticks) until offloaded to a thread; Go/Rust(multi-thread runtime)/Kotlin(Dispatchers.Default)/C# all kept ticking without the explicit fix in this run, which is a fact about this run's spare thread capacity, not proof the fix is unnecessary; the original ~8x Go-vs-Node gap was investigated and found to be Go's unused-result dead-code elimination, not a real difference |
| 8. Untrusted JSON | Go and C# (without `required`) split the same way: silent on absence, error on wrong type; Rust (serde) and Kotlin (kotlinx.serialization) are strict on both by default, with an explicit default-value opt-in mirroring Go; TS's assertion lies through both; Python crashes on the first bad payload and never reaches the second |
| 9-11. Productivity | See `results/productivity.md` (median of 3 runs) -- Go smallest image (19.5 MB) and fastest clean build; Kotlin largest image (481 MB) and slowest clean build (Gradle/JVM cold start); C#'s Native AOT variant (207 MB) was its smallest of three image variants tried |

## What I'd learn next

<!-- AUTHOR-TODO: fill in. Scaffold: "if you come from ___, consider ___" -->

- If you come from ___, consider ___.
- If you come from ___, consider ___.

## What I didn't test, how to reproduce, and sources

**What this post didn't test**: large-codebase maintainability over time, build-system behavior at monorepo scale, CPU-bound (as opposed to I/O-bound) throughput under real load, memory usage under sustained load, cross-compilation, debugging/IDE experience, hiring/ecosystem-maturity considerations, and Zig and Mojo implementations (not done yet).

**Reproduce this**: clone [the repo](https://github.com/russfrith/russfrith-blog), `cd companion-code/programming-languages-then-and-now`, `docker compose up -d mockserver`, then `docker compose run --rm <language> --top N --concurrency N --timeout SECONDS /data/urls-happy.txt` for any of `python`, `go`, `rust`, `typescript`, `kotlin`, `csharp` -- those `docker compose` commands work the same on any OS. `./scripts/run-scenarios.ps1` runs every scenario against the pinned containers and diffs the result against what's committed here; `./scripts/run-acceptance.ps1` runs the shared CLI acceptance suite plus the independent coreutils-only verification. Both of those two scripts are PowerShell specifically; each language's own `scenarios/README.md` gives the plain `docker run` commands for that language's scenarios individually, which aren't PowerShell-specific.

**Versions and sources**: [`VERSIONS.md`](https://github.com/russfrith/russfrith-blog/blob/master/companion-code/programming-languages-then-and-now/VERSIONS.md) (this post's version pins and lookup sources), [`PREDICTIONS.md`](https://github.com/russfrith/russfrith-blog/blob/master/companion-code/programming-languages-then-and-now/PREDICTIONS.md) (predictions recorded before each run, with mismatches flagged).

