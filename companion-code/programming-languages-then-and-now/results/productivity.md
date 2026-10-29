# Productivity observations (scenarios 9-11)

Measured for all six core languages, back to back in one session, on one development machine (Windows 11 Pro, Docker Desktop/WSL2, host otherwise idle), 2026-10-29, by `scripts/measure-productivity.ps1` -- this file is GENERATED from `results/productivity.json`, never hand-edited; see that file for raw per-run numbers, tool versions, and the exact capture timestamp. Median and range reported, labeled observations, not benchmarks. Reproduce with:

```
./scripts/measure-productivity.ps1
```

## 9. CLI size and dependency count

Lines of code: after trimming whitespace, a line counts if it's non-empty and doesn't start with the language's line-comment marker (`//` or `#`) -- a simple, consistent rule, not a semantic parse (block comments and trailing inline comments aren't special-cased); this reads lower than a raw `wc -l` on the same file.

| Language | Lines of code (wordfreq only) | Direct third-party dependencies |
|---|---|---|
| Python | 55 | 0 (stdlib only) |
| C# | 84 | 0 (BCL only) -- 0 total via `dotnet restore; entries in obj/project.assets.json's libraries map` |
| TypeScript | 106 | 2 dev-only (`typescript`, `@types/node`); 0 runtime deps -- 3 total via `npm install --package-lock-only; entries in package-lock.json's packages map, root excluded (direct + transitive)` |
| Go | 130 | 0 (stdlib only) |
| Rust | 93 | 2 (ureq, clap) -- 57 total via `cargo generate-lockfile; [[package]] entries in Cargo.lock, root excluded (direct + transitive)` (50 actually compiled on Linux via `clean cargo build --release; unique 'Compiling <name> v...' lines in the build log, root excluded`) |
| Kotlin | 78 | 1 (kotlinx-coroutines-core) -- 5 total via `gradle dependencies --configuration runtimeClasspath; unique group:artifact pairs, (c) constraint lines excluded` |

Go, Python, and C# all reach zero direct third-party dependencies for the CLI itself by relying entirely on their standard library / base class library.

## 10. Clean build, no-op rebuild, and rebuild after a one-line change (median of 3 runs; range in parentheses)

Three columns, three different things:
- **Clean build**: `docker compose build --no-cache`. Every layer rebuilt from scratch.
- **No-op rebuild (Docker layer cache check)**: `docker compose build` again with NOTHING changed. This only proves Docker skipped re-running the build steps because their inputs didn't change -- it says nothing about the compiler/runtime's own incremental behavior.
- **Rebuild after a one-line change (warm toolchain cache)**: a real edit -- one comment line appended to the source -- rebuilt with the language's own build command, inside a container with the toolchain's real caches (package registry, build cache, NuGet packages, etc.) already warm from an untimed clean build. This is what a developer actually feels after a small edit. Python has no compile step, so its number here is only an interpreter + argparse process start, not a rebuild.

| Language | Clean build (`--no-cache`) | No-op rebuild (Docker layer cache check) | Rebuild after a one-line change (warm toolchain cache) |
|---|---|---|---|
| Python | 1.29s (1.23-1.47s) | 1.12s (1.09-1.13s) | 0.79s (0.78-0.8s) (process start, not a rebuild -- Python has no compile step) |
| C# | 4.12s (3.93-4.77s) | 1.21s (1.17-1.21s) | 2.36s (2.34-2.39s) |
| TypeScript | 5.45s (5.25-5.97s) | 1.03s (1.02-1.09s) | 1.09s (1.08-1.1s) |
| Go | 6.81s (6.73-7.37s) | 1.25s (1.22-1.46s) | 0.7s (0.69-0.72s) |
| Rust | 15.32s (15.04-16.51s) | 1.11s (1.06-1.11s) | 0.93s (0.92-0.93s) |
| Kotlin | 92.2s (84.02-95.69s) | 1.04s (1.03-1.08s) | 11.39s (11.2-11.63s) |

Kotlin's clean build still includes downloading Gradle 9.5.0 inside the build stage every time `--no-cache` is used; a production setup would cache that distribution in a named volume. Rust's clean build resolves 57 packages (direct + transitive) and actually compiles 50 crates on Linux.

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

All three built from the same `Program.cs`/`wordfreq.csproj`, varying only the publish mode:

- **Framework-dependent** (`csharp/Dockerfile`, used for the CLI service everywhere else in this post): `dotnet publish -c Release`, runs on `mcr.microsoft.com/dotnet/runtime:10.0`. 287.4 MB.
- **Self-contained** (`csharp/Dockerfile.selfcontained`): `dotnet publish -c Release -r linux-x64 --self-contained true`, bundles a private copy of the runtime, runs on the smaller `runtime-deps:10.0` base. 286.2 MB.
- **Native AOT** (`csharp/Dockerfile.aot`): `dotnet publish -c Release -r linux-x64 -p:PublishAot=true`, also on `runtime-deps:10.0`. 197.8 MB -- the smallest of the three, and the only one shipping a fully native binary with no JIT or managed runtime at all.

Native AOT built successfully in this container with ONE extra build-stage dependency beyond the SDK image: `clang` and `zlib1g-dev`. No separate native toolchain installation outside the container was needed. The resulting binary was verified to run and produce byte-identical `wordfreq` output to the other two variants.
