#!/usr/bin/env python3
"""Fetches current Azure and AWS list prices live and turns results/summary.csv into a
cost table: instances needed and monthly dollars per 1,000 RPS for each endpoint variant.

Azure: Retail Prices API (https://prices.azure.com/api/retail/prices), public, no auth.
AWS: public Price List bulk offer files (https://pricing.us-east-1.amazonaws.com/...),
public, no credentials required for the offer JSON.

Usage: python cost_model.py [--target-rps 1000,5000,20000] [--headroom 0.65]
"""
import argparse
import csv
import json
import statistics
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

import requests

ROOT = Path(__file__).resolve().parent.parent
RESULTS = ROOT / "results"

REGION = "eastus"  # Azure region slug
AWS_REGION = "us-east-1"


def fetch_azure_app_service_prices():
    """Finds Azure App Service Linux plans near 1 vCPU / 2GB and returns their retail price."""
    url = "https://prices.azure.com/api/retail/prices"
    params = {
        "$filter": f"serviceName eq 'Azure App Service' and armRegionName eq '{REGION}' "
                   f"and priceType eq 'Consumption'"
    }
    items = []
    while True:
        r = requests.get(url, params=params, timeout=30)
        r.raise_for_status()
        data = r.json()
        items.extend(data.get("Items", []))
        next_page = data.get("NextPageLink")
        if not next_page or len(items) > 3000:
            break
        url, params = next_page, None
    return items


def fetch_aws_fargate_prices():
    """Downloads the AWS Fargate (AmazonECS) region price index for us-east-1."""
    index_url = "https://pricing.us-east-1.amazonaws.com/offers/v1.0/aws/AmazonECS/current/region_index.json"
    r = requests.get(index_url, timeout=30)
    r.raise_for_status()
    region_file = r.json()["regions"][AWS_REGION]["currentVersionUrl"]
    r2 = requests.get(f"https://pricing.us-east-1.amazonaws.com{region_file}", timeout=60)
    r2.raise_for_status()
    return r2.json()


def pick_azure_plan(items):
    # vCPU/RAM for common App Service Linux plan tiers, from Azure's published specs.
    specs = {
        "B1": (1, 1.75), "B2": (2, 3.5),
        "S1": (1, 1.75), "S2": (2, 3.5),
        "P0V3": (1, 4), "P1V3": (2, 8),
    }
    candidates = []
    for item in items:
        name = item.get("productName", "") or ""
        sku_display = (item.get("skuName", "") or "").replace(" ", "").upper()
        if "Linux" not in name:
            continue
        if item.get("unitOfMeasure") != "1 Hour" or item.get("type") != "Consumption":
            continue
        if sku_display in specs:
            vcpu, ram = specs[sku_display]
            candidates.append((sku_display, vcpu, ram, item))

    def distance(c):
        _, vcpu, ram, _ = c
        return abs(vcpu - 1) * 10 + abs(ram - 2)

    if not candidates:
        return None
    best_tier, vcpu, ram, item = min(candidates, key=distance)
    return {
        "tier": best_tier,
        "vcpu": vcpu,
        "ram_gb": ram,
        "hourly_usd": item.get("retailPrice"),
        "sku": item.get("armSkuName"),
        "product": item.get("productName"),
    }


def fetch_aws_lightsail_prices():
    """Downloads the AWS Lightsail (AmazonLightsail) region price index for us-east-1."""
    index_url = "https://pricing.us-east-1.amazonaws.com/offers/v1.0/aws/AmazonLightsail/current/region_index.json"
    r = requests.get(index_url, timeout=30)
    r.raise_for_status()
    region_file = r.json()["regions"][AWS_REGION]["currentVersionUrl"]
    r2 = requests.get(f"https://pricing.us-east-1.amazonaws.com{region_file}", timeout=60)
    r2.raise_for_status()
    return r2.json()


def extract_lightsail_rate(price_data):
    """Finds the standard Linux Lightsail instance bundle closest to 2GB RAM.

    As of late 2026, AWS's smallest standard Lightsail Linux bundles are all
    2 vCPU (burstable), not 1 vCPU; there is no exact 1 vCPU / 2GB bundle
    published. The "2GB" bundle (2 vCPU, 2GB RAM) is the closest RAM match,
    so this is a generous comparison for Lightsail: more CPU than the
    benchmark's 1 vCPU container for the same price.
    """
    products = price_data["products"]
    terms = price_data["terms"]["OnDemand"]
    for sku, product in products.items():
        attrs = product.get("attributes", {})
        if attrs.get("servicecode") != "AmazonLightsail":
            continue
        if attrs.get("group") != "Lightsail Instance":
            continue
        if attrs.get("operatingSystem") != "Linux":
            continue
        if attrs.get("usagetype") != "USE1-BundleUsage:2GB":
            continue
        for _, term in terms.get(sku, {}).items():
            for _, dim in term["priceDimensions"].items():
                return {
                    "vcpu": float(attrs.get("vcpu", 0)),
                    "memory": attrs.get("memory"),
                    "hourly_usd": float(dim["pricePerUnit"]["USD"]),
                }
    return None


def extract_fargate_rates(price_data):
    products = price_data["products"]
    terms = price_data["terms"]["OnDemand"]
    vcpu_rate = mem_rate = None
    for sku, product in products.items():
        attrs = product.get("attributes", {})
        if attrs.get("servicecode") != "AmazonECS":
            continue
        usage_type = attrs.get("usagetype", "")
        if "Fargate-vCPU-Hours" not in usage_type and "Fargate-GB-Hours" not in usage_type:
            continue
        term_list = terms.get(sku, {})
        for _, term in term_list.items():
            for _, dim in term["priceDimensions"].items():
                price = float(dim["pricePerUnit"]["USD"])
                if "vCPU" in usage_type:
                    vcpu_rate = price
                elif "GB-Hours" in usage_type:
                    mem_rate = price
    return vcpu_rate, mem_rate


def load_summary():
    rows = []
    with (RESULTS / "summary.csv").open() as f:
        for row in csv.DictReader(f):
            rows.append(row)
    return rows


def max_sustainable_rps_per_variant(rows):
    best = defaultdict(float)
    for row in rows:
        key = (row["endpoint"], row["stub_latency_ms"])
        sustainable = row["sustainable"] in ("True", "true", "1")
        rps = float(row["rps"]) if row["rps"] else 0
        if sustainable and rps > best[key]:
            best[key] = rps
    return best


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--target-rps", default="1000,2000,5000,20000")
    parser.add_argument("--headroom", type=float, default=0.65)
    args = parser.parse_args()
    targets = [int(x) for x in args.target_rps.split(",")]

    print("Fetching live Azure App Service prices...")
    azure_items = fetch_azure_app_service_prices()
    azure_plan = pick_azure_plan(azure_items)

    print("Fetching live AWS Fargate prices...")
    aws_data = fetch_aws_fargate_prices()
    vcpu_rate, mem_rate = extract_fargate_rates(aws_data)
    fargate_hourly = (vcpu_rate or 0) * 1 + (mem_rate or 0) * 2  # 1 vCPU, 2 GB
    fargate_monthly = fargate_hourly * 24 * 30.44

    print("Fetching live AWS Lightsail prices...")
    lightsail_data = fetch_aws_lightsail_prices()
    lightsail = extract_lightsail_rate(lightsail_data)
    lightsail_monthly = (lightsail["hourly_usd"] * 24 * 30.44) if lightsail else None

    prices = {
        "fetched_at_utc": datetime.now(timezone.utc).isoformat(),
        "region": {"azure": REGION, "aws": AWS_REGION},
        "assumptions": "On-demand / pay-as-you-go list prices, not reserved or savings-plan rates.",
        "azure_app_service": azure_plan,
        "azure_monthly_usd": (azure_plan["hourly_usd"] * 24 * 30.44) if azure_plan else None,
        "aws_fargate_1vcpu_2gb": {
            "vcpu_hourly_usd": vcpu_rate,
            "gb_hourly_usd": mem_rate,
            "hourly_usd": fargate_hourly,
            "monthly_usd": fargate_monthly,
        },
        "aws_lightsail_2gb": {
            **(lightsail or {}),
            "monthly_usd": lightsail_monthly,
            "note": "Closest standard Linux bundle by RAM; it's 2 vCPU, not 1, so this "
                    "is generous to Lightsail versus the benchmark's 1 vCPU container.",
        },
    }
    (RESULTS / "prices.json").write_text(json.dumps(prices, indent=2))
    print(f"Saved results/prices.json (Azure {azure_plan['tier'] if azure_plan else 'N/A'}, "
          f"${prices['azure_monthly_usd']:.2f}/mo; Fargate ${fargate_monthly:.2f}/mo; "
          f"Lightsail ${lightsail_monthly:.2f}/mo)")

    rows = load_summary()
    sustainable_rps = max_sustainable_rps_per_variant(rows)
    best_rps = max(sustainable_rps.values()) if sustainable_rps else 1

    lines = ["# Cost table", "", f"Prices fetched {prices['fetched_at_utc']}, "
             f"region: Azure {REGION} / AWS {AWS_REGION}, on-demand rates only.", "",
             f"Azure App Service: {azure_plan['tier'] if azure_plan else 'N/A'} "
             f"({azure_plan['vcpu'] if azure_plan else '?'} vCPU, {azure_plan['ram_gb'] if azure_plan else '?'}GB), "
             "the closest published tier to 1 vCPU / 2GB. "
             f"AWS Fargate: exact 1 vCPU / 2GB task. "
             f"AWS Lightsail: the \"2GB\" standard Linux bundle "
             f"({lightsail['vcpu'] if lightsail else '?'} vCPU, {lightsail['memory'] if lightsail else '?'}) "
             "is the closest published bundle by RAM, but it's 2 vCPU, not 1, so it's a generous "
             "comparison point for Lightsail, not an exact match.", "",
             f"Headroom factor: {args.headroom} (instances = ceil(target_rps / (sustainable_rps_per_instance * headroom))).",
             ""]

    for target in targets:
        lines.append(f"## Target: {target:,} RPS")
        lines.append("")
        lines.append("| Endpoint | Stub latency | Sustainable RPS/instance | Instances needed | "
                      "Azure App Service $/mo | AWS Fargate $/mo | AWS Lightsail $/mo | Ratio vs best |")
        lines.append("|---|---|---|---|---|---|---|---|")
        for (endpoint, latency), rps in sorted(sustainable_rps.items(), key=lambda kv: -kv[1]):
            if rps <= 0:
                continue
            import math
            instances = math.ceil(target / (rps * args.headroom))
            azure_cost = instances * (prices["azure_monthly_usd"] or 0)
            fargate_cost = instances * fargate_monthly
            lightsail_cost = instances * (lightsail_monthly or 0)
            best_instances = math.ceil(target / (best_rps * args.headroom))
            ratio = instances / best_instances if best_instances else None
            lines.append(f"| {endpoint} | {latency}ms | {rps:.1f} | {instances} | "
                          f"${azure_cost:,.0f} | ${fargate_cost:,.0f} | ${lightsail_cost:,.0f} | {ratio:.1f}x |")
        lines.append("")

    (RESULTS / "cost_table.md").write_text("\n".join(lines))
    print("Wrote results/cost_table.md")


if __name__ == "__main__":
    main()
