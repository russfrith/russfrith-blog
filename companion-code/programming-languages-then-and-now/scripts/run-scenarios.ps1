# One-command reproduction of every scenario in this post, run in the pinned
# containers, with results written to a temp directory and diffed against the
# committed output.txt files. Nondeterministic text (timings, addresses, thread
# ids, race totals) is normalized before comparing -- see $normalizers below.
#
# Usage (from companion-code/programming-languages-then-and-now):
#   docker compose up -d mockserver
#   ./scripts/run-scenarios.ps1
#
# Exits non-zero (and lists every scenario) if any normalized diff fails.
#
# NOT covered by this script (documented, not silently skipped -- run these by
# hand per each language's scenarios/README.md):
#   - TypeScript's tsc --noEmit checks (check-all.sh) -- type-checking, not a
#     timed/nondeterministic run, nothing to normalize.
#   - Scenario 2 (data race) in every language -- the whole point is a
#     nondeterministic wrong total; "reproducing" it means confirming it's wrong
#     and the fix is right, not matching one committed number. Spot-check by eye.
#   - Scenario 6 (hung/cancellation) -- a cross-language docker-compose run
#     against the mock server, not a single-container scenario; see
#     shared/scenarios/06_hung_cancellation/README.md.
#   - Scenario 7 (CPU-heavy blocking) in every language -- timed, workload-equalized,
#     and (for C#) deliberately run in two build configurations; spot-check by eye
#     against each output.txt rather than an automated byte-for-byte diff.
#   - Rust/Kotlin scenario 8 (serde/kotlinx.serialization) and Rust scenario 7
#     (tokio) -- multi-binary Cargo projects / multi-jar classpaths; see each
#     README.md for the exact build+run sequence.
#   - C#'s indexer_demo/ and checked_project/ supplementary comparisons.
#   - Rust's compile_reject_*.rs files (expected to FAIL to compile; "reproducing"
#     them means confirming the same compiler error, not diffing stdout).
#   - C's -fsanitize=address leak-detector run for scenario 5 (a second build of
#     the same source with different flags).

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

# Regex replacements applied to BOTH actual and expected text before diffing,
# in order. Each collapses one class of nondeterministic output to a fixed token.
$normalizers = @(
    @{ Pattern = '\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d+Z?'; Replace = '<TIMESTAMP>' }     # ISO8601 goroutine/async timestamps
    @{ Pattern = '\b1[78]\d{11}\b'; Replace = '<EPOCH_MS>' }                                  # Node Date.now() epoch millis
    @{ Pattern = '\b\d+(\.\d+)?ms\b'; Replace = '<DURATION_MS>' }                              # "404.469077ms", "402ms"
    @{ Pattern = '\b\d+(\.\d+)?s\b'; Replace = '<DURATION_S>' }                                 # "2.016s", "13.601s"
    @{ Pattern = '0x[0-9a-fA-F]+'; Replace = '<ADDR>' }                                         # memory addresses
    @{ Pattern = '(?i)\bgoroutine \d+\b'; Replace = 'goroutine <N>' }                           # Go goroutine ids
    @{ Pattern = "(?i)\bthread '[^']*' \(\d+\)"; Replace = "thread '<T>' (<N>)" }               # Rust thread ids
    @{ Pattern = 'heartbeat tick \d+'; Replace = 'heartbeat tick <N>' }                          # tick counts depend on wall-clock scheduling
    @{ Pattern = 'ticks seen: \d+'; Replace = 'ticks seen: <N>' }
    @{ Pattern = 'at \d+\.\d+'; Replace = 'at <T>' }                                            # Python/relative timestamps in heartbeat lines
)

function Normalize([string]$text) {
    foreach ($n in $normalizers) {
        $text = [regex]::Replace($text, $n.Pattern, $n.Replace)
    }
    return $text
}

# $steps is an array of @{ Dir = "..."; Image = "..."; Cmd = "..."; Net = $bool }
# run in order; their stdout/stderr is concatenated before diffing against
# $expectedFile (one output.txt covering every step for that scenario).
function Test-Scenario($name, $steps, $expectedFile, $skipLinePattern) {
    $actual = ""
    foreach ($step in $steps) {
        $hostDir = Join-Path $root $step.Dir
        $networkArgs = @()
        if ($step.Net) { $networkArgs = @("--network", $network) }
        $stepOut = docker run --rm @networkArgs -v "${hostDir}:/src" -w /src $step.Image bash -c $step.Cmd 2>&1 | Out-String
        $actual += $stepOut
    }
    $actualNorm = Normalize $actual
    $expectedRaw = Get-Content -Raw $expectedFile
    $expectedNorm = Normalize $expectedRaw

    $actualPath = Join-Path $tmpDir ($name -replace '[\\/]', '_')
    $actualNorm | Set-Content -Path $actualPath

    # Loose containment check rather than exact-line diff: output.txt also has
    # "$ command" echo lines (never produced by actual docker run output) and a
    # trailing toolchain/version line this script doesn't re-derive -- both are
    # excluded here, everything else must appear in actual output.
    $expectedLines = ($expectedNorm -split "`n") | Where-Object {
        $_.Trim() -ne "" -and
        $_ -notmatch '^(Toolchain|Interpreter|SDK|Runtime|Compiler|Type checker):' -and
        $_ -notmatch '^\$ ' -and
        $_ -notmatch '^EXIT:' -and
        $_ -notmatch '^\(no errors\)$' -and  # tsc check output -- covered separately by check-all.sh, not re-run here
        ($null -eq $skipLinePattern -or $_ -notmatch $skipLinePattern)
    }
    $actualLines = ($actualNorm -split "`n") | ForEach-Object { $_.TrimEnd() }
    $missing = @()
    foreach ($line in $expectedLines) {
        if (-not ($actualLines -contains $line.TrimEnd())) { $missing += $line }
    }
    if ($missing.Count -eq 0) {
        Write-Host "PASS: $name" -ForegroundColor Green
        return $true
    } else {
        Write-Host "FAIL: $name" -ForegroundColor Red
        Write-Host "  missing/changed lines (normalized):" -ForegroundColor Red
        $missing | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
        Write-Host "  actual output saved to: $actualPath"
        return $false
    }
}

function S($dir, $image, $cmd, $net) {
    return @{ Dir = $dir; Image = $image; Cmd = $cmd; Net = [bool]$net }
}

$py = "python:3.14.8-slim"
$go = "golang:1.26.4-bookworm"
$rs = "rust:1.96.1-slim-bookworm"
$node = "node:26-bookworm-slim"
$kt = "kotlinc-2.4.0"
$cs = "mcr.microsoft.com/dotnet/sdk:10.0"
$gcc = "gcc:16.2"

$scenarios = @(
    # --- Python ---
    @{ Name="python/01_absent_null"; Expected="python/scenarios/01_absent_null/output.txt"; Steps=@(
        S "python/scenarios/01_absent_null" $py "python broken.py"
        S "python/scenarios/01_absent_null" $py "python fixed.py"
    )}
    @{ Name="python/03_integer_overflow"; Expected="python/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "python/scenarios/03_integer_overflow" $py "python demo.py"
    )}
    @{ Name="python/04_ignored_errors"; Expected="python/scenarios/04_ignored_errors/output.txt"; Steps=@(
        S "python/scenarios/04_ignored_errors" $py "python broken.py"
        S "python/scenarios/04_ignored_errors" $py "python fixed.py"
    )}
    @{ Name="python/05_resource_cleanup"; Expected="python/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "python/scenarios/05_resource_cleanup" $py "python broken.py"
        S "python/scenarios/05_resource_cleanup" $py "python fixed.py"
    )}
    @{ Name="python/08_untrusted_json"; Expected="python/scenarios/08_untrusted_json/output.txt"; Steps=@(
        S "python/scenarios/08_untrusted_json" $py "python broken.py" $true
        S "python/scenarios/08_untrusted_json" $py "python fixed.py" $true
    )}
    @{ Name="python/footgun_mutable_default"; Expected="python/scenarios/footgun_mutable_default/output.txt"; Steps=@(
        S "python/scenarios/footgun_mutable_default" $py "python broken.py"
        S "python/scenarios/footgun_mutable_default" $py "python fixed.py"
    )}

    # --- Go ---
    @{ Name="go/01_absent_null"; Expected="go/scenarios/01_absent_null/output.txt"; Steps=@(
        S "go/scenarios/01_absent_null" $go "go run broken.go"
        S "go/scenarios/01_absent_null" $go "go run fixed.go"
    )}
    @{ Name="go/03_integer_overflow"; Expected="go/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "go/scenarios/03_integer_overflow" $go "go run broken.go"
        S "go/scenarios/03_integer_overflow" $go "go run fixed.go"
    )}
    @{ Name="go/04_ignored_errors"; Expected="go/scenarios/04_ignored_errors/output.txt"; Steps=@(
        S "go/scenarios/04_ignored_errors" $go "go run broken.go"
        S "go/scenarios/04_ignored_errors" $go "go run fixed.go"
    )}
    @{ Name="go/05_resource_cleanup"; Expected="go/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "go/scenarios/05_resource_cleanup" $go "go run broken.go"
        S "go/scenarios/05_resource_cleanup" $go "go run fixed.go"
    )}
    @{ Name="go/08_untrusted_json"; Expected="go/scenarios/08_untrusted_json/output.txt"; Steps=@(
        S "go/scenarios/08_untrusted_json" $go "go run broken.go" $true
        S "go/scenarios/08_untrusted_json" $go "go run fixed.go" $true
    )}
    @{ Name="go/footgun_nil_interface"; Expected="go/scenarios/footgun_nil_interface/output.txt"; Steps=@(
        S "go/scenarios/footgun_nil_interface" $go "go run broken.go"
        S "go/scenarios/footgun_nil_interface" $go "go run fixed.go"
    )}

    # --- TypeScript (primary runs only; tsc checks are separate, see check-all.sh) ---
    @{ Name="typescript/01_absent_null"; Expected="typescript/scenarios/01_absent_null/output.txt"; Steps=@(
        S "typescript/scenarios/01_absent_null" $node "node broken.ts"
        S "typescript/scenarios/01_absent_null" $node "node fixed.ts"
    )}
    @{ Name="typescript/03_integer_overflow"; Expected="typescript/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "typescript/scenarios/03_integer_overflow" $node "node demo.ts"
    )}
    @{ Name="typescript/05_resource_cleanup"; Expected="typescript/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "typescript/scenarios/05_resource_cleanup" $node "node broken.ts"
        S "typescript/scenarios/05_resource_cleanup" $node "node fixed.ts"
    )}
    @{ Name="typescript/08_untrusted_json"; Expected="typescript/scenarios/08_untrusted_json/output.txt"; Steps=@(
        S "typescript/scenarios/08_untrusted_json" $node "node broken.ts" $true
        S "typescript/scenarios/08_untrusted_json" $node "node fixed.ts" $true
    )}

    # --- C baseline ---
    # NOTE: broken.c's exact "Segmentation fault (core dumped)" message text is a
    # bash job-control artifact that depends on whether bash forks or execs for
    # the dying process -- not reliably reproducible byte-for-byte via scripting.
    # The substance (SIGSEGV, exit 139) reproduces every time; this scenario is
    # intentionally excluded from the strict line-match and spot-checked by eye.
    @{ Name="c/01-absent-null"; Expected="c/scenarios/01-absent-null/output.txt"; SkipReason="segfault message text is a bash job-control artifact, not byte-reproducible"; Steps=@(
        S "c/scenarios/01-absent-null" $gcc "gcc -O0 -g -Wall broken.c -o broken && ./broken; echo done"
        S "c/scenarios/01-absent-null" $gcc "gcc -O0 -g -Wall fixed.c -o fixed && ./fixed"
    )}
    @{ Name="c/03-integer-overflow"; Expected="c/scenarios/03-integer-overflow/output.txt"; Steps=@(
        S "c/scenarios/03-integer-overflow" $gcc "gcc -O0 -g -Wall broken.c -o broken && ./broken"
        S "c/scenarios/03-integer-overflow" $gcc "gcc -O0 -g -Wall fixed.c -o fixed && ./fixed"
    )}
    @{ Name="c/04-ignored-errors"; Expected="c/scenarios/04-ignored-errors/output.txt"; SkipReason="segfault message text is a bash job-control artifact, not byte-reproducible"; Steps=@(
        S "c/scenarios/04-ignored-errors" $gcc "gcc -O0 -g -Wall broken.c -o broken && ./broken; echo done"
        S "c/scenarios/04-ignored-errors" $gcc "gcc -O0 -g -Wall fixed.c -o fixed && ./fixed"
    )}

    # --- Rust (single-file scenarios only; 07/08 are full Cargo projects, see README) ---
    @{ Name="rust/01_absent_null"; Expected="rust/scenarios/01_absent_null/output.txt"; Steps=@(
        S "rust/scenarios/01_absent_null" $rs "rustc -O broken.rs -o broken && ./broken; true"
        S "rust/scenarios/01_absent_null" $rs "rustc -O fixed.rs -o fixed && ./fixed"
    )}
    @{ Name="rust/04_ignored_errors"; Expected="rust/scenarios/04_ignored_errors/output.txt"; Steps=@(
        S "rust/scenarios/04_ignored_errors" $rs "rustc -O broken.rs -o broken && ./broken"
        S "rust/scenarios/04_ignored_errors" $rs "rustc -O fixed.rs -o fixed && ./fixed"
    )}
    @{ Name="rust/05_resource_cleanup"; Expected="rust/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "rust/scenarios/05_resource_cleanup" $rs "rustc -O broken.rs -o broken && ./broken"
        S "rust/scenarios/05_resource_cleanup" $rs "rustc -O fixed.rs -o fixed && ./fixed"
    )}

    # --- Kotlin (single-file scenarios only; 07/08 need the fetched jars, see README) ---
    @{ Name="kotlin/01_absent_null"; Expected="kotlin/scenarios/01_absent_null/output.txt"; Steps=@(
        S "kotlin/scenarios/01_absent_null" $kt "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar; true"
        S "kotlin/scenarios/01_absent_null" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar"
    )}
    @{ Name="kotlin/03_integer_overflow"; Expected="kotlin/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "kotlin/scenarios/03_integer_overflow" $kt "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar"
        S "kotlin/scenarios/03_integer_overflow" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar"
    )}
    @{ Name="kotlin/04_ignored_errors"; Expected="kotlin/scenarios/04_ignored_errors/output.txt"; Steps=@(
        S "kotlin/scenarios/04_ignored_errors" $kt "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar"
        S "kotlin/scenarios/04_ignored_errors" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar"
    )}
    @{ Name="kotlin/05_resource_cleanup"; Expected="kotlin/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "kotlin/scenarios/05_resource_cleanup" $kt "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar"
        S "kotlin/scenarios/05_resource_cleanup" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar"
    )}
    @{ Name="kotlin/footgun_platform_types"; Expected="kotlin/scenarios/footgun_platform_types/output.txt"; Steps=@(
        S "kotlin/scenarios/footgun_platform_types" $kt "kotlinc Broken.kt -include-runtime -d broken.jar; java -jar broken.jar; true"
        S "kotlin/scenarios/footgun_platform_types" $kt "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar"
    )}

    # --- C# (dotnet run per broken/fixed subfolder; Debug config, matches the
    #     primary captured output -- Release-config evidence for scenario 7 is
    #     a separate documented comparison, not re-checked here) ---
    @{ Name="csharp/01_absent_null"; Expected="csharp/scenarios/01_absent_null/output.txt"; Steps=@(
        S "csharp/scenarios/01_absent_null/broken" $cs "rm -rf bin obj && dotnet run; true"
        S "csharp/scenarios/01_absent_null/fixed" $cs "rm -rf bin obj && dotnet run"
        S "csharp/scenarios/01_absent_null/indexer_demo" $cs "rm -rf bin obj && dotnet run"
    )}
    @{ Name="csharp/03_integer_overflow"; Expected="csharp/scenarios/03_integer_overflow/output.txt"; Steps=@(
        S "csharp/scenarios/03_integer_overflow/broken" $cs "rm -rf bin obj && dotnet run"
        S "csharp/scenarios/03_integer_overflow/fixed" $cs "rm -rf bin obj && dotnet run"
        S "csharp/scenarios/03_integer_overflow/checked_project" $cs "rm -rf bin obj && dotnet run"
    )}
    @{ Name="csharp/05_resource_cleanup"; Expected="csharp/scenarios/05_resource_cleanup/output.txt"; Steps=@(
        S "csharp/scenarios/05_resource_cleanup/broken" $cs "rm -rf bin obj && dotnet run"
        S "csharp/scenarios/05_resource_cleanup/fixed" $cs "rm -rf bin obj && dotnet run"
    )}
    @{ Name="csharp/footgun_async_void"; Expected="csharp/scenarios/footgun_async_void/output.txt"; Steps=@(
        S "csharp/scenarios/footgun_async_void/broken" $cs "rm -rf bin obj && dotnet run; true"
        S "csharp/scenarios/footgun_async_void/fixed" $cs "rm -rf bin obj && dotnet run"
    )}
)

$results = @()
foreach ($s in $scenarios) {
    $expectedFile = Join-Path $root $s.Expected
    if (-not (Test-Path $expectedFile)) {
        Write-Host "SKIP (no output.txt): $($s.Name)" -ForegroundColor Yellow
        continue
    }
    $skipPattern = if ($s.SkipReason) { 'Segmentation fault' } else { $null }
    $ok = Test-Scenario -name $s.Name -steps $s.Steps -expectedFile $expectedFile -skipLinePattern $skipPattern
    if ($s.SkipReason) { Write-Host "  (known gap, not required: $($s.SkipReason))" -ForegroundColor DarkYellow }
    $results += @{ Name = $s.Name; Pass = $ok }
}

Pop-Location

$failed = $results | Where-Object { -not $_.Pass }
Write-Host ""
Write-Host "$($results.Count - $failed.Count)/$($results.Count) scenarios reproduced." -ForegroundColor Cyan
if ($failed.Count -gt 0) {
    Write-Host "FAILED:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Red }
    Write-Host "Results written to: $tmpDir"
    exit 1
} else {
    Write-Host "Results written to: $tmpDir"
    exit 0
}
