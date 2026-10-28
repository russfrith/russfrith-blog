# Pinned versions

Looked up 2026-10-28 (web search plus direct `--version` checks against the pulled images; see links). All containers pin exact tags, not `latest`, so results are reproducible.

| Component | Version | Base image | Source |
|---|---|---|---|
| Python | 3.14.8 (stable); 3.15.0 exists since 2026-10-09 but is too new for a settled `-slim` base image at lookup time | `python:3.14.8-slim` | [python.org downloads](https://www.python.org/), warp2search release notes |
| Python (free-threaded) | Not used in this repo. There is no official free-threaded Docker image as of this lookup -- the `docker-library/python` project has an open, unresolved request for official `3.14t` images, and only third-party/community images exist. Every Python scenario output prints `sys.version` and `sys._is_gil_enabled()` so each run states plainly which build it used; that is the default GIL build in every case here. | `python:3.14.8-slim` (GIL build, confirmed via printed `sys._is_gil_enabled()`) | [PEP 779](https://peps.python.org/pep-0779/); [docker-library/python discussion: "Freethreading builds for official Python container images"](https://discuss.python.org/t/freethreading-builds-for-official-python-container-images/108708) |
| Go | 1.26.4 | `golang:1.26.4-bookworm` | [go.dev/doc/devel/release](https://go.dev/doc/devel/release) |
| Rust | 1.96.1 (stable channel) | `rust:1.96.1-slim-bookworm` | [blog.rust-lang.org/releases](https://blog.rust-lang.org/releases) |
| Node.js / TypeScript runtime | Node 26 LTS (entered Active LTS 2026-10-28, the day of this lookup) | `node:26-bookworm-slim` | [Node.js release schedule](https://nodejs.org/en/about/previous-releases) |
| TypeScript compiler | latest `typescript` npm release compatible with Node 26 at lookup time, pinned exactly in `typescript/package.json` lockfile | n/a | npm registry |
| Kotlin | 2.4.0 | compiled on JDK, see below | [JetBrains Kotlin blog](https://blog.jetbrains.com/kotlin/) |
| JVM (for Kotlin) | Eclipse Temurin 25 (LTS), build 25.0.4.1+1, dated 2026-08-18, confirmed directly via `docker run --rm eclipse-temurin:25-jdk java -version` against the pulled image. | `eclipse-temurin:25-jdk` (build), `eclipse-temurin:25-jre` (runtime) | [Adoptium release notes](https://adoptium.net/) and direct `java -version` output from the pulled image, 2026-10-28 |
| Gradle (for Kotlin build only) | 9.5.0 -- Gradle 8.11.1 was tried first and failed against JDK 25 (`gradle shadowJar` aborted immediately with a version-detection error); 9.5.0 is the version JetBrains names as Kotlin 2.4.0's tested-compatible Gradle release | n/a (downloaded in the build stage, not a base image) | [JetBrains Kotlin 2.4.0 release notes](https://blog.jetbrains.com/kotlin/) |
| C compiler | GCC 16.2 | `gcc:16.2` | [gcc.gnu.org/gcc-16](https://gcc.gnu.org/gcc-16/) |
| .NET SDK / runtime (C#) | .NET 10 -- LTS (Long Term Support), confirmed via `docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 dotnet --version` → `10.0.402`, on 2026-10-28. (.NET 9 is the current STS release; .NET 10 is the LTS line pinned for this comparison.) | `mcr.microsoft.com/dotnet/sdk:10.0` (build), `mcr.microsoft.com/dotnet/runtime:10.0` (framework-dependent runtime image) | [.NET release notes / support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), direct `dotnet --version` output |
| Mock server | Python 3.14.8 stdlib only (`http.server`), no third-party deps | `python:3.14.8-slim` | n/a |

## Zig and Mojo

Not implemented. Looked up 2026-10-28 for a future go/no-go decision; no code written.

- **Zig**: 0.17.0 is current and brought incremental compilation to `x86_64-linux`; earlier 0.15.x/0.16.x already shipped Linux x86-64 builds and are present in distro repos (Arch `extra`, Alpine). Linux x86-64 container support is solid -- a plain `x86_64-linux-musl` or `-gnu` target builds and runs fine in a minimal container. Real HTTP should be attemptable with `std.http.Client`/`Server` in 0.17; fallback is pre-fetched fixture files if the pinned std.http API is unstable. Source: [Zig 0.17.0 release notes](https://ziglang.org/).
- **Mojo**: 1.1 released 2026-09-17, following Mojo 1.0 (2026-08-11) and the compiler/stdlib going fully open source under Apache-2.0-with-LLVM-exceptions on 2026-08-18. Official Linux support exists (Windows support is still being added); Docker images are mostly community-maintained rather than an official slim container at lookup time. HTTP client/server maturity in the stdlib is an open question. Source: [Modular Mojo 1.0 announcement](https://modular.com/blog/modular-26-5-mojo-1-0-is-here), [Phoronix: Mojo 1.1 released](https://www.phoronix.com/news/Mojo-1.1-Released).

## Other notes

- Python free-threading: officially supported (not experimental) as of 3.14 per PEP 779; GIL remains default unless the `t`-suffixed interpreter is used, and not every C-extension wheel has a free-threaded build yet. Not run in this repo -- every Python result in this post, scenario 7 included, is from the default GIL build (see the table row above).
