#!/usr/bin/env python3
"""Generates the four required charts from results/summary.csv, results/cost_table.md
source data, and (if present) a dotnet-counters JSON trace, into results/charts/.

Color palette and chart conventions follow the project's dataviz skill: fixed
categorical hue order (never cycled), single axis, labeled units, light chart
surface, muted gridlines/axes.
"""
import csv
import json
from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd

ROOT = Path(__file__).resolve().parent.parent
RESULTS = ROOT / "results"
CHARTS = RESULTS / "charts"
CHARTS.mkdir(parents=True, exist_ok=True)

SURFACE = "#fcfcfb"
INK = "#0b0b0b"
MUTED = "#898781"
GRID = "#e1e0d9"
# Derived from the post's cover image (cloud-city-r4vDq.jpg: navy/blue sky,
# cyan/teal city lights, green data-stream glow), re-stepped and validated
# with the dataviz skill's validate_palette.js (light mode, surface #fcfcfb):
# all six categorical checks pass, worst adjacent pair ΔE 10.3 (CVD) / 23.3 (normal).
CATEGORICAL = ["#1d4ed8", "#d97706", "#0d9488", "#7c3aed",
               "#16a34a", "#be185d", "#0891b2", "#a16207"]

plt.rcParams.update({
    "figure.facecolor": SURFACE,
    "axes.facecolor": SURFACE,
    "axes.edgecolor": GRID,
    "axes.labelcolor": INK,
    "text.color": INK,
    "xtick.color": MUTED,
    "ytick.color": MUTED,
    "axes.grid": True,
    "grid.color": GRID,
    "grid.linewidth": 0.8,
    "font.family": "sans-serif",
    "font.size": 10,
})


def load_summary():
    df = pd.read_csv(RESULTS / "summary.csv")
    df["sustainable"] = df["sustainable"].astype(str).str.lower() == "true"
    return df


def chart_max_sustainable_rps(df):
    best = (df[df["sustainable"]]
            .groupby(["endpoint", "stub_latency_ms"])["rps"]
            .max().reset_index())
    endpoints = sorted(best["endpoint"].unique())
    latencies = sorted(best["stub_latency_ms"].unique())

    fig, ax = plt.subplots(figsize=(10, 6))
    width = 0.35
    x = range(len(endpoints))
    for i, latency in enumerate(latencies):
        vals = [best[(best.endpoint == e) & (best.stub_latency_ms == latency)]["rps"].max()
                for e in endpoints]
        vals = [v if pd.notna(v) else 0 for v in vals]
        offset = (i - (len(latencies) - 1) / 2) * width
        ax.bar([xi + offset for xi in x], vals, width,
               label=f"{latency}ms stub", color=CATEGORICAL[i % len(CATEGORICAL)])

    ax.set_xticks(list(x))
    ax.set_xticklabels(endpoints, rotation=35, ha="right")
    ax.set_ylabel("Max sustainable requests/sec")
    ax.set_title("Max sustainable RPS per endpoint variant, by stub latency")
    ax.legend(frameon=False)
    fig.tight_layout()
    fig.savefig(CHARTS / "max_sustainable_rps.png", dpi=150)
    plt.close(fig)


def chart_latency_vs_concurrency(df):
    targets = ["async-correct", "sync-over-async", "blocking-sync"]
    fig, axes = plt.subplots(1, 2, figsize=(12, 5), sharey=True)
    for ax, latency in zip(axes, [50, 200]):
        for i, endpoint in enumerate(targets):
            sub = df[(df.endpoint == endpoint) & (df.stub_latency_ms == latency)].sort_values("vus")
            if sub.empty:
                continue
            ax.plot(sub["vus"], sub["p95_ms"], marker="o", markersize=6,
                     color=CATEGORICAL[i], label=endpoint, linewidth=2)
        ax.set_xlabel("Concurrent virtual users")
        ax.set_title(f"{latency}ms stub latency")
    axes[0].set_ylabel("p95 latency (ms)")
    axes[0].legend(frameon=False)
    fig.suptitle("Latency vs. concurrency: async-correct vs. sync-over-async vs. blocking-sync")
    fig.tight_layout()
    fig.savefig(CHARTS / "latency_vs_concurrency.png", dpi=150)
    plt.close(fig)


def chart_threadpool_queue(counters_path):
    if not counters_path.exists():
        print(f"skip: {counters_path} not found (run the dotnet-counters demo first)")
        return
    data = json.loads(counters_path.read_text())
    events = data.get("Events", [])
    rows = [(e["timestamp"], e["value"]) for e in events
            if "thread_pool.queue.length" in e["name"]]
    if not rows:
        print("skip: no threadpool queue length samples found in counters trace")
        return
    df = pd.DataFrame(rows, columns=["timestamp", "value"])
    df["timestamp"] = pd.to_datetime(df["timestamp"])
    t0 = df["timestamp"].min()
    df["seconds"] = (df["timestamp"] - t0).dt.total_seconds()

    fig, ax = plt.subplots(figsize=(10, 5))
    ax.plot(df["seconds"], df["value"], color=CATEGORICAL[0], linewidth=2)
    ax.set_xlabel("Seconds into the saturation run")
    ax.set_ylabel("Thread pool queue length")
    ax.set_title("Thread pool queue length during a saturation run")
    fig.tight_layout()
    fig.savefig(CHARTS / "threadpool_queue_length.png", dpi=150)
    plt.close(fig)


def chart_cost_per_1000_rps():
    lines = (RESULTS / "cost_table.md").read_text().splitlines()
    rows = []
    for line in lines:
        if line.startswith("| ") and "Instances needed" not in line and "---" not in line:
            parts = [p.strip() for p in line.strip("|").split("|")]
            if len(parts) >= 6:
                try:
                    rows.append({
                        "endpoint": parts[0],
                        "latency": parts[1],
                        "instances": int(parts[3]),
                        "azure": float(parts[4].replace("$", "").replace(",", "")),
                    })
                except ValueError:
                    continue
    if not rows:
        print("skip: no rows parsed from cost_table.md")
        return
    df = pd.DataFrame(rows).drop_duplicates(subset=["endpoint", "latency"])
    df = df.sort_values("instances")

    fig, ax1 = plt.subplots(figsize=(10, 6))
    labels = [f"{r.endpoint}\n({r.latency})" for r in df.itertuples()]
    x = range(len(df))
    ax1.bar(x, df["instances"], color=CATEGORICAL[0])
    ax1.set_ylabel("Instances needed per 1,000 RPS")
    ax1.set_xticks(list(x))
    ax1.set_xticklabels(labels, rotation=40, ha="right", fontsize=8)
    ax1.set_title("Instances and Azure monthly cost per 1,000 RPS, by variant")
    for i, v in enumerate(df["azure"]):
        ax1.annotate(f"${v:,.0f}/mo", (i, df["instances"].iloc[i]),
                     textcoords="offset points", xytext=(0, 4), ha="center", fontsize=7, color=MUTED)
    fig.tight_layout()
    fig.savefig(CHARTS / "cost_per_1000_rps.png", dpi=150)
    plt.close(fig)


def main():
    df = load_summary()
    chart_max_sustainable_rps(df)
    chart_latency_vs_concurrency(df)
    chart_threadpool_queue(RESULTS / "raw" / "saturation_counters.json")
    if (RESULTS / "cost_table.md").exists():
        chart_cost_per_1000_rps()
    print(f"Charts written to {CHARTS}")


if __name__ == "__main__":
    main()
