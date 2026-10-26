#!/bin/bash
# Attaches dotnet-counters to the api process (PID namespace shared via `pid: service:api`
# in docker-compose.yml) and streams ThreadPool EventCounters to JSON for the duration of
# one benchmark run. Usage: collect.sh <output-file-name> <duration-seconds>
set -euo pipefail

OUT_NAME="${1:-counters}"
DURATION="${2:-60}"

API_PID=$(pgrep -f "dotnet.*Api.dll" | head -n1)
if [ -z "$API_PID" ]; then
  echo "could not find api process in shared pid namespace" >&2
  exit 1
fi

dotnet-counters collect \
  -p "$API_PID" \
  --format json \
  -o "/results/${OUT_NAME}.json" \
  --counters System.Runtime,Microsoft.AspNetCore.Hosting \
  --duration "$DURATION"
