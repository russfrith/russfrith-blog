# Productivity observations (scenarios 9-11)

Measured for all six core languages, back to back in one session, three runs
each, on one development machine (Windows 11 Pro, Docker Desktop/WSL2, host
otherwise idle), 2026-10-28. Median and range reported, labeled observations,
not benchmarks -- no isolated hardware, no repeated trials beyond these three,
no warm-vs-cold-cache control beyond what's noted below. Reproduce with:

```
docker compose build --no-cache <service>   # clean
docker compose build <service>              # incremental, no source changes
docker images --format "{{.Repository}}\t{{.Size}}"
```

## 9. CLI size and dependency count

| Language | Lines of code (wordfreq only) | Direct third-party dependencies |
|---|---|---|
| Go | 137 | 0 (stdlib only) |
| Python | 56 | 0 (stdlib only) |
| C# | 91 | 0 (BCL only: `HttpClient`, `System.Text.RegularExpressions`, `SemaphoreSlim`, `CancellationTokenSource`) |
| Rust | 101 | 2 (`ureq`, `clap`) -- ~30 transitive crates |
| TypeScript | 113 | 2 dev-only (`typescript`, `@types/node`); 0 runtime deps |
| Kotlin | 85 | 1 (`kotlinx-coroutines-core`) + its transitive deps, bundled into the shadow jar |

Go, Python, and C# all reach zero direct third-party dependencies for the CLI
itself by relying entirely on their standard library / base class library.

## 10. Clean build vs. incremental rebuild time (median of 3 runs; range in parentheses)

| Language | Clean build (`--no-cache`) | Incremental rebuild (layer cache warm) |
|---|---|---|
| Python | 1.6s (1.5-1.7s) | 1.1s (1.1-1.2s) |
| C# | 4.5s (4.4-4.8s) | 1.3s (1.2-1.4s) |
| TypeScript | 4.6s (4.5-5.5s) | 1.0s (0.9-1.0s) |
| Go | 6.2s (6.2-6.9s) | 1.2s (1.2-1.2s) |
| Rust | 14.0s (12.7-14.3s) | 1.1s (1.1-1.2s) |
| Kotlin | 66.4s (66.1-70.6s) | 1.0s (1.0-1.2s) |

Incremental rebuild times converge to roughly the same ~1-1.4s across all six once
the Docker layer cache is warm and nothing in the source changed; the real spread
is in the clean build. Kotlin's clean build still includes downloading Gradle
9.5.0 inside the build stage every time `--no-cache` is used; a production setup
would cache that distribution in a named volume. Rust's clean build compiles ~30
transitive crates from source every time.

## 11. Final image size (idiomatic multi-stage build)

| Language | Final image size | Base |
|---|---|---|
| Go | 19.5 MB | `gcr.io/distroless/static-debian12` (static binary, no libc) |
| Rust | 129 MB | `debian:bookworm-slim` + `ca-certificates` (dynamically linked, glibc) |
| Python | 178 MB | `python:3.14.8-slim` (full interpreter + stdlib) |
| C# (Native AOT) | 207 MB | `mcr.microsoft.com/dotnet/runtime-deps:10.0` + a native binary, no managed runtime shipped |
| C# (self-contained) | 300 MB | `mcr.microsoft.com/dotnet/runtime-deps:10.0` + a bundled private copy of the .NET runtime |
| C# (framework-dependent, the default used elsewhere in this post) | 301 MB | `mcr.microsoft.com/dotnet/runtime:10.0` (shared .NET runtime image) |
| TypeScript | 349 MB | `node:26-bookworm-slim` (full Node runtime, in both build and final stages) |
| Kotlin | 481 MB | `eclipse-temurin:25-jre` (JRE) + the shadow jar |

### C# image-size variants

All three built from the same `Program.cs`/`wordfreq.csproj`, varying only the
publish mode:

- **Framework-dependent** (`csharp/Dockerfile`, used for the CLI service everywhere else in this post): `dotnet publish -c Release`, runs on `mcr.microsoft.com/dotnet/runtime:10.0`. 301 MB.
- **Self-contained** (`csharp/Dockerfile.selfcontained`): `dotnet publish -c Release -r linux-x64 --self-contained true`, bundles a private copy of the runtime, runs on the smaller `runtime-deps:10.0` base (no shared runtime needed). 300 MB -- about the same as framework-dependent, because the bundled private runtime copy is roughly the same size as the shared runtime image layer it replaces.
- **Native AOT** (`csharp/Dockerfile.aot`): `dotnet publish -c Release -r linux-x64 -p:PublishAot=true`, also on `runtime-deps:10.0`. 207 MB -- the smallest of the three, and the only one shipping a fully native binary with no JIT or managed runtime at all.

Native AOT built successfully in this container with ONE extra build-stage
dependency beyond the SDK image: `clang` and `zlib1g-dev` (installed via `apt-get`
in the build stage only; neither ships in the final image). No separate native
toolchain installation outside the container was needed. The resulting binary was
verified to run and produce byte-identical `wordfreq` output to the other two
variants.
