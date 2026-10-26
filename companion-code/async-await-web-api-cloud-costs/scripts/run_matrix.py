#!/usr/bin/env python3
"""Orchestrates the benchmark matrix: for every endpoint x stub latency x concurrency
step, (re)starts api+stub fresh, collects dotnet-counters, runs k6, snapshots container
stats, and appends one row per run to results/summary.csv.

Scope note: ENDPOINTS/LATENCIES/STEPS/REPETITIONS below are a reduced matrix (fewer
concurrency steps, 1 repetition) run in a single sitting on a shared dev machine --
see README.md "Scope note on this run". Increase STEPS/REPETITIONS to reproduce the
full spec; nothing else needs to change.
"""
import csv
import json
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
RESULTS_RAW = ROOT / "results" / "raw"
SUMMARY_CSV = ROOT / "results" / "summary.csv"

COMPOSE = ["docker", "compose"]

# endpoint -> (latencies to test, fanout_n or None, abort_fraction)
ENDPOINTS = {
    "async-correct": ([50, 200], None, 0),
    "sync-over-async": ([50, 200], None, 0),
    "blocking-sync": ([50, 200], None, 0),
    "fanout-unbounded": ([50, 200], 10, 0),
    "fanout-bounded": ([50, 200], 10, 0),
    "async-no-cancellation": ([50, 200], None, 0.2),
    "async-with-cancellation": ([50, 200], None, 0.2),
    "cpu-bound": ([0], None, 0),  # stub latency is irrelevant; CPU-bound, no downstream call
}

STEPS = [50, 200, 500]  # VU targets -- reduced from the spec's [50,100,250,500,1000]
REPETITIONS = 1  # reduced from the spec's 3
WARMUP = "5s"
HOLD = "20s"
OVERHEAD_BUDGET_MS = 250  # sustainable rule: p95 < stub_latency_ms + OVERHEAD_BUDGET_MS


def run(cmd, **kwargs):
    print(f"$ {' '.join(cmd)}", flush=True)
    return subprocess.run(cmd, cwd=ROOT, check=True, **kwargs)


def wait_healthy(timeout=30):
    deadline = time.time() + timeout
    while time.time() < deadline:
        try:
            r = subprocess.run(
                ["docker", "compose", "exec", "-T", "api", "wget", "-qO-", "http://localhost:8080/healthz"],
                cwd=ROOT, capture_output=True, text=True, timeout=5,
            )
            if r.returncode == 0 and "ok" in r.stdout:
                return
        except Exception:
            pass
        time.sleep(1)
    raise RuntimeError("api did not become healthy in time")


def stage_json(hold):
    return json.dumps([
        {"duration": WARMUP, "target": 1},
        {"duration": hold, "target": 1},
    ])


def stages_for(vus, hold):
    return json.dumps([
        {"duration": WARMUP, "target": vus},
        {"duration": hold, "target": vus},
    ])


def docker_stats_api():
    r = subprocess.run(
        ["docker", "stats", "--no-stream", "--format", "{{json .}}",
         "async-await-web-api-cloud-costs-api-1"],
        capture_output=True, text=True,
    )
    if r.returncode != 0 or not r.stdout.strip():
        return None, None
    data = json.loads(r.stdout.strip().splitlines()[0])
    cpu = float(data["CPUPerc"].strip("%"))
    mem = data["MemUsage"].split("/")[0].strip()
    return cpu, mem


def main():
    RESULTS_RAW.mkdir(parents=True, exist_ok=True)
    rows = []
    run(["docker", "compose", "build", "stub", "api"])

    run_index = 0
    for endpoint, (latencies, fanout_n, abort_fraction) in ENDPOINTS.items():
        for latency_ms in latencies:
            threshold_ms = latency_ms + OVERHEAD_BUDGET_MS if latency_ms > 0 else 300
            for vus in STEPS:
                for rep in range(1, REPETITIONS + 1):
                    run_index += 1
                    name = f"{endpoint}_{latency_ms}ms_{vus}vus_rep{rep}"
                    print(f"\n==== [{run_index}] {name} ====", flush=True)

                    run(["docker", "compose", "up", "-d", "--force-recreate", "stub", "api"])
                    wait_healthy()

                    counters_proc = subprocess.Popen(
                        COMPOSE + ["--profile", "diag", "run", "--rm",
                                   "dotnet-counters", name, "35"],
                        cwd=ROOT,
                    )

                    time.sleep(1)  # let dotnet-counters attach before load starts

                    summary_path = f"/results/{name}_k6.json"
                    env_args = [
                        "-e", f"BASE_URL=http://api:8080",
                        "-e", f"ENDPOINT={endpoint}",
                        "-e", f"LATENCY_MS={latency_ms}",
                        "-e", f"FANOUT_N={fanout_n or 10}",
                        "-e", f"ABORT_FRACTION={abort_fraction}",
                        "-e", f"STAGES={stages_for(vus, HOLD)}",
                        "-e", f"P95_THRESHOLD_MS={threshold_ms}",
                    ]
                    try:
                        run(COMPOSE + ["--profile", "load", "run", "--rm", *env_args,
                                       "k6", "run", f"--summary-export={summary_path}",
                                       "/scripts/bench.js"])
                        k6_ok = True
                    except subprocess.CalledProcessError:
                        k6_ok = False  # k6 exits non-zero when thresholds fail; still have the summary

                    cpu, mem = docker_stats_api()
                    counters_proc.wait(timeout=60)

                    local_summary = RESULTS_RAW / f"{name}_k6.json"
                    rps = p95 = error_rate = None
                    if local_summary.exists():
                        data = json.loads(local_summary.read_text())
                        metrics = data.get("metrics", {})
                        rps = metrics.get("http_reqs", {}).get("rate")
                        p95 = metrics.get("http_req_duration", {}).get("p(95)")
                        error_rate = metrics.get("http_req_failed", {}).get("value")

                    sustainable = (
                        p95 is not None and error_rate is not None and
                        p95 < threshold_ms and error_rate < 0.01
                    )

                    rows.append({
                        "endpoint": endpoint,
                        "stub_latency_ms": latency_ms,
                        "threshold_p95_ms": threshold_ms,
                        "vus": vus,
                        "repetition": rep,
                        "rps": round(rps, 2) if rps else None,
                        "p95_ms": round(p95, 2) if p95 else None,
                        "error_rate": round(error_rate, 4) if error_rate is not None else None,
                        "api_cpu_percent": cpu,
                        "api_mem": mem,
                        "sustainable": sustainable,
                        "k6_thresholds_met": k6_ok,
                    })

                    run(["docker", "compose", "down"])

    with SUMMARY_CSV.open("w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)

    print(f"\nWrote {len(rows)} rows to {SUMMARY_CSV}")


if __name__ == "__main__":
    main()
