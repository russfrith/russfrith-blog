# Productivity observations (scenarios 9-11)

Measured for all six core languages, back to back in one session, three runs
each, on one development machine (Windows 11 Pro, Docker Desktop/WSL2, host
otherwise idle), 2026-10-29, by `scripts/measure-productivity.ps1` (not
hand-timed) -- see `results/productivity.json` for the raw per-run numbers,
tool versions, and capture timestamp this file is generated from. Median and
range reported, labeled observations, not benchmarks -- no isolated hardware,
no repeated trials beyond these three, no warm-vs-cold-cache control beyond
what's noted below. Reproduce with:

```
./scripts/measure-productivity.ps1
```

which runs the same `docker compose build --no-cache <service>` (clean),
`docker compose build <service>` (incremental), and `docker image inspect`
commands this file's numbers come from.

## 9. CLI size and dependency count

Lines of code: after trimming whitespace, a line counts if it's non-empty and
doesn't start with the language's line-comment marker (`//` or `#`) -- a
simple, consistent rule, not a semantic parse (block comments and trailing
inline comments aren't special-cased).

| Language | Lines of code (wordfreq only) | Direct third-party dependencies |
|---|---|---|
| Go | 130 | 0 (stdlib only) |
| Python | 55 | 0 (stdlib only) |
| C# | 84 | 0 (BCL only: `HttpClient`, `System.Text.RegularExpressions`, `SemaphoreSlim`, `CancellationTokenSource`) |
| Rust | 93 | 2 (`ureq`, `clap`) -- ~30 transitive crates |
| TypeScript | 106 | 2 dev-only (`typescript`, `@types/node`); 0 runtime deps |
| Kotlin | 78 | 1 (`kotlinx-coroutines-core`) + its transitive deps, bundled into the shadow jar |

Go, Python, and C# all reach zero direct third-party dependencies for the CLI
itself by relying entirely on their standard library / base class library.

## 10. Clean build vs. incremental rebuild time (median of 3 runs; range in parentheses)

| Language | Clean build (`--no-cache`) | Incremental rebuild (layer cache warm) |
|---|---|---|
| Python | 1.26s (1.16-1.52s) | 1.0s (0.93-1.0s) |
| C# | 4.27s (4.01-5.06s) | 1.25s (1.22-1.31s) |
| TypeScript | 4.38s (4.07-4.86s) | 0.95s (0.95-0.97s) |
| Go | 6.75s (6.28-7.27s) | 1.32s (1.28-1.34s) |
| Rust | 11.59s (11.42-13.23s) | 0.98s (0.97-1.0s) |
| Kotlin | 69.1s (65.27-71.1s) | 1.07s (1.06-1.07s) |

Incremental rebuild times converge to roughly the same ~1-1.3s across all six once
the Docker layer cache is warm and nothing in the source changed; the real spread
is in the clean build. Kotlin's clean build still includes downloading Gradle
9.5.0 inside the build stage every time `--no-cache` is used; a production setup
would cache that distribution in a named volume. Rust's clean build compiles ~30
transitive crates from source every time.

## 11. Final image size (idiomatic multi-stage build)

| Language | Final image size | Base |
|---|---|---|
| Go | 18.6 MB | `gcr.io/distroless/static-debian12` (static binary, no libc) |
| Rust | 122.9 MB | `debian:bookworm-slim` + `ca-certificates` (dynamically linked, glibc) |
| Python | 169.9 MB | `python:3.14.8-slim` (full interpreter + stdlib) |
| C# (Native AOT) | 197.8 MB | `mcr.microsoft.com/dotnet/runtime-deps:10.0` + a native binary, no managed runtime shipped |
| C# (self-contained) | 286.2 MB | `mcr.microsoft.com/dotnet/runtime-deps:10.0` + a bundled private copy of the .NET runtime |
| C# (framework-dependent, the default used elsewhere in this post) | 287.4 MB | `mcr.microsoft.com/dotnet/runtime:10.0` (shared .NET runtime image) |
| TypeScript | 332.5 MB | `node:26-bookworm-slim` (full Node runtime, in both build and final stages) |
| Kotlin | 459.1 MB | `eclipse-temurin:25-jre` (JRE) + the shadow jar |

### C# image-size variants

All three built from the same `Program.cs`/`wordfreq.csproj`, varying only the
publish mode:

- **Framework-dependent** (`csharp/Dockerfile`, used for the CLI service everywhere else in this post): `dotnet publish -c Release`, runs on `mcr.microsoft.com/dotnet/runtime:10.0`. 287.4 MB.
- **Self-contained** (`csharp/Dockerfile.selfcontained`): `dotnet publish -c Release -r linux-x64 --self-contained true`, bundles a private copy of the runtime, runs on the smaller `runtime-deps:10.0` base (no shared runtime needed). 286.2 MB -- about the same as framework-dependent, because the bundled private runtime copy is roughly the same size as the shared runtime image layer it replaces.
- **Native AOT** (`csharp/Dockerfile.aot`): `dotnet publish -c Release -r linux-x64 -p:PublishAot=true`, also on `runtime-deps:10.0`. 197.8 MB -- the smallest of the three, and the only one shipping a fully native binary with no JIT or managed runtime at all.

Native AOT built successfully in this container with ONE extra build-stage
dependency beyond the SDK image: `clang` and `zlib1g-dev` (installed via `apt-get`
in the build stage only; neither ships in the final image). No separate native
toolchain installation outside the container was needed. The resulting binary was
verified to run and produce byte-identical `wordfreq` output to the other two
variants.
