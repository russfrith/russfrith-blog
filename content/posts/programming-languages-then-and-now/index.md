---
title: "Programming Languages, Then and Now"
description: "A hands-on comparison of Python, Go, Rust, TypeScript, and Kotlin (plus a C safety baseline) on the same small CLI task, and a look at why the language landscape looks the way it does."
date: 2026-10-27T20:00:00-04:00
draft: true
tags: ["python", "go", "rust", "typescript", "kotlin", "programming-languages"]
categories: ["programming"]
ShowToc: true
---

<!-- AUTHOR-TODO: opening anecdote -- my start in the 1980s, and my professor's claim
that reading a language's specification teaches you new languages. This post is,
among other things, a test of that claim: see the "what was familiar / what
surprised me" notes under each language below. -->

## Why languages exist

Most early general-purpose languages were built to solve one specific problem, not to be general-purpose in the way we'd use that word today.

- **Fortran** (1957) existed to make scientific and engineering numerical computation practical without hand-written assembly, on IBM's own hardware. Its name is literally "FORmula TRANslation."
- **COBOL** (1959) existed to make business data processing (payroll, inventory, billing) readable by non-programmers reviewing the code, which is why its syntax reads like stilted English.
- **BASIC** (1964) existed to make programming accessible to students outside engineering and science departments at Dartmouth, on a time-sharing system, deliberately simplified versus Fortran.
- **Pascal** (1970) existed to teach structured programming systematically, as a reaction against languages (including early BASIC and Fortran) that made it easy to write unstructured, `GOTO`-laden code.
- **C** (1972) existed to write an operating system (Unix) and its tools efficiently, portably, and close to the hardware, replacing the assembly Unix had been written in.
- **Ada** (1980) existed because the U.S. Department of Defense was paying for hundreds of incompatible languages across embedded and safety-critical defense systems and wanted one language with built-in support for strong typing, concurrency, and real-time constraints.

<!-- AUTHOR-TODO: candidate observation from writing this section -- how many of
these "one problem" origin stories map cleanly onto scenarios later in this post
(Ada's concurrency focus vs. scenario 6/7; Pascal's structure vs. the footguns). -->

## Eras and pivot points

The 1980s-1990s object-oriented era (C++, Java, C#) reframed "what problem does this solve" around managing complexity in large codebases through encapsulation and inheritance, not around a single computing domain the way Fortran or COBOL had been.

Web scripting in the mid-1990s to 2000s (Perl, PHP, then Python and Ruby) optimized for a different thing entirely: get a dynamic page in front of a browser quickly, with the language's job being to glue together HTML, a database, and some logic. Ruby on Rails (2004) was a pivot point within that era specifically because it bundled opinionated conventions (routing, an ORM, scaffolding) into the framework itself, which changed what "web scripting language" meant in practice even for languages that weren't Ruby.

Python's rise to its current dominance is widely attributed less to language design changes than to its library ecosystem, particularly NumPy, pandas, scikit-learn, and later PyTorch and TensorFlow, which made it the default glue language for data science and machine learning. The language itself changed relatively incrementally over that period; the ecosystem around it is what changed the calculation of "which language should I use for this."

## What changed after the 2000s

A cluster of new systems and application languages appeared from the mid-2000s onward -- Go (2009), Rust (2010 announced, 2015 stable 1.0), Swift (2014), Kotlin (2011), Zig (2016), Mojo (2023) -- at a pace that looks different from the preceding couple of decades. A few commonly cited, partial explanations, stated carefully because this is a contested, multi-causal historical question (see REVIEW.md for claims that didn't clear the bar for this post):

- **LLVM lowering the cost of building a compiler backend.** Writing a production-quality optimizing backend from scratch is a multi-year undertaking; LLVM let new language projects focus their effort on the frontend and type system instead. This is a commonly cited factor, not a universal one: **Go is an explicit counterexample** -- it uses a hand-written backend, not LLVM, and still shipped a usable, reasonably fast compiler.
- **Package managers built into the language or its official tooling from day one** (Cargo for Rust, Go modules, npm having already set the expectation for JavaScript). Older languages like C and C++ still don't have one standard, built-in answer to "how do I pull in a dependency," and that gap is routinely cited by newer-language designers as something they wanted to avoid repeating.
- **Corporate sponsors with a platform to push**: Go (Google, systems/infra), Rust (originally Mozilla, for Servo/Firefox), Swift (Apple, for its own platforms), Kotlin (JetBrains, then adopted by Google for Android). Having a company fund full-time compiler engineers for years is a different resourcing model than earlier languages often had.
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

Everything runs in Docker containers pinned to specific versions (full list and lookup sources in `companion-code/programming-languages-then-and-now/VERSIONS.md`, looked up 2026-10-28):

{{< snippet file="VERSIONS.md" lang="text" >}}

A shared mock HTTP server (Python stdlib `http.server`, no third-party dependencies) serves fixed pages, a 404, an endpoint that hangs, a large page, and two JSON endpoints with a missing field and a wrong-typed field, respectively. A connection-refused case is tested by pointing at an unused port. Nothing ever touches the live internet. A shared acceptance script runs every implementation against the same fixtures and diffs stdout against one expected-output file per fixture; all five core languages (Python, Go, Rust, TypeScript, Kotlin) pass it as of this writing.

**Honest limits of this comparison**: this is a small, I/O-bound task. It says very little about CPU-bound throughput, large-codebase maintainability, or how any of these languages feel after a year of real production use. The numbers under "productivity" below are single-machine observations from one development session, not controlled benchmarks -- no repeated trials, no isolated hardware. Treat everything quantitative in this post as "this is what happened, once, on my machine," not "this is the true relative performance of these languages."

## Scenarios

Ten fixed scenarios, grouped by theme, plus one signature footgun per language. C serves as a baseline for the five safety scenarios only (no full CLI). For each scenario, languages where something notable happened get full detail; the rest get a line or a table row.

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

Kotlin's `!!` operator forces past an already-nullable `Map.get()` result and crashes exactly the way `unwrap()`-equivalents do elsewhere:

{{< snippet file="kotlin/scenarios/01_absent_null/output.txt" lang="text" >}}

Python's direct dict indexing raises a specific `KeyError`, and TypeScript's declared type lies silently (`count: undefined` at runtime despite a `number` annotation) -- both detailed fully under scenario 8, where the same pattern shows up against real untrusted JSON from the mock server.

| Language | Behavior on absent field |
|---|---|
| C | undefined behavior (segfault here) |
| Go | silent zero-fill, no error |
| Rust | panic (`HashMap` index) or `None` (`.get()`) |
| Kotlin | crash via `!!`, or `null` if respected |
| Python | `KeyError` |
| TypeScript | `undefined`, type system says otherwise |

#### 2. Data race

The C baseline, built with GCC's ThreadSanitizer-adjacent `-fsanitize=address` run separately for leaks (scenario 5) and run directly here for the race itself via two pthreads incrementing an unguarded shared counter:

{{< snippet file="c/scenarios/02-data-race/output.txt" lang="text" >}}

Go's race detector (`go run -race`) catches the race explicitly, naming the exact conflicting accesses:

{{< snippet file="go/scenarios/02_data_race/output.txt" lang="text" >}}

Rust's type system makes the naive version (plain shared mutable state across threads, no synchronization) a **compile error** -- there's no safe way to even write the C/Go-style mistake. Getting an actual race requires opting into `unsafe`:

{{< snippet file="rust/scenarios/02_data_race/broken.rs" >}}

{{< snippet file="rust/scenarios/02_data_race/output.txt" lang="text" >}}

Python and Rust's plain tight-loop increments (200,000 iterations, no induced delay) did **not** reproduce a lost update reliably in this environment -- a mismatch with the initial prediction, recorded in PREDICTIONS.md. Inserting an explicit yield between the read and the write (simulating the gap a slower real-world read-modify-write would have) made both races reliable:

{{< snippet file="python/scenarios/02_data_race/output.txt" lang="text" >}}

Kotlin's and TypeScript's (worker_threads + `SharedArrayBuffer`) versions raced reliably with no induced delay:

{{< snippet file="kotlin/scenarios/02_data_race/output.txt" lang="text" >}}

{{< snippet file="typescript/scenarios/02_data_race/output.txt" lang="text" >}}

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

| Language | Overflow behavior |
|---|---|
| C | undefined behavior |
| Go | silent, defined wraparound |
| Rust | compile-time error (const-evaluable case) / debug panic / release wrap |
| Kotlin | silent, defined wraparound (like Java) |
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

#### 5. Resource cleanup and leaks

C, built with `-fsanitize=address` to get a leak detector: an unfreed `malloc` is a straightforward, detectable leak:

{{< snippet file="c/scenarios/05-resource-cleanup/output.txt" lang="text" >}}

Go, Python, Kotlin, and TypeScript are all garbage collected and don't leak in the C sense, but none of them guarantee *when* a resource (a file descriptor, here) gets released without an explicit `defer`/`with`/`use`/`finally`:

{{< snippet file="go/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

{{< snippet file="python/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

{{< snippet file="kotlin/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

{{< snippet file="typescript/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

Rust's case is the one worth stating precisely. An `Rc` reference cycle (two nodes each holding a strong `Rc` to the other) leaks -- neither node's `Drop` ever runs, because neither's strong count ever reaches zero:

{{< snippet file="rust/scenarios/05_resource_cleanup/broken.rs" >}}

{{< snippet file="rust/scenarios/05_resource_cleanup/output.txt" lang="text" >}}

**This is memory-safe. It is not leak-free.** Rust's guarantee is deterministic `Drop` for values that *do* drop, plus no use-after-free and no double-free; it does not promise leak-freedom against reference cycles. The Rust book states this explicitly: "it's possible to leak memory" with `Rc<T>` and `RefCell<T>` cycles, and recommends `Weak<T>` for the back-reference to break the cycle -- exactly the fix here:

{{< snippet file="rust/scenarios/05_resource_cleanup/fixed.rs" >}}

### Concurrency

#### 6. Hung request and cancellation

All five core `wordfreq` implementations already wire `--timeout` through to their HTTP client correctly -- there's no separate broken/fixed pair for this one, since timeout handling was part of the baseline CLI contract from the start. Pointed at the mock server's `/hang` endpoint (which sleeps for an hour and never responds) with `--timeout 2`:

{{< snippet file="shared/scenarios/06_hung_cancellation/output.txt" lang="text" >}}

All five cancel at roughly the configured timeout rather than hanging. The "broken" counterpart in every language is simply constructing the HTTP client *without* a timeout at all (e.g., Go's `http.DefaultClient`), which would hang for the mock server's full sleep duration -- described rather than run to completion, since capturing that verbatim isn't practical.

#### 7. CPU-heavy work blocking an event loop

Node: a synchronous CPU-bound loop on the single JS thread blocks a 100ms heartbeat completely:

{{< snippet file="typescript/scenarios/07_cpu_heavy_blocking/broken.ts" >}}

{{< snippet file="typescript/scenarios/07_cpu_heavy_blocking/output.txt" lang="text" >}}

Offloading the same work to a `worker_thread` (its own OS thread) frees the event loop:

{{< snippet file="typescript/scenarios/07_cpu_heavy_blocking/fixed.ts" >}}

Python asyncio shows the identical pattern: a synchronous CPU-bound call inside an async function with no `await` points blocks the event loop completely; moving it to `loop.run_in_executor` frees it, though that's scheduling around the blocking call, not true parallelism -- the executor thread still contends for the GIL with the event loop thread in the default (non-free-threaded) build:

{{< snippet file="python/scenarios/07_cpu_heavy_blocking/output.txt" lang="text" >}}

Go's counterexample needed no fix at all: a heartbeat goroutine keeps ticking on schedule *while* a CPU-bound loop runs concurrently, because Go's scheduler runs goroutines across real OS threads rather than cooperatively on one:

{{< snippet file="go/scenarios/07_cpu_heavy_blocking/output.txt" lang="text" >}}

Kotlin coroutines on a multi-threaded dispatcher (`Dispatchers.Default`) and Rust async tasks that avoid placing blocking work directly on an executor thread (using `spawn_blocking` instead) behave the same way as Go here; neither was run as a full separate demo, per the rule of showing full detail only where something notable happens.

<!-- AUTHOR-TODO: candidate observation -- Python's officially-supported
free-threaded build (3.14t, PEP 779) was looked up for this scenario but not run
head-to-head against the GIL build here. Worth a follow-up post on its own; see
"what I'd learn next." -->

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

Rust (`serde` with an `Option` field) and Kotlin (`kotlinx.serialization` with a nullable field) behave like the fixed Python/TypeScript versions when written defensively; neither was run as a full separate demo here.

### Productivity

#### 9. Baseline CLI size and dependency count

#### 10. Clean build vs. incremental rebuild time

#### 11. Final container image size

Measured once, on one development machine, 2026-10-28; full methodology and the "reproduce this" commands are in `companion-code/programming-languages-then-and-now/results/productivity.md`.

{{< snippet file="results/productivity.md" lang="text" >}}

### One footgun per language

So this post doesn't read as a scoreboard: one commonly-cited surprise per language, each one real, run, and captured.

**Go: the nil-interface gotcha.** An interface holding a nil *pointer* is not itself nil -- the interface value carries a non-nil type descriptor, so `err != nil` is true even when the underlying pointer is nil:

{{< snippet file="go/scenarios/footgun_nil_interface/broken.go" >}}

{{< snippet file="go/scenarios/footgun_nil_interface/output.txt" lang="text" >}}

**Python: the mutable default argument.** A default argument's value is created once, at function-definition time, and shared across every call that doesn't supply its own:

{{< snippet file="python/scenarios/footgun_mutable_default/broken.py" >}}

{{< snippet file="python/scenarios/footgun_mutable_default/output.txt" lang="text" >}}

**Kotlin: platform types from Java interop.** An unannotated Java API (here, `java.util.Properties.getProperty`, which predates Java's nullability annotations entirely) returns a "platform type" that Kotlin lets you treat as non-null with zero warnings -- it compiles clean and crashes at runtime:

{{< snippet file="kotlin/scenarios/footgun_platform_types/broken.kt" >}}

{{< snippet file="kotlin/scenarios/footgun_platform_types/output.txt" lang="text" >}}

**Rust: the borrow checker rejects correct code.** A helper function that returns `&mut v[i]` can't prove to the compiler that two calls with different indices are disjoint, even though they provably never alias at runtime:

{{< snippet file="rust/scenarios/footgun_borrow_checker/broken.rs" >}}

{{< snippet file="rust/scenarios/footgun_borrow_checker/output.txt" lang="text" >}}

`split_at_mut` proves disjointness by construction and the compiler accepts it.

**TypeScript: type erasure makes `as` a promise, not a check.** A type assertion compiles clean and type-checks clean, then lies at runtime with no exception at the point of the lie:

{{< snippet file="typescript/scenarios/footgun_type_erasure/broken.ts" >}}

{{< snippet file="typescript/scenarios/footgun_type_erasure/output.txt" lang="text" >}}

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

Targets: systems programming, performance-critical services, anywhere C/C++ would traditionally be reached for but memory safety matters. Tooling: Cargo, widely regarded as one of the best package-manager-plus-build-tool combinations of any language, bundled from day one. Signature strength: the borrow checker eliminates whole categories of memory-safety bugs at compile time (no use-after-free, no data races in safe code, demonstrated directly in scenario 2). Signature footgun: the borrow checker rejecting correct code (above). Costs, stated plainly: the slowest clean build of the five core languages measured here (companion-code results), and real friction when the borrow checker can't see through an abstraction you know is safe.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### TypeScript

Platform-anchored: it's a layer over JavaScript, which means it's anchored to the browser and Node.js ecosystems rather than being a general-purpose language with its own independent runtime story. Tooling: npm/pnpm/yarn, `tsc`, an enormous ecosystem inherited wholesale from JavaScript. Signature strength: a structural type system that catches entire classes of mistakes before runtime, when the types are trustworthy. Signature footgun, and the load-bearing caveat on that strength: types are fully erased at runtime (above) -- a type assertion is a promise, not a check.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### Kotlin

Platform-anchored: built by JetBrains, officially adopted by Google as a first-class Android language, and runs on the JVM (plus Kotlin/Native and Kotlin/JS targets). Tooling: Gradle (or Maven), full interop with the existing Java/Maven Central ecosystem. Signature strength: null safety baked into the type system for Kotlin-native code, concise syntax relative to Java for equivalent logic. Signature footgun: platform types erasing that same null safety at the Java interop boundary (above). Server-side adoption: JetBrains' own "State of Developer Ecosystem" survey reports usage figures for Kotlin on the server, but those are vendor-reported numbers, not an independent survey, and are called out as such rather than cited as settled fact (see REVIEW.md).

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

Server-side Swift (Vapor, Hummingbird) exists and is used in production in some shops, but adoption outside Apple-centric teams is limited, and the tooling/platform experience on Linux is reported as noticeably different from developing on macOS -- a measured note, not a dismissal.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### Zig (Phase 2 -- not yet implemented)

Looked up 2026-10-28: Zig 0.17.0 is current and brought incremental compilation to `x86_64-linux`; Linux x86-64 container support is solid. Implementation (companion code, scenarios, benchmarking) is planned for Phase 2 of this project, pending approval, after Python/Go/Rust/TypeScript/Kotlin/C land. A dated "moving target" note: Zig has not yet reached a 1.0 release, and its standard library (including `std.http`) has a documented history of breaking changes between minor versions.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

### Mojo (Phase 2 -- not yet implemented)

Looked up 2026-10-28: Mojo reached 1.0 on 2026-08-11, 1.1 on 2026-09-17, and the compiler/stdlib went fully open source under Apache-2.0-with-LLVM-exceptions shortly after 1.0. Official Linux support exists; Windows support is still being added. Implementation is planned for Phase 2. Per the original plan: real HTTP will be attempted first against the mock server, with a documented fallback to reading pre-fetched fixture files if the pinned version's HTTP support isn't there yet, and Zig/Mojo are excluded from the scenario metrics comparison regardless. A dated "moving target" note: despite reaching 1.0, Mojo is young enough that its HTTP client/server maturity at the exact pinned version is an open question until actually tried.

<!-- AUTHOR-TODO: what was familiar -->
<!-- AUTHOR-TODO: what surprised me -->

## Results at a glance

A factual summary, drawn only from the recorded results above and in the companion-code `results/` and `PREDICTIONS.md` files. No rankings, no verdicts -- those are mine to draw separately, not baked into this table.

| Scenario | Notable finding |
|---|---|
| 1. Absent/null | Go zero-fills silently; Rust/Python panic/raise loudly; Kotlin crashes via `!!`; TypeScript's type lies |
| 2. Data race | Go's `-race` catches it directly; Rust requires `unsafe` to even write it; plain tight loops in Python/Rust needed an induced delay to race reliably (mismatch vs. prediction); Kotlin/TS raced on the first try |
| 3. Integer overflow | C: UB; Go/Kotlin: silent defined wrap; Rust: compile-time error for the const case (mismatch vs. prediction), else debug-panic/release-wrap; Python: no overflow; TS: silent precision loss, not wraparound |
| 4. Ignored errors | Go/Rust let it through (warning only in Rust); Python/Kotlin require an active swallow; TS's unhandled rejection crashes the process asynchronously (mismatch vs. prediction) |
| 5. Resource cleanup | C: detected leak via ASan; Rust's `Rc` cycle leaks but is memory-safe (deterministic Drop, not leak-freedom); GC'd languages all need explicit cleanup for deterministic timing |
| 6. Hung request | All five core CLIs cancel correctly at the configured timeout |
| 7. CPU-heavy blocking | Node and Python asyncio block completely (0 heartbeat ticks) until offloaded to a thread; Go needed no fix |
| 8. Untrusted JSON | Go splits: silent on absence, error on wrong type; TS's assertion lies through both; Python crashes on the first bad payload and never reaches the second |
| 9-11. Productivity | See `results/productivity.md` -- Go smallest image (19.5 MB) and fastest clean build; Kotlin largest image (481 MB) and slowest clean build (Gradle/JVM cold start) |

## What I'd learn next

<!-- AUTHOR-TODO: fill in. Scaffold: "if you come from ___, consider ___" -->

- If you come from ___, consider ___.
- If you come from ___, consider ___.

## What I didn't test, how to reproduce, and sources

**What this post didn't test**: large-codebase maintainability over time, build-system behavior at monorepo scale, CPU-bound (as opposed to I/O-bound) throughput under real load, memory usage under sustained load, cross-compilation, debugging/IDE experience, hiring/ecosystem-maturity considerations, and (this phase) Zig and Mojo implementations.

**Reproduce this**: clone the repo, `cd companion-code/programming-languages-then-and-now`, `docker compose up -d mockserver`, then `docker compose run --rm <language> --top N --concurrency N --timeout SECONDS /data/urls-happy.txt` for any of `python`, `go`, `rust`, `typescript`, `kotlin`. `./scripts/run-acceptance.ps1` runs the full acceptance suite across all five. Each language's `scenarios/` folder has its own broken/fixed pairs with instructions in that folder's README.

**Versions and sources**: `companion-code/programming-languages-then-and-now/VERSIONS.md` (this post's version pins and lookup sources), `PREDICTIONS.md` (predictions recorded before each run, with mismatches flagged), `REVIEW.md` (claims that didn't clear the bar for this post as stated fact).
