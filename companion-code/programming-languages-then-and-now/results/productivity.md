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
| Rust | 93 | 2 (`ureq`, `clap`) -- 56 total via `cargo generate-lockfile`; `[[package]]` entries in Cargo.lock, root excluded (direct + transitive) |
| TypeScript | 106 | 2 dev-only (`typescript`, `@types/node`); 0 runtime deps -- 3 total via `npm install --package-lock-only`; entries in package-lock.json's packages map, root excluded (direct + transitive) |
| Kotlin | 78 | 1 (`kotlinx-coroutines-core`) -- 5 total via `gradle dependencies --configuration runtimeClasspath`; unique group:artifact pairs, `(c)` constraint lines excluded, bundled into the shadow jar |

Go, Python, and C# all reach zero direct third-party dependencies for the CLI
itself by relying entirely on their standard library / base class library.

## 10. Clean build vs. incremental rebuild time (median of 3 runs; range in parentheses)

| Language | Clean build (`--no-cache`) | Incremental rebuild (layer cache warm) |
|---|---|---|
| Python | 1.4s (1.31-1.46s) | 0.97s (0.95-0.99s) |
| C# | 4.0s (3.96-4.66s) | 1.17s (1.17-1.21s) |
| TypeScript | 4.9s (4.43-4.95s) | 0.95s (0.94-0.99s) |
| Go | 6.06s (6.02-6.54s) | 1.32s (1.3-1.38s) |
| Rust | 11.36s (11.22-12.79s) | 1.14s (1.13-1.2s) |
| Kotlin | 73.66s (69.37-74.4s) | 1.0s (1-1.22s) |

Incremental rebuild times converge to roughly the same ~0.95-1.3s across all six once
the Docker layer cache is warm and nothing in the source changed; the real spread
is in the clean build. Kotlin's clean build still includes downloading Gradle
9.5.0 inside the build stage every time `--no-cache` is used; a production setup
would cache that distribution in a named volume. Rust's clean build compiles 56
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
