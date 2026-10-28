# Pinned versions

Looked up 2026-10-28 (web search; see links). All containers pin exact tags, not `latest`, so results are reproducible.

| Component | Version | Base image | Source |
|---|---|---|---|
| Python | 3.14.8 (stable); 3.15.0 exists since 2026-10-09 but is too new for a settled `-slim` base image at lookup time | `python:3.14.8-slim` | [python.org downloads](https://www.python.org/), warp2search release notes |
| Python (free-threaded, scenario 7 only) | 3.14.8, `t`-suffixed build (`python3.14t`), free-threading officially supported per PEP 779 as of 3.14 | built from `python:3.14.8-slim` with `--disable-gil` interpreter via `deadsnakes`-equivalent step documented in `python/README.md` | [PEP 779](https://peps.python.org/pep-0779/) |
| Go | 1.26.4 | `golang:1.26.4-bookworm` | [go.dev/doc/devel/release](https://go.dev/doc/devel/release) |
| Rust | 1.96.1 (stable channel) | `rust:1.96.1-slim-bookworm` | [blog.rust-lang.org/releases](https://blog.rust-lang.org/releases) |
| Node.js / TypeScript runtime | Node 26 LTS (entered Active LTS 2026-10-28, the day of this lookup) | `node:26-bookworm-slim` | [Node.js release schedule](https://nodejs.org/en/about/previous-releases) |
| TypeScript compiler | latest `typescript` npm release compatible with Node 26 at lookup time, pinned exactly in `typescript/package.json` lockfile | n/a | npm registry |
| Kotlin | 2.4.0 | compiled on JDK, see below | [JetBrains Kotlin blog](https://blog.jetbrains.com/kotlin/) |
| JVM (for Kotlin) | Eclipse Temurin 25 (LTS, released 2026-08-21, version 25.0.4.7) | `eclipse-temurin:25-jdk` (build), `eclipse-temurin:25-jre` (runtime) | [Adoptium release notes](https://adoptium.net/) |
| Gradle (for Kotlin build only) | 9.5.0 — Gradle 8.11.1 was tried first and failed against JDK 25 (`gradle shadowJar` aborted immediately with a version-detection error); 9.5.0 is the version JetBrains names as Kotlin 2.4.0's tested-compatible Gradle release | n/a (downloaded in the build stage, not a base image) | [JetBrains Kotlin 2.4.0 release notes](https://blog.jetbrains.com/kotlin/) |
| C compiler | GCC 16.2 | `gcc:16.2-bookworm` | [gcc.gnu.org/gcc-16](https://gcc.gnu.org/gcc-16/) |
| Mock server | Python 3.14.8 stdlib only (`http.server`), no third-party deps | `python:3.14.8-slim` | n/a |

## Zig and Mojo (Phase 2 — not implemented this phase)

Looked up 2026-10-28, recorded here for the Phase 2 approval decision; no code written yet.

- **Zig**: 0.17.0 is current and brought incremental compilation to `x86_64-linux`; earlier 0.15.x/0.16.x already shipped Linux x86-64 builds and are present in distro repos (Arch `extra`, Alpine). Linux x86-64 container support is solid — a plain `x86_64-linux-musl` or `-gnu` target builds and runs fine in a minimal container. Real HTTP should be attemptable with `std.http.Client`/`Server` in 0.17; fallback is pre-fetched fixture files if the pinned std.http API is unstable. Source: [Zig 0.17.0 release notes](https://ziglang.org/).
- **Mojo**: 1.1 released 2026-09-17, following Mojo 1.0 (2026-08-11) and the compiler/stdlib going fully open source under Apache-2.0-with-LLVM-exceptions on 2026-08-18. Official Linux support exists (Windows support is still being added); Docker images are mostly community-maintained rather than an official slim container at lookup time, so the Dockerfile for Phase 2 will build from the official Linux installer inside a standard base image. HTTP client/server maturity in the stdlib is the open question for Phase 2 — plan is "try real HTTP first, fall back to reading pre-fetched fixture files and document the deviation," per the post's contract. Source: [Modular Mojo 1.0 announcement](https://modular.com/blog/modular-26-5-mojo-1-0-is-here), [Phoronix: Mojo 1.1 released](https://www.phoronix.com/news/Mojo-1.1-Released).

## Other notes

- Python free-threading: officially supported (not experimental) as of 3.14 per PEP 779; GIL remains default unless the `t`-suffixed interpreter is used, and not every C-extension wheel has a free-threaded build yet. Used for scenario 7 (CPU-heavy work blocking an event loop) comparison only.
