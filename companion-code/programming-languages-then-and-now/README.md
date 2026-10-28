# Programming Languages, Then and Now -- companion code

Companion code for the blog post [Programming Languages, Then and Now](../../content/posts/programming-languages-then-and-now/index.md).

Everything runs in Docker. No local Python/Go/Rust/Node/JVM/.NET install is required.

## What's here

- `mockserver/` -- a Python-stdlib-only HTTP server used by every language as the shared test target. No third-party dependencies, no live internet access.
- `python/`, `go/`, `rust/`, `typescript/`, `kotlin/`, `csharp/` -- each has the shared `wordfreq` CLI (`Dockerfile` + source) and a `scenarios/` folder with broken/fixed pairs and captured verbatim output for scenarios 1-8 plus one signature footgun. `csharp/` additionally has `Dockerfile.selfcontained` and `Dockerfile.aot` for the two extra image-size variants measured in `results/productivity.md`.
- `c/` -- the safety-scenario baseline (scenarios 1-5 only, no CLI).
- `shared/` -- fixture URL lists, expected CLI output, the PowerShell acceptance script's results, the independent `verify-expected` coreutils check, and scenario 6's README (no broken/fixed pair, documents each language's cancellation mechanism).
- `scripts/run-acceptance.ps1` -- runs every core language's CLI against the shared fixtures, diffs stdout, and re-checks the happy-path fixture with the independent coreutils pipeline.
- `results/productivity.md` -- measured (not benchmarked) CLI size, build time, and image size figures for scenarios 9-11.
- `VERSIONS.md`, `PREDICTIONS.md`, `REVIEW.md` -- version pins with lookup sources, pre-run predictions with recorded mismatches, and claims held back from the post pending a better source.
- `zig/`, `mojo/` -- not yet implemented; Phase 2, pending approval.

## Running the shared CLI

```
docker compose up -d mockserver
docker compose run --rm python --top 5 --concurrency 4 --timeout 5 /data/urls-happy.txt
docker compose run --rm go --top 5 --concurrency 4 --timeout 5 /data/urls-happy.txt
docker compose run --rm rust --top 5 --concurrency 4 --timeout 5 /data/urls-happy.txt
docker compose run --rm typescript --top 5 --concurrency 4 --timeout 5 /data/urls-happy.txt
docker compose run --rm kotlin --top 5 --concurrency 4 --timeout 5 /data/urls-happy.txt
docker compose run --rm csharp --top 5 --concurrency 4 --timeout 5 /data/urls-happy.txt
```

`/data` inside each container is `shared/` on the host (read-only mount). `urls-happy.txt`, `urls-partial.txt`, and `urls-refused.txt` are the three fixture files; `expected-output/` holds what correct stdout looks like for the first two.

## Running the acceptance suite

```
docker compose up -d mockserver
./scripts/run-acceptance.ps1
```

Runs all six core languages against the happy-path, partial-failure, and missing-input-file fixtures, diffs stdout, checks exit codes (0/1/2), re-checks the happy-path fixture with the independent coreutils pipeline, and writes `shared/acceptance/RESULTS.md`.

## Running a scenario

Each language's `scenarios/<NN_name>/` folder has `broken.*` and `fixed.*` (or a single `demo.*`/plain file where there's no meaningful broken/fixed split), plus `output.txt` with the exact captured output and the toolchain version used. These were run directly against the pinned per-language base image, e.g.:

```
docker run --rm -v "${PWD}/python/scenarios/01_absent_null:/src" -w /src python:3.14.8-slim python broken.py
```

See each `output.txt` for the exact command used for that scenario (some need the mock server reachable on the shared `wordfreq` Docker network; `docker compose up -d mockserver` first, then add `--network programming-languages-then-and-now_wordfreq` to the `docker run` command).

## C baseline

```
docker run --rm -v "${PWD}/c/scenarios/01-absent-null:/src" -w /src gcc:16.2 bash -c "gcc -O0 -g -Wall broken.c -o broken && ./broken"
```

See `c/scenarios/*/output.txt` for the exact flags used per scenario (pthreads for the race, `-fsanitize=address` for the leak detector).
