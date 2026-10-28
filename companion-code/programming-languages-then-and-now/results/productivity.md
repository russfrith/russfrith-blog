# Productivity observations (scenarios 9-11)

Measured once, on one development machine (Windows 11 Pro, Docker Desktop/WSL2,
host otherwise idle), 2026-10-28. These are observations, not benchmarks -- no
repeated trials, no isolated hardware, no warm-vs-cold-cache control beyond what's
noted below. Reproduce with:

```
docker compose build --no-cache <service>   # clean
docker compose build <service>              # incremental, no source changes
docker images --format "{{.Repository}}\t{{.Size}}"
```

## 9. CLI size and dependency count

| Language | Lines of code (wordfreq only) | Direct third-party dependencies |
|---|---|---|
| Go | 137 | 0 (stdlib only: net/http, encoding/json not even needed here) |
| Python | 56 | 0 (stdlib only: urllib, argparse, concurrent.futures) |
| Rust | 101 | 2 (`ureq`, `clap`) -- pulled in ~30 transitive crates at build time |
| TypeScript | 113 | 2 dev-only (`typescript`, `@types/node`); 0 runtime deps (uses Node's built-in `fetch`) |
| Kotlin | 85 | 1 (`kotlinx-coroutines-core`) + whatever it pulls in, bundled into the shadow jar |

Go and Python need the most lines for the least direct dependency count; Rust and
Kotlin trade a shorter CLI for a larger transitive dependency tree pulled in by
one or two direct ones.

## 10. Clean build vs. incremental rebuild time

| Language | Clean build (`--no-cache`) | Incremental rebuild (no source changes, layer cache warm) |
|---|---|---|
| Go | 6.3s | 1.2s |
| Rust | 13.8s | 1.2s |
| Python | 1.4s | 0.9s |
| TypeScript | 4.5s | 1.0s |
| Kotlin | 69.4s | 1.3s |

Kotlin's clean build includes downloading Gradle 9.5.0 itself inside the build
stage (not cached across clean builds in this setup) plus the Gradle/Kotlin
compiler daemon cold-starting; a production setup would cache the Gradle
distribution and dependencies in a named volume, which would close most of this
gap. Rust's clean build compiles ~30 transitive crates from source every time
`--no-cache` is used, matching its reputation for slower clean builds.

## 11. Final image size (idiomatic multi-stage build)

| Language | Final image size | Base |
|---|---|---|
| Go | 19.5 MB | `gcr.io/distroless/static-debian12` (static binary, no libc even) |
| Rust | 129 MB | `debian:bookworm-slim` + `ca-certificates` (dynamically linked against glibc) |
| Python | 178 MB | `python:3.14.8-slim` (full interpreter + stdlib) |
| TypeScript | 349 MB | `node:26-bookworm-slim` (full Node runtime, twice -- build stage artifacts aren't trimmed further here) |
| Kotlin | 481 MB | `eclipse-temurin:25-jre` (JRE) + the shadow jar |

Go's static binary in a distroless base is by far the smallest. Rust's dynamically
linked release binary plus a minimal Debian base lands in the middle. The three
runtime-managed languages (Python, Node, JVM) carry their runtime into the final
image and are all meaningfully larger, with the JVM/JRE the largest of the three
here.
