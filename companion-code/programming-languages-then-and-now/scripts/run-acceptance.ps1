# Shared acceptance check: runs every core-language wordfreq implementation against
# the same mock-server fixtures and diffs stdout against one expected-output file per
# fixture. Run from the companion-code/programming-languages-then-and-now directory:
#
#   docker compose up -d mockserver
#   ./scripts/run-acceptance.ps1
#
# Exits non-zero if any language's stdout doesn't match, or an exit code is wrong.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

$languages = @("python", "go", "rust", "typescript", "kotlin", "csharp")
$failures = @()
$resultLines = @()
$resultLines += "# Acceptance results"
$resultLines += ""
$resultLines += "Run on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$resultLines += ""

foreach ($lang in $languages) {
    $resultLines += "## $lang"
    $resultLines += ""

    # happy path -> exit 0, matches happy-top5.txt
    $out = docker compose run --rm $lang --top 5 --concurrency 4 --timeout 5 /data/urls-happy.txt 2>$null
    $code = $LASTEXITCODE
    $expected = Get-Content "$root/shared/expected-output/happy-top5.txt"
    $outTrim = $out | ForEach-Object { $_.TrimEnd() }
    $match = ($outTrim -join "`n") -eq ($expected -join "`n")
    $resultLines += "- happy path: exit=$code (want 0), stdout matches expected: $match"
    if ($code -ne 0 -or -not $match) { $failures += "$lang happy path" }

    # partial failure -> exit 1, matches partial-top5.txt
    $out = docker compose run --rm $lang --top 5 --concurrency 4 --timeout 5 /data/urls-partial.txt 2>$null
    $code = $LASTEXITCODE
    $expected = Get-Content "$root/shared/expected-output/partial-top5.txt"
    $outTrim = $out | ForEach-Object { $_.TrimEnd() }
    $match = ($outTrim -join "`n") -eq ($expected -join "`n")
    $resultLines += "- partial failure: exit=$code (want 1), stdout matches expected: $match"
    if ($code -ne 1 -or -not $match) { $failures += "$lang partial failure" }

    # bad args -> exit 2 (missing input file)
    docker compose run --rm $lang --top 5 --concurrency 4 --timeout 5 /data/does-not-exist.txt 2>$null | Out-Null
    $code = $LASTEXITCODE
    $resultLines += "- missing input file: exit=$code (want 2)"
    if ($code -ne 2) { $failures += "$lang missing input file" }

    $resultLines += ""
}

# Independent recomputation: a coreutils-only pipeline that shares no
# code with any implementation above, reading the same fixtures.
$resultLines += "## independent verification (verify-expected, coreutils only)"
$resultLines += ""
$independentHappy = docker run --rm --network programming-languages-then-and-now_wordfreq -v "$root/shared/acceptance/verify-expected:/verify-expected" debian:bookworm-slim sh -c "apt-get update -qq && apt-get install -y -qq curl gawk >/dev/null 2>&1; sh /verify-expected 5 http://mockserver:8080/fixed/alpha.html http://mockserver:8080/fixed/beta.html"
$expectedHappy = Get-Content "$root/shared/expected-output/happy-top5.txt"
$independentMatch = (($independentHappy | ForEach-Object { $_.TrimEnd() }) -join "`n") -eq ($expectedHappy -join "`n")
$resultLines += "- happy path matches shared/expected-output/happy-top5.txt: $independentMatch"
if (-not $independentMatch) { $failures += "independent verification (happy path)" }
$resultLines += ""

New-Item -ItemType Directory -Force -Path "$root/shared/acceptance" | Out-Null
$resultLines | Set-Content "$root/shared/acceptance/RESULTS.md"

Pop-Location

if ($failures.Count -gt 0) {
    Write-Host "ACCEPTANCE FAILURES:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
} else {
    Write-Host "All acceptance checks passed." -ForegroundColor Green
    exit 0
}
