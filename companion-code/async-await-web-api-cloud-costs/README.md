# Async/await web API cloud costs

Companion code for the blog post [Async/Await and What It Actually Costs You in the Cloud](../../content/posts/async-await-web-api-cloud-costs/index.md).

Everything here runs in Docker. No local .NET SDK, k6, or Python install is required.

## What's here

- `src/Api/` - the ASP.NET Core benchmark API. Eight endpoints share identical business logic (one downstream HTTP call, or a fan-out of several) but differ in how they handle concurrency: `/async-correct`, `/sync-over-async`, `/blocking-sync`, `/fanout-unbounded`, `/fanout-bounded`, `/async-no-cancellation`, `/async-with-cancellation`, `/cpu-bound`.
- `src/StubService/` - a downstream dependency with a configurable, fixed artificial latency (`X-Stub-Latency-Ms` header or `latencyMs` query string), used to simulate a slow downstream call.
- `src/DiagTools/` - a sidecar container running `dotnet-counters`, attached to the `api` container's process via a shared PID namespace, to capture thread pool and GC metrics during a run.
- `load/` - k6 load test scripts, parameterized by endpoint, target latency, and concurrency.
- `scripts/` - result parsing, the cost model, and chart generation (all run in containers).
- `results/` - raw output, the consolidated summary, live-fetched prices, the cost table, and charts. Committed, not gitignored (except scratch temp files).
- `docker-compose.yml` - wires everything together on an isolated `bench` network, with `--profile diag` and `--profile load` for the sidecar/load-generator jobs that aren't needed for every invocation.

## Running it

### Start the API and stub

```
docker compose up -d stub api
```

The API is published on `http://localhost:5080`. Try an endpoint:

```
curl "http://localhost:5080/async-correct?latencyMs=50"
```

### Run the thread pool / GC counter sidecar

```
docker compose --profile diag run --rm dotnet-counters <output-name> <duration-seconds>
```

Writes `results/raw/<output-name>.json`. Requires `api` to already be running (shares its PID namespace).

### Run a load test

```
docker compose --profile load run --rm k6 run /scripts/bench.js
```

Environment variables (passed via `-e`) select the scenario: `ENDPOINT`, `LATENCY_MS`, `STAGES` (JSON), `ABORT_FRACTION` (for the cancellation endpoints), `FANOUT_N`. See `load/bench.js` for defaults.

### Run the full orchestrated matrix

```
docker compose --profile tools run --rm runner
```

`runner` mounts the Docker socket so it can drive `docker compose` for you, a "Docker-outside-of-Docker" pattern, so the only host requirement remains Docker itself. **Caveat confirmed on this machine (Windows + Docker Desktop):** the nested `docker compose` invocation inside `runner` resolves relative bind-mount paths (`./load`, `./results/raw`) against its own container filesystem, not the host's, which breaks the k6/results mounts. On Linux hosts (including most CI runners) this generally isn't an issue. **The results actually committed to this repo were produced with `scripts/run_matrix.ps1`**, a host-side PowerShell equivalent that issues the exact same `docker compose` commands directly against the host daemon, sidestepping the path-translation problem entirely. If you're on Windows, use that script; `run_matrix.py`/the `runner` service are the documented, portable path for everyone else, and a welcome target for a path-mapping fix.

This builds `api`/`stub`, runs every endpoint at both stub latencies (`cpu-bound` once, since stub latency doesn't apply to it) across the configured concurrency steps, writes raw k6 JSON to `results/raw/`, and a consolidated `results/summary.csv`.

**Scope note on this run:** the full spec called for 5 concurrency steps (50/100/250/500/1000 VUs) x 3 repetitions per endpoint/latency combination. The committed `results/` in this repo reflect a reduced matrix (3 concurrency steps: 50/200/500 VUs, 1 repetition per combination) run in a single sitting on a shared development machine, documented plainly in `results/summary.csv` and in the post. The matrix definition is parameterized (`STEPS` / `REPETITIONS` in `run_matrix.py`, `$steps` / `$repetitions` in `run_matrix.ps1`), so running the full stepped/repeated matrix is a config change, not a rewrite; it just takes substantially longer.

The `dotnet-counters` sidecar is not wired into every matrix iteration (early testing showed `--duration` needs `dd:hh:mm:ss` formatting, not raw seconds, since fixed in `src/DiagTools/collect.sh`; running it on every one of 45+ iterations would also meaningfully slow the whole run down). Instead, run it standalone alongside one saturating load test when you want a thread-pool-queue-length trace (see "Run the thread pool / GC counter sidecar" above); that's how `results/raw/saturation_counters.json` and the thread pool chart were produced.

### Price fetch and cost model

```
docker compose --profile tools run --rm cost-model
```

Fetches current Azure App Service, AWS Fargate, and AWS Lightsail list prices live (needs outbound network access; this is the only stage that does), saves them to `results/prices.json`, and writes `results/cost_table.md` from `results/summary.csv`. Defaults to target RPS values of 1,000 / 2,000 / 5,000 / 20,000 and a 0.65 headroom factor. To use different targets, run the script directly with its CLI flags instead of through the fixed compose command, e.g.:

```
docker run --rm -v "${PWD}:/workspace" -w /workspace python:3.13-slim sh -c \
  "pip install --quiet requests==2.32.3 && python3 scripts/cost_model.py --target-rps 500,2000,10000 --headroom 0.65"
```

### Charts

```
docker compose --profile tools run --rm charts
```

Writes PNGs to `results/charts/`, copied into the post's folder (`content/posts/async-await-web-api-cloud-costs/`), flat alongside `index.md`, matching this blog's convention.

## Last known-good run

2026-10-26, on the machine described in `results/environment.md`. Headline: `async-correct` sustained roughly 9x the throughput of `sync-over-async` at 50 concurrent users before the latter collapsed (under 5 req/s past 200 concurrent users, p95 past 44 seconds); `blocking-sync` never met the sustainability bar at any tested concurrency. Full numbers: `results/summary.csv`, `results/cost_table.md`, `results/prices.json`. This run used the reduced matrix described above, not the full spec.
