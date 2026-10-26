# Windows/Docker Desktop equivalent of run_matrix.py. Nested docker-outside-of-docker
# bind-mount path translation (relative paths resolve against the *inner* container's
# filesystem view, not the host's) is unreliable on Windows, so this runs the same
# docker compose commands directly against the host daemon instead. Same matrix
# definition, same docker-compose.yml, no code difference in what gets tested.
#
# Scope note: ENDPOINTS/LATENCIES/STEPS/REPETITIONS below are a reduced matrix (fewer
# concurrency steps, 1 repetition) run in a single sitting on a shared dev machine --
# see README.md "Scope note on this run".

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$endpoints = [ordered]@{
    "async-correct"            = @{ latencies = @(50,200); abortFraction = 0 }
    "sync-over-async"          = @{ latencies = @(50,200); abortFraction = 0 }
    "blocking-sync"            = @{ latencies = @(50,200); abortFraction = 0 }
    "fanout-unbounded"         = @{ latencies = @(50,200); abortFraction = 0 }
    "fanout-bounded"           = @{ latencies = @(50,200); abortFraction = 0 }
    "async-no-cancellation"    = @{ latencies = @(50,200); abortFraction = 0.2 }
    "async-with-cancellation"  = @{ latencies = @(50,200); abortFraction = 0.2 }
    "cpu-bound"                = @{ latencies = @(0);      abortFraction = 0 }
}

$steps = @(50, 200, 500)
$repetitions = 1
$warmup = "5s"
$hold = "20s"
$overheadBudgetMs = 250

function Wait-Healthy {
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri "http://localhost:5080/healthz" -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -eq 200) { return }
        } catch {}
        Start-Sleep -Seconds 1
    }
    throw "api did not become healthy in time"
}

function Get-ApiStats {
    $raw = docker stats --no-stream --format "{{json .}}" "async-await-web-api-cloud-costs-api-1" 2>$null
    if (-not $raw) { return $null, $null }
    $data = $raw | ConvertFrom-Json
    $cpu = [double]($data.CPUPerc.TrimEnd('%'))
    $mem = ($data.MemUsage -split '/')[0].Trim()
    return $cpu, $mem
}

docker compose down --remove-orphans 2>$null | Out-Null
docker compose build stub api | Out-Null

$rows = @()
$runIndex = 0

foreach ($endpoint in $endpoints.Keys) {
    $cfg = $endpoints[$endpoint]
    foreach ($latencyMs in $cfg.latencies) {
        $thresholdMs = if ($latencyMs -gt 0) { $latencyMs + $overheadBudgetMs } else { 300 }
        foreach ($vus in $steps) {
            for ($rep = 1; $rep -le $repetitions; $rep++) {
                $runIndex++
                $name = "${endpoint}_${latencyMs}ms_${vus}vus_rep${rep}"
                Write-Host "`n==== [$runIndex] $name ====" -ForegroundColor Cyan

                docker compose up -d --force-recreate stub api | Out-Null
                Wait-Healthy

                $stages = (@(@{duration=$warmup; target=$vus}, @{duration=$hold; target=$vus}) | ConvertTo-Json -Compress)

                $summaryContainerPath = "/results/${name}_k6.json"
                $k6Args = @(
                    "--profile", "load", "run", "--rm",
                    "-e", "BASE_URL=http://api:8080",
                    "-e", "ENDPOINT=$endpoint",
                    "-e", "LATENCY_MS=$latencyMs",
                    "-e", "FANOUT_N=10",
                    "-e", "ABORT_FRACTION=$($cfg.abortFraction)",
                    "-e", "STAGES=$stages",
                    "-e", "P95_THRESHOLD_MS=$thresholdMs",
                    "k6", "run", "--summary-export=$summaryContainerPath", "/scripts/bench.js"
                )
                docker compose @k6Args | Out-Null
                $k6Ok = ($LASTEXITCODE -eq 0)

                $cpu, $mem = Get-ApiStats

                $localSummary = Join-Path $root "results\raw\${name}_k6.json"
                $rps = $null; $p95 = $null; $errorRate = $null
                if (Test-Path $localSummary) {
                    $data = Get-Content $localSummary -Raw | ConvertFrom-Json
                    $rps = $data.metrics.http_reqs.rate
                    $p95 = $data.metrics.http_req_duration.'p(95)'
                    $errorRate = $data.metrics.http_req_failed.value
                }

                $sustainable = ($null -ne $p95 -and $null -ne $errorRate -and $p95 -lt $thresholdMs -and $errorRate -lt 0.01)

                $rows += [PSCustomObject]@{
                    endpoint          = $endpoint
                    stub_latency_ms   = $latencyMs
                    threshold_p95_ms  = $thresholdMs
                    vus               = $vus
                    repetition        = $rep
                    rps               = if ($rps) { [math]::Round($rps, 2) } else { $null }
                    p95_ms            = if ($p95) { [math]::Round($p95, 2) } else { $null }
                    error_rate        = if ($null -ne $errorRate) { [math]::Round($errorRate, 4) } else { $null }
                    api_cpu_percent   = $cpu
                    api_mem           = $mem
                    sustainable       = $sustainable
                    k6_thresholds_met = $k6Ok
                }

                docker compose down | Out-Null
            }
        }
    }
}

$rows | Export-Csv -Path (Join-Path $root "results\summary.csv") -NoTypeInformation
Write-Host "`nWrote $($rows.Count) rows to results\summary.csv" -ForegroundColor Green
