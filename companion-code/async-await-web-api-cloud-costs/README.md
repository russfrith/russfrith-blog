# Async/await web API cloud costs

Companion code for the blog post [Async/await and what it actually costs you in the cloud](../../content/posts/async-await-web-api-cloud-costs/index.md).

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

(`runner` mounts the Docker socket so it can drive `docker compose` for you - a documented "Docker-outside-of-Docker" pattern - the only host requirement remains Docker itself.)

This builds `api`/`stub`, runs every endpoint at both stub latencies across the configured concurrency steps, writes raw k6 JSON + container stats to `results/raw/`, and a consolidated `results/summary.csv`.

**Scope note on this run:** the full spec called for 5 concurrency steps (50/100/250/500/1000 VUs) x 3 repetitions per endpoint/latency combination. The committed `results/` in this repo reflect a reduced matrix (fewer concurrency steps and 1 repetition per combination) run in a single sitting on a shared development machine, documented plainly in `results/summary.csv` and in the post. The matrix definition in `scripts/run_matrix.py` is parameterized, so running the full stepped/repeated matrix is a config change (`STEPS` and `REPETITIONS` constants), not a code change — it just takes substantially longer.

### Price fetch and cost model

```
docker compose --profile tools run --rm cost-model
```

Fetches current Azure and AWS list prices live (needs outbound network access; this is the only stage that does), saves them to `results/prices.json`, and writes `results/cost_table.md` from `results/summary.csv`. Takes `TARGET_RPS` values as an argument; see `scripts/cost_model.py --help`.

### Charts

```
docker compose --profile tools run --rm charts
```

Writes PNGs to `results/charts/`, copied into the post's `images/` folder.

## Last known-good run

See `results/environment.md` for host specs and `results/summary.csv` for the measured numbers. Run date and headline findings are recorded in the post itself; treat this companion repo as the source of truth for exact figures.
