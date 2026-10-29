# One-command reproduction of every scenario in this post, run in the pinned
# containers. Two kinds of checks:
#
#   1. Output-diff scenarios: run the real commands, synthesize the same
#      "$ command" / output / "EXIT:N" structure the committed output.txt uses,
#      normalize nondeterministic text (timings, addresses, thread ids, tick
#      counts), then compare BOTH directions -- lines missing from the actual
#      run AND lines in the actual run that aren't in the committed file. A
#      small, explicit set of lines (toolchain/version metadata that was never
#      "captured output" in the first place, plus one documented bash
#      job-control artifact) is excluded from BOTH sides before comparing.
#
#   2. Property scenarios: for things where matching one committed transcript
#      byte-for-byte is the wrong test (data races, timing-sensitive heartbeat
#      counts, "this must fail to compile with error E0499"), run the real
#      thing and assert a property instead.
#
# Usage (from companion-code/programming-languages-then-and-now):
#   docker compose up -d mockserver
#   ./scripts/run-scenarios.ps1                 # check mode (default)
#   ./scripts/run-scenarios.ps1 -Update          # regenerate EVERY output-diff
#                                                 # scenario's output.txt from a
#                                                 # real run, so the committed
#                                                 # text is provably exactly
#                                                 # what ran -- never hand-edit
#                                                 # output.txt
#
# Exits non-zero (and lists every failure) in check mode.
#
# STILL NOT COVERED (documented, not silently skipped):
#   - C#'s indexer_demo/ and checked_project/ supplementary comparisons, and
#     the Debug-vs-Release scenario 7 pair -- both are secondary evidence
#     already spot-checked by eye in NOTES.md, not re-verified here.
#   - Rust's exact "Segmentation fault (core dumped)" message text for C
#     scenarios 1 and 4 is excluded from the diff (see $bothSidesExclude) --
#     it's a bash job-control artifact that depends on whether bash forks or
#     execs for the dying process, not reliably reproducible byte-for-byte.

param(
    [switch]$Update
)

$ErrorActionPreference = "Continue"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

$tmpDir = Join-Path $env:TEMP "wordfreq-scenario-repro-$(Get-Date -Format 'yyyyMMddHHmmss')"
New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null

$network = "programming-languages-then-and-now_wordfreq"

# Kotlin scenarios need a kotlinc image; there is no official one, so build it
# from the pinned Dockerfile if it isn't already present (idempotent).
$kotlincImage = docker images -q kotlinc-2.4.0
if (-not $kotlincImage) {
    Write-Host "Building kotlinc-2.4.0 helper image (one-time)..." -ForegroundColor Cyan
    docker build -t kotlinc-2.4.0 -f "$root/kotlin/scenarios/Dockerfile.kotlinc" "$root/kotlin/scenarios" | Out-Null
}
if (-not (Test-Path "$root/kotlin/scenarios/07_cpu_heavy_blocking/coroutines.jar") -or
    -not (Test-Path "$root/kotlin/scenarios/08_untrusted_json/serialization-core.jar")) {
    Write-Host "Fetching Kotlin scenario jars (one-time)..." -ForegroundColor Cyan
    & "$root/kotlin/scenarios/fetch-jars.ps1"
}

# --- normalization: collapses one class of nondeterministic output to a fixed token ---
$normalizers = @(
    @{ Pattern = '\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z?'; Replace = '<TIMESTAMP>' }
    @{ Pattern = '\b1[78]\d{11}\b'; Replace = '<EPOCH_MS>' }
    @{ Pattern = '\b\d+(\.\d+)?ms\b'; Replace = '<DURATION_MS>' }
    @{ Pattern = '\b\d+(\.\d+)?s\b'; Replace = '<DURATION_S>' }
    @{ Pattern = '0x[0-9a-fA-F]+'; Replace = '<ADDR>' }
    @{ Pattern = '(?i)\bgoroutine \d+\b'; Replace = 'goroutine <N>' }
    @{ Pattern = "(?i)\bthread '[^']*' \(\d+\)"; Replace = "thread '<T>' (<N>)" }
    @{ Pattern = 'heartbeat tick \d+'; Replace = 'heartbeat tick <N>' }
    @{ Pattern = 'ticks seen: \d+'; Replace = 'ticks seen: <N>' }
    @{ Pattern = 'at \d+\.\d+'; Replace = 'at <T>' }
    @{ Pattern = '\bPID \d+\b'; Replace = 'PID <N>' }
    @{ Pattern = 'line \d+:\s+\d+ Segmentation fault'; Replace = 'Segmentation fault' }  # bash job-control PID noise
)

function Normalize([string]$text) {
    foreach ($n in $normalizers) { $text = [regex]::Replace($text, $n.Pattern, $n.Replace) }
    return $text
}

# Lines excluded from BOTH sides before the bidirectional compare: documented,
# not silently dropped. These are things that were never "captured output" in
# the first place (toolchain/version metadata lines this script doesn't
# re-derive) or one specific known-nondeterministic bash artifact.
$bothSidesExclude = @(
    '^(Toolchain|Interpreter|SDK|Runtime|Compiler|Type checker):',
    'Segmentation fault'
)

function Filter-Lines($lines) {
    # Two passes: (1) drop a whole "$ npx tsc ..." command block (that command
    # plus every line up to the next "$ " prompt or metadata line) -- tsc
    # checks are covered separately as a property scenario, not re-run as part
    # of every scenario's output-diff. (2) drop the simple per-line exclusions.
    $out = @()
    $inTscBlock = $false
    foreach ($line in $lines) {
        if ($line -match '^\$ npx tsc') { $inTscBlock = $true; continue }
        if ($inTscBlock) {
            if ($line -match '^\$ ' -or $line -match '^(Toolchain|Interpreter|SDK|Runtime|Compiler|Type checker):') {
                $inTscBlock = $false
            } else {
                continue
            }
        }
        $out += $line
    }
    return $out | Where-Object {
        $line = $_
        -not ($bothSidesExclude | Where-Object { $line -match $_ })
    }
}

# NOTE: no automatic EXIT-code echoing here, deliberately -- the committed
# output.txt files are inconsistent about when they show an EXIT: line (some
# scenarios never show one for a successful run, others always do, some only
# show it after the LAST of several split compile/run steps). Rather than
# guess, every $cmd string includes its own "; echo EXIT:$?" exactly where the
# committed transcript has one, so the comparison is honest either way.
function S($dir, $image, $cmd, $net, $display) {
    $autoDisplay = $cmd -replace [regex]::Escape($exit0), ''
    return @{ Dir = $dir; Image = $image; Cmd = $cmd; Net = [bool]$net; Display = $(if ($display) { $display } else { $autoDisplay }) }
}

function Invoke-Step($step) {
    $hostDir = Join-Path $root $step.Dir
    $networkArgs = @()
    if ($step.Net) { $networkArgs = @("--network", $network) }
    $out = docker run --rm @networkArgs -v "${hostDir}:/src" -w /src $step.Image bash -c $step.Cmd 2>&1 | Out-String
    return "`$ $($step.Display)`n$($out.TrimEnd())`n`n"
}

function Run-Steps($steps) {
    $actual = ""
    foreach ($step in $steps) { $actual += Invoke-Step $step }
    return $actual.TrimEnd() + "`n"
}

function Test-Scenario($name, $steps, $expectedFile) {
    $actual = Run-Steps $steps
    $actualNorm = Normalize $actual
    $expectedNorm = Normalize (Get-Content -Raw $expectedFile)

    $actualPath = Join-Path $tmpDir ($name -replace '[\\/]', '_')
    $actualNorm | Set-Content -Path $actualPath

    $expectedLines = Filter-Lines (($expectedNorm -split "`n") | Where-Object { $_.Trim() -ne "" } | ForEach-Object { $_.TrimEnd() })
    $actualLines = Filter-Lines (($actualNorm -split "`n") | Where-Object { $_.Trim() -ne "" } | ForEach-Object { $_.TrimEnd() })

    $missing = Compare-Object $expectedLines $actualLines | Where-Object { $_.SideIndicator -eq "<=" } | ForEach-Object { $_.InputObject }
    $unexpected = Compare-Object $expectedLines $actualLines | Where-Object { $_.SideIndicator -eq "=>" } | ForEach-Object { $_.InputObject }

    if ($missing.Count -eq 0 -and $unexpected.Count -eq 0) {
        Write-Host "PASS: $name" -ForegroundColor Green
        return $true
    } else {
        Write-Host "FAIL: $name" -ForegroundColor Red
        if ($missing.Count -gt 0) {
            Write-Host "  in output.txt but NOT in this run:" -ForegroundColor Red
            $missing | ForEach-Object { Write-Host "    - $_" -ForegroundColor Red }
        }
        if ($unexpected.Count -gt 0) {
            Write-Host "  in this run but NOT in output.txt:" -ForegroundColor Red
            $unexpected | ForEach-Object { Write-Host "    + $_" -ForegroundColor Red }
        }
        Write-Host "  actual output saved to: $actualPath"
        return $false
    }
}

function Test-Property($name, [scriptblock]$check) {
    try {
        $result = & $check
        if ($result -eq $true) {
            Write-Host "PASS: $name" -ForegroundColor Green
            return $true
        } else {
            Write-Host "FAIL: $name -- $result" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "FAIL: $name -- exception: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

$py = "python:3.14.8-slim"
$go = "golang:1.26.4-bookworm"
$rs = "rust:1.96.1-slim-bookworm"
$node = "node:26-bookworm-slim"
$kt = "kotlinc-2.4.0"
$cs = "mcr.microsoft.com/dotnet/sdk:10.0"
$gcc = "gcc:16.2"
$exit0 = '; echo "EXIT:$?"'

# ============================================================================
# Output-diff scenarios
# ============================================================================
$scenarios = @(
    # --- Python ---
    @{ Name="python/01_absent_null"; Expected="python/scenarios/01_absent_null/output.txt"; Steps=@(
        S "python/scenarios/01_absent_null" $py "python broken.py$exit0"
        S "python/scenarios/01_absent_null" $py "python fixed.py$exit0"
    )}
    @{ Name="python/03_integer_overflow"; Expected="python/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "python/scenarios/03_integer_overflow" $py "python demo.py$exit0"
    )}
    @{ Name="python/04_ignored_errors"; Expected="python/scenarios/04_ignored_errors/output.txt"; Steps=@(
        S "python/scenarios/04_ignored_errors" $py "python broken.py$exit0"
        S "python/scenarios/04_ignored_errors" $py "python fixed.py$exit0"
    )}
    @{ Name="python/05_resource_cleanup"; Expected="python/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "python/scenarios/05_resource_cleanup" $py "python broken.py$exit0"
        S "python/scenarios/05_resource_cleanup" $py "python fixed.py$exit0"
    )}
    @{ Name="python/08_untrusted_json"; Expected="python/scenarios/08_untrusted_json/output.txt"; Steps=@(
        S "python/scenarios/08_untrusted_json" $py "python broken.py$exit0" $true
        S "python/scenarios/08_untrusted_json" $py "python fixed.py$exit0" $true
    )}
    @{ Name="python/footgun_mutable_default"; Expected="python/scenarios/footgun_mutable_default/output.txt"; Steps=@(
        S "python/scenarios/footgun_mutable_default" $py "python broken.py$exit0"
        S "python/scenarios/footgun_mutable_default" $py "python fixed.py$exit0"
    )}

    # --- Go ---
    @{ Name="go/01_absent_null"; Expected="go/scenarios/01_absent_null/output.txt"; Steps=@(
        S "go/scenarios/01_absent_null" $go "go run broken.go$exit0"
        S "go/scenarios/01_absent_null" $go "go run fixed.go$exit0"
    )}
    @{ Name="go/03_integer_overflow"; Expected="go/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "go/scenarios/03_integer_overflow" $go "go run broken.go$exit0"
        S "go/scenarios/03_integer_overflow" $go "go run fixed.go$exit0"
    )}
    @{ Name="go/04_ignored_errors"; Expected="go/scenarios/04_ignored_errors/output.txt"; Steps=@(
        S "go/scenarios/04_ignored_errors" $go "go run broken.go$exit0"
        S "go/scenarios/04_ignored_errors" $go "go run fixed.go$exit0"
    )}
    @{ Name="go/05_resource_cleanup"; Expected="go/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "go/scenarios/05_resource_cleanup" $go "go run broken.go$exit0"
        S "go/scenarios/05_resource_cleanup" $go "go run fixed.go$exit0"
    )}
    @{ Name="go/08_untrusted_json"; Expected="go/scenarios/08_untrusted_json/output.txt"; Steps=@(
        S "go/scenarios/08_untrusted_json" $go "go run broken.go$exit0" $true
        S "go/scenarios/08_untrusted_json" $go "go run fixed.go$exit0" $true
    )}
    @{ Name="go/footgun_nil_interface"; Expected="go/scenarios/footgun_nil_interface/output.txt"; Steps=@(
        S "go/scenarios/footgun_nil_interface" $go "go run broken.go$exit0"
        S "go/scenarios/footgun_nil_interface" $go "go run fixed.go$exit0"
    )}

    # --- TypeScript (node runs only here; tsc checks are a property scenario below) ---
    @{ Name="typescript/01_absent_null"; Expected="typescript/scenarios/01_absent_null/output.txt"; Steps=@(
        S "typescript/scenarios/01_absent_null" $node "node broken.ts$exit0"
        S "typescript/scenarios/01_absent_null" $node "node fixed.ts$exit0"
    )}
    @{ Name="typescript/03_integer_overflow"; Expected="typescript/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "typescript/scenarios/03_integer_overflow" $node "node demo.ts$exit0"
    )}
    @{ Name="typescript/05_resource_cleanup"; Expected="typescript/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "typescript/scenarios/05_resource_cleanup" $node "node broken.ts$exit0"
        S "typescript/scenarios/05_resource_cleanup" $node "node fixed.ts$exit0"
    )}
    @{ Name="typescript/08_untrusted_json"; Expected="typescript/scenarios/08_untrusted_json/output.txt"; Steps=@(
        S "typescript/scenarios/08_untrusted_json" $node "node broken.ts" $true
        S "typescript/scenarios/08_untrusted_json" $node "node fixed.ts" $true
    )}

    # --- C baseline: every scenario compiles and runs broken/fixed as four
    #     separate "$ " prompts; only the two "run" steps get an EXIT: line,
    #     matching the committed transcripts exactly ---
    @{ Name="c/01-absent-null"; Expected="c/scenarios/01-absent-null/output.txt"; Steps=@(
        S "c/scenarios/01-absent-null" $gcc "gcc -O0 -g -Wall broken.c -o broken"
        S "c/scenarios/01-absent-null" $gcc "./broken$exit0"
        S "c/scenarios/01-absent-null" $gcc "gcc -O0 -g -Wall fixed.c -o fixed"
        S "c/scenarios/01-absent-null" $gcc "./fixed$exit0"
    )}
    @{ Name="c/03-integer-overflow"; Expected="c/scenarios/03-integer-overflow/output.txt"; Steps=@(
        S "c/scenarios/03-integer-overflow" $gcc "gcc -O0 -g -Wall broken.c -o broken"
        S "c/scenarios/03-integer-overflow" $gcc "./broken$exit0"
        S "c/scenarios/03-integer-overflow" $gcc "gcc -O0 -g -Wall fixed.c -o fixed"
        S "c/scenarios/03-integer-overflow" $gcc "./fixed$exit0"
    )}
    @{ Name="c/04-ignored-errors"; Expected="c/scenarios/04-ignored-errors/output.txt"; Steps=@(
        S "c/scenarios/04-ignored-errors" $gcc "gcc -O0 -g -Wall broken.c -o broken"
        S "c/scenarios/04-ignored-errors" $gcc "./broken$exit0"
        S "c/scenarios/04-ignored-errors" $gcc "gcc -O0 -g -Wall fixed.c -o fixed"
        S "c/scenarios/04-ignored-errors" $gcc "./fixed$exit0"
    )}

    # --- Rust (single-file scenarios; 07/08 and compile-reject are property scenarios below) ---
    @{ Name="rust/01_absent_null"; Expected="rust/scenarios/01_absent_null/output.txt"; Steps=@(
        S "rust/scenarios/01_absent_null" $rs "rustc -O broken.rs -o broken && ./broken$exit0"
        S "rust/scenarios/01_absent_null" $rs "rustc -O fixed.rs -o fixed && ./fixed$exit0"
    )}
    @{ Name="rust/04_ignored_errors"; Expected="rust/scenarios/04_ignored_errors/output.txt"; Steps=@(
        S "rust/scenarios/04_ignored_errors" $rs "rustc -O broken.rs -o broken && ./broken$exit0"
        S "rust/scenarios/04_ignored_errors" $rs "rustc -O fixed.rs -o fixed && ./fixed$exit0"
    )}
    @{ Name="rust/05_resource_cleanup"; Expected="rust/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "rust/scenarios/05_resource_cleanup" $rs "rustc -O broken.rs -o broken && ./broken$exit0"
        S "rust/scenarios/05_resource_cleanup" $rs "rustc -O fixed.rs -o fixed && ./fixed$exit0"
    )}

    # --- Kotlin (single-file scenarios; 07/08 are property scenarios below) ---
    @{ Name="kotlin/01_absent_null"; Expected="kotlin/scenarios/01_absent_null/output.txt"; Steps=@(
        S "kotlin/scenarios/01_absent_null" $kt "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar$exit0"
        S "kotlin/scenarios/01_absent_null" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar$exit0"
    )}
    @{ Name="kotlin/03_integer_overflow"; Expected="kotlin/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "kotlin/scenarios/03_integer_overflow" $kt "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar$exit0"
        S "kotlin/scenarios/03_integer_overflow" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar$exit0"
    )}
    @{ Name="kotlin/04_ignored_errors"; Expected="kotlin/scenarios/04_ignored_errors/output.txt"; Steps=@(
        S "kotlin/scenarios/04_ignored_errors" $kt "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar$exit0"
        S "kotlin/scenarios/04_ignored_errors" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar$exit0"
    )}
    @{ Name="kotlin/05_resource_cleanup"; Expected="kotlin/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "kotlin/scenarios/05_resource_cleanup" $kt "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar$exit0"
        S "kotlin/scenarios/05_resource_cleanup" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar$exit0"
    )}
    @{ Name="kotlin/footgun_platform_types"; Expected="kotlin/scenarios/footgun_platform_types/output.txt"; Steps=@(
        S "kotlin/scenarios/footgun_platform_types" $kt "kotlinc Broken.kt -include-runtime -d broken.jar"
        S "kotlin/scenarios/footgun_platform_types" $kt "java -jar broken.jar$exit0"
        S "kotlin/scenarios/footgun_platform_types" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar$exit0"
    )}

    # --- C# (03/05 never show an EXIT: line at all; 01/footgun_async_void do, after each step) ---
    @{ Name="csharp/01_absent_null"; Expected="csharp/scenarios/01_absent_null/output.txt"; Steps=@(
        S "csharp/scenarios/01_absent_null/broken" $cs "rm -rf bin obj && dotnet run$exit0" "" "dotnet run   (broken/)"
        S "csharp/scenarios/01_absent_null/fixed" $cs "rm -rf bin obj && dotnet run$exit0" "" "dotnet run   (fixed/)"
        S "csharp/scenarios/01_absent_null/indexer_demo" $cs "rm -rf bin obj && dotnet run" "" "dotnet run   (indexer_demo/ -- Dictionary indexer vs TryGetValue)"
    )}
    @{ Name="csharp/03_integer_overflow"; Expected="csharp/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "csharp/scenarios/03_integer_overflow/broken" $cs "rm -rf bin obj && dotnet run" "" "dotnet run   (broken/ -- default unchecked context)"
        S "csharp/scenarios/03_integer_overflow/fixed" $cs "rm -rf bin obj && dotnet run" "" 'dotnet run   (fixed/ -- explicit `checked(...)`)'
        S "csharp/scenarios/03_integer_overflow/checked_project" $cs "rm -rf bin obj && dotnet run" "" 'dotnet run   (checked_project/ -- same plain code, project-level <CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>, no `checked` keyword in source)'
    )}
    @{ Name="csharp/05_resource_cleanup"; Expected="csharp/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "csharp/scenarios/05_resource_cleanup/broken" $cs "rm -rf bin obj && dotnet run" "" "dotnet run   (broken/)"
        S "csharp/scenarios/05_resource_cleanup/fixed" $cs "rm -rf bin obj && dotnet run" "" "dotnet run   (fixed/)"
    )}
    @{ Name="csharp/footgun_async_void"; Expected="csharp/scenarios/footgun_async_void/output.txt"; Steps=@(
        S "csharp/scenarios/footgun_async_void/broken" $cs "rm -rf bin obj && dotnet run$exit0" "" "dotnet run   (broken/)"
        S "csharp/scenarios/footgun_async_void/fixed" $cs "rm -rf bin obj && dotnet run$exit0" "" "dotnet run   (fixed/)"
    )}
)

$results = @()
if (-not $Update) {
    foreach ($s in $scenarios) {
        $expectedFile = Join-Path $root $s.Expected
        if (-not (Test-Path $expectedFile)) {
            Write-Host "SKIP (no output.txt): $($s.Name)" -ForegroundColor Yellow
            continue
        }
        $ok = Test-Scenario -name $s.Name -steps $s.Steps -expectedFile $expectedFile
        $results += @{ Name = $s.Name; Pass = $ok }
    }
}

# ============================================================================
# --update mode: regenerate every output-diff scenario's output.txt from a real
# run, so the committed text is provably exactly what ran (not hand-edited).
# ============================================================================
$updateTargets = $scenarios | ForEach-Object { @{ Name = $_.Name; Scenario = $_ } }

if ($Update) {
    foreach ($t in $updateTargets) {
        $s = $t.Scenario
        if (-not $s) { Write-Host "SKIP (not found in table): $($t.Name)" -ForegroundColor Yellow; continue }
        $expectedFile = Join-Path $root $s.Expected
        $existing = Get-Content -Raw $expectedFile
        $toolchainLines = ($existing -split "`n") | Where-Object { $_ -match '^(Toolchain|Interpreter|SDK|Runtime|Compiler|Type checker):' }
        $actual = Run-Steps $s.Steps
        $newContent = $actual.TrimEnd() + "`n"
        if ($toolchainLines.Count -gt 0) {
            $newContent += "`n" + ($toolchainLines -join "`n") + "`n"
        }
        Set-Content -Path $expectedFile -Value $newContent -NoNewline
        Write-Host "UPDATED: $($s.Expected)" -ForegroundColor Cyan
    }
    Pop-Location
    Write-Host "`n--update complete. Review the diffs (git diff) before committing -- this does not auto-commit." -ForegroundColor Cyan
    exit 0
}

# ============================================================================
# Property scenarios
# ============================================================================
Write-Host "`n--- Property scenarios ---" -ForegroundColor Cyan

# -- Data races: broken total < expected, fixed total == expected --
function Test-Race($name, $dir, $image, $brokenCmd, $fixedCmd, $expected, $net) {
    Test-Property $name {
        $networkArgs = @()
        if ($net) { $networkArgs = @("--network", $network) }
        $hostDir = Join-Path $root $dir

        # Races are inherently timing-sensitive -- a single run occasionally completes
        # without losing an update even with a real race present. Retry the BROKEN run
        # up to 5 times and pass as soon as any one of them loses an update; this is not
        # masking a bug, since the thing under test is "does this data race EVER lose an
        # update," not "does it lose one on every single run." The FIXED run is
        # deterministic (correctly synchronized) and is checked once, not retried.
        $fixedOut = docker run --rm @networkArgs -v "${hostDir}:/src" -w /src $image bash -c $fixedCmd 2>&1 | Out-String
        $fixedMatch = [regex]::Match($fixedOut, 'counter\s*=\s*(\d+)')
        if (-not $fixedMatch.Success) { return "could not parse counter from fixed output" }
        $fixedVal = [int]$fixedMatch.Groups[1].Value
        if ($fixedVal -ne $expected) { return "fixed total was $fixedVal, expected exactly $expected" }

        $lastBrokenVal = $null
        for ($attempt = 1; $attempt -le 5; $attempt++) {
            $brokenOut = docker run --rm @networkArgs -v "${hostDir}:/src" -w /src $image bash -c $brokenCmd 2>&1 | Out-String
            $brokenMatch = [regex]::Match($brokenOut, 'counter\s*=\s*(\d+)')
            if (-not $brokenMatch.Success) { return "could not parse counter from broken output (attempt $attempt)" }
            $lastBrokenVal = [int]$brokenMatch.Groups[1].Value
            if ($lastBrokenVal -lt $expected) { return $true }
        }
        return "broken total was $lastBrokenVal every one of 5 attempts, expected LESS than $expected at least once (race never lost an update)"
    }
}
$goRaceCountResult = Test-Race "property: go/02_data_race (broken<400000, fixed=400000)" "go/scenarios/02_data_race" $go "go run broken.go" "go run fixed.go" 400000 $false
# Go's race detector also prints a WARNING -- check it directly rather than just the count.
$goRaceResults = Test-Property "property: go/02_data_race -race detector reports DATA RACE" {
    $hostDir = Join-Path $root "go/scenarios/02_data_race"
    $out = docker run --rm -v "${hostDir}:/src" -w /src $go bash -c "go run -race broken.go" 2>&1 | Out-String
    if ($out -match "WARNING: DATA RACE") { return $true }
    return "no 'WARNING: DATA RACE' in -race output"
}

# Shared retry wrapper for the remaining languages' scenario-2 races, whose docker
# invocations don't fit Test-Race's single-dir/single-command shape (C# needs two
# subfolders, Kotlin needs a compile step, etc). Same reasoning as Test-Race: retry
# only the timing-sensitive BROKEN run, up to 5 times; the FIXED run is deterministic
# and checked once.
function Test-RaceRetry($name, [scriptblock]$runFixed, [scriptblock]$runBroken, $expected) {
    Test-Property $name {
        $fixedOut = & $runFixed
        $fixedVal = [int]([regex]::Match($fixedOut, 'counter\s*=\s*(\d+)').Groups[1].Value)
        if ($fixedVal -ne $expected) { return "fixed total was $fixedVal, expected exactly $expected" }

        $lastBrokenVal = $null
        for ($attempt = 1; $attempt -le 5; $attempt++) {
            $brokenOut = & $runBroken
            $lastBrokenVal = [int]([regex]::Match($brokenOut, 'counter\s*=\s*(\d+)').Groups[1].Value)
            if ($lastBrokenVal -lt $expected) { return $true }
        }
        return "broken total was $lastBrokenVal every one of 5 attempts, expected less than $expected at least once"
    }
}

# C#'s scenario 2 needs both broken/ and fixed/ subfolders, not a single dir --
# redo it directly instead of forcing it through Test-Race's single-dir shape.
$csharpRaceResult = Test-RaceRetry "property: csharp/02_data_race (broken<400000, fixed=400000)" {
    $fixedDir = Join-Path $root "csharp/scenarios/02_data_race/fixed"
    docker run --rm -v "${fixedDir}:/src" -w /src $cs bash -c "rm -rf bin obj && dotnet run" 2>&1 | Out-String
} {
    $brokenDir = Join-Path $root "csharp/scenarios/02_data_race/broken"
    docker run --rm -v "${brokenDir}:/src" -w /src $cs bash -c "rm -rf bin obj && dotnet run" 2>&1 | Out-String
} 400000

# TypeScript and Kotlin and Python and Rust scenario 2 races, similarly:
$tsRaceResult = Test-RaceRetry "property: typescript/02_data_race (broken<400000, fixed=400000)" {
    $hostDir = Join-Path $root "typescript/scenarios/02_data_race"
    docker run --rm -v "${hostDir}:/src" -w /src $node bash -c "node fixed.ts" 2>&1 | Out-String
} {
    $hostDir = Join-Path $root "typescript/scenarios/02_data_race"
    docker run --rm -v "${hostDir}:/src" -w /src $node bash -c "node broken.ts" 2>&1 | Out-String
} 400000
$ktRaceResult = Test-RaceRetry "property: kotlin/02_data_race (broken<400000, fixed=400000)" {
    $hostDir = Join-Path $root "kotlin/scenarios/02_data_race"
    docker run --rm -v "${hostDir}:/src" -w /src $kt bash -c "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar" 2>&1 | Out-String
} {
    $hostDir = Join-Path $root "kotlin/scenarios/02_data_race"
    docker run --rm -v "${hostDir}:/src" -w /src $kt bash -c "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar" 2>&1 | Out-String
} 400000
$pyRaceResult = Test-RaceRetry "property: python/02_data_race (broken<400, fixed=400)" {
    $hostDir = Join-Path $root "python/scenarios/02_data_race"
    docker run --rm -v "${hostDir}:/src" -w /src $py bash -c "python fixed.py" 2>&1 | Out-String
} {
    $hostDir = Join-Path $root "python/scenarios/02_data_race"
    docker run --rm -v "${hostDir}:/src" -w /src $py bash -c "python broken.py" 2>&1 | Out-String
} 400
$rsRaceResult = Test-RaceRetry "property: rust/02_data_race (broken<400000, fixed=400000)" {
    # broken.rs and fixed.rs use the SAME iteration count (200,000/thread, 400,000
    # total) -- broken.rs's thread::yield_now() between the raw read and raw write
    # is what makes the race reliable, not a reduced iteration count. See NOTES.md.
    $hostDir = Join-Path $root "rust/scenarios/02_data_race"
    docker run --rm -v "${hostDir}:/src" -w /src $rs bash -c "rustc -O fixed.rs -o fixed 2>/dev/null && ./fixed" 2>&1 | Out-String
} {
    $hostDir = Join-Path $root "rust/scenarios/02_data_race"
    docker run --rm -v "${hostDir}:/src" -w /src $rs bash -c "rustc -O broken.rs -o broken 2>/dev/null; ./broken" 2>&1 | Out-String
} 400000

# -- Scenario 6: all six CLIs exit 1 and cancel near the configured timeout --
$scenario6Results = @()
docker compose up -d mockserver 2>&1 | Out-Null
foreach ($lang in @("python", "go", "rust", "typescript", "kotlin", "csharp")) {
    $scenario6Results += Test-Property "property: scenario6/$lang (exit 1, wall time near 2s timeout)" {
        $sw = [Diagnostics.Stopwatch]::StartNew()
        docker compose run --rm $lang --top 5 --concurrency 1 --timeout 2 /data/urls-hang.txt 2>&1 | Out-Null
        $code = $LASTEXITCODE
        $sw.Stop()
        if ($code -ne 1) { return "exit code was $code, expected 1" }
        if ($sw.Elapsed.TotalSeconds -lt 1.5 -or $sw.Elapsed.TotalSeconds -gt 6) { return "wall time was $($sw.Elapsed.TotalSeconds)s, expected roughly 2-4s (2s timeout plus overhead)" }
        return $true
    }
}

# -- Scenario 7: single-threaded broken = 0 ticks, offloaded = >0 ticks, correct result --
$expectedResult = "3464101606477.5415"
function Test-Scenario7($name, $dir, $image, $brokenCmd, $fixedCmd, $net) {
    Test-Property $name {
        $networkArgs = @()
        if ($net) { $networkArgs = @("--network", $network) }
        $hostDir = Join-Path $root $dir
        $brokenOut = docker run --rm @networkArgs -v "${hostDir}:/src" -w /src $image bash -c $brokenCmd 2>&1 | Out-String
        $fixedOut = docker run --rm @networkArgs -v "${hostDir}:/src" -w /src $image bash -c $fixedCmd 2>&1 | Out-String
        $brokenTicks = [int]([regex]::Match($brokenOut, 'ticks seen:\s*(\d+)').Groups[1].Value)
        $fixedTicks = [int]([regex]::Match($fixedOut, 'ticks seen:\s*(\d+)').Groups[1].Value)
        if ($brokenTicks -ne 0) { return "broken (single-threaded) ticks was $brokenTicks, expected exactly 0" }
        if ($fixedTicks -le 0) { return "fixed (offloaded) ticks was $fixedTicks, expected more than 0" }
        if ($brokenOut -notmatch [regex]::Escape($expectedResult) -or $fixedOut -notmatch [regex]::Escape($expectedResult)) {
            return "result value did not match the expected deterministic constant $expectedResult"
        }
        return $true
    }
}
$scenario7Results = @(
    Test-Scenario7 "property: typescript/07_cpu_heavy_blocking (0 vs >0 ticks)" "typescript/scenarios/07_cpu_heavy_blocking" $node "node broken.ts" "node fixed.ts" $false
    Test-Scenario7 "property: python/07_cpu_heavy_blocking (0 vs >0 ticks)" "python/scenarios/07_cpu_heavy_blocking" $py "python broken.py" "python fixed.py" $false
)
$ktScenario7Result = Test-Property "property: kotlin/07_cpu_heavy_blocking (single-thread=0, Dispatchers.Default>0)" {
    $hostDir = Join-Path $root "kotlin/scenarios/07_cpu_heavy_blocking"
    $coroutinesJar = "coroutines.jar"
    $singleOut = docker run --rm -v "${hostDir}:/src" -w /src $kt bash -c "kotlinc -cp $coroutinesJar SingleThreadBroken.kt -include-runtime -d single.jar && java -cp single.jar:$coroutinesJar SingleThreadBrokenKt" 2>&1 | Out-String
    $defaultOut = docker run --rm -v "${hostDir}:/src" -w /src $kt bash -c "kotlinc -cp $coroutinesJar DefaultDispatcherBroken.kt -include-runtime -d default.jar && java -cp default.jar:$coroutinesJar DefaultDispatcherBrokenKt" 2>&1 | Out-String
    $singleTicks = [int]([regex]::Match($singleOut, 'ticks seen:\s*(\d+)').Groups[1].Value)
    $defaultTicks = [int]([regex]::Match($defaultOut, 'ticks seen:\s*(\d+)').Groups[1].Value)
    if ($singleTicks -ne 0) { return "single-threaded ticks was $singleTicks, expected exactly 0" }
    if ($defaultTicks -le 0) { return "Dispatchers.Default ticks was $defaultTicks, expected more than 0" }
    return $true
}
$rsScenario7Result = Test-Property "property: rust/07_cpu_heavy_blocking (current_thread=0, multi_thread>0)" {
    $hostDir = Join-Path $root "rust/scenarios/07_cpu_heavy_blocking"
    docker run --rm -v "${hostDir}:/src" -w /src $rs bash -c "cargo build --release 2>&1" | Out-Null
    $currentOut = docker run --rm -v "${hostDir}:/src" -w /src $rs bash -c "./target/release/current_thread_broken" 2>&1 | Out-String
    $multiOut = docker run --rm -v "${hostDir}:/src" -w /src $rs bash -c "./target/release/multi_thread_broken" 2>&1 | Out-String
    $currentTicks = [int]([regex]::Match($currentOut, 'ticks seen:\s*(\d+)').Groups[1].Value)
    $multiTicks = [int]([regex]::Match($multiOut, 'ticks seen:\s*(\d+)').Groups[1].Value)
    if ($currentTicks -ne 0) { return "current_thread runtime ticks was $currentTicks, expected exactly 0" }
    if ($multiTicks -le 0) { return "multi_thread runtime ticks was $multiTicks, expected more than 0 (NOTE: this machine's spare worker threads can make this pass even without the fix -- see NOTES.md)" }
    return $true
}

# -- Rust compile_reject files: expect specific error codes, nonzero exit --
$compileRejectResult = Test-Property "property: rust/02_data_race compile_reject files (E0373/E0499 and E0277)" {
    $hostDir = Join-Path $root "rust/scenarios/02_data_race"
    $out1 = docker run --rm -v "${hostDir}:/src" -w /src $rs bash -c "rustc compile_reject_plain_mut.rs -o out1" 2>&1 | Out-String
    $out2 = docker run --rm -v "${hostDir}:/src" -w /src $rs bash -c "rustc compile_reject_rc.rs -o out2" 2>&1 | Out-String
    if ($out1 -notmatch "E0499" -and $out1 -notmatch "E0373") { return "compile_reject_plain_mut.rs did not report E0373/E0499" }
    if ($out2 -notmatch "E0277") { return "compile_reject_rc.rs did not report E0277" }
    return $true
}

# -- TypeScript tsc checks: every file must type-check with zero errors --
# check-all.sh runs `npm install`, which writes node_modules/ and rewrites
# package.json's formatting in whatever directory it's run against. Run it
# against a throwaway temp copy, not the real companion-code tree, so this
# check never dirties `git status`.
$tscResult = Test-Property "property: typescript tsc --noEmit (all scenario files, zero errors)" {
    $hostDir = Join-Path $root "typescript/scenarios"
    $tmpTsDir = Join-Path $env:TEMP "wordfreq-ts-check-$([guid]::NewGuid().ToString('N'))"
    Copy-Item -Path $hostDir -Destination $tmpTsDir -Recurse -Exclude "node_modules"
    try {
        $out = docker run --rm -v "${tmpTsDir}:/src" -w /src $node bash check-all.sh 2>&1 | Out-String
    } finally {
        Remove-Item -Recurse -Force $tmpTsDir -ErrorAction SilentlyContinue
    }
    if ($out -match "error TS") { return "tsc reported at least one TS error -- see captured output for which file" }
    $exitCount = ([regex]::Matches($out, "EXIT:0")).Count
    if ($exitCount -lt 15) { return "expected 15 'EXIT:0' markers (one per checked file), found $exitCount" }
    return $true
}

# -- C AddressSanitizer run: leak detected in broken, exit reflects the leak --
$asanResult = Test-Property "property: c/05_resource_cleanup ASan leak detector" {
    $hostDir = Join-Path $root "c/scenarios/05-resource-cleanup"
    $out = docker run --rm -v "${hostDir}:/src" -w /src $gcc bash -c "gcc -O0 -g -fsanitize=address broken.c -o broken_asan && ./broken_asan; echo EXIT:`$?" 2>&1 | Out-String
    if ($out -notmatch "LeakSanitizer: detected memory leaks") { return "ASan did not report a detected leak" }
    if ($out -notmatch "EXIT:1") { return "expected ASan's nonzero exit code (1) on leak detection" }
    return $true
}

Pop-Location

$failed = $results | Where-Object { -not $_.Pass }
$propertyResults = @($goRaceCountResult, $goRaceResults, $csharpRaceResult, $tsRaceResult, $ktRaceResult, $pyRaceResult, $rsRaceResult) +
    $scenario6Results + $scenario7Results + @($ktScenario7Result, $rsScenario7Result, $compileRejectResult, $tscResult, $asanResult)
$propertyFailCount = ($propertyResults | Where-Object { $_ -eq $false }).Count

Write-Host ""
Write-Host "$($results.Count - $failed.Count)/$($results.Count) output-diff scenarios reproduced." -ForegroundColor Cyan
Write-Host "$($propertyResults.Count - $propertyFailCount)/$($propertyResults.Count) property scenarios passed." -ForegroundColor Cyan
if ($failed.Count -gt 0) {
    Write-Host "FAILED (output-diff):" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Red }
}
Write-Host "Results written to: $tmpDir"
if ($failed.Count -gt 0 -or $propertyFailCount -gt 0) { exit 1 } else { exit 0 }
