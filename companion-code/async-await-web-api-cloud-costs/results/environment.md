# Benchmark environment

Captured 2026-10-26.

- CPU: Intel Core i5-12600K, 10 cores / 16 logical processors
- OS: Windows 11 Pro 10.0.26200
- Docker: Docker Desktop, Docker Engine 29.8.0, Compose v5.5.1 (WSL2 backend)
- .NET SDK (host): 10.0.401; containers pin `mcr.microsoft.com/dotnet/sdk:10.0` / `aspnet:10.0`
- k6: `grafana/k6:1.0.0` container image
- `api` container is constrained to `--cpus=1 --memory=2g`. `stub` is unconstrained.
- The k6 load generator runs in its own container but **shares the same physical host** as `api` and `stub` (no separate load-generation machine was available for this run). This means the host's other 15 logical processors are shared between the load generator, the Docker Desktop VM overhead, and the constrained `api` container. Results are directionally valid (all variants were measured under identical conditions) but absolute RPS numbers are not representative of an isolated cloud instance; see the post's caveats section.
