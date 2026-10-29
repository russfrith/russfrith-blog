# Re-measures the three productivity numbers (scenarios 9-11) for all six core
# languages, back to back in one session, so the post's tables, results/productivity.md,
# and results/productivity.json are all the same measurement, not three hand-maintained
# copies that can drift apart.
#
# Writes results/productivity.json: raw per-run timings, medians, ranges, line
# counts, dependency counts, image sizes, capture date, and tool versions. Also
# appends a provenance entry to results/provenance.json (same mechanism as
# run-scenarios.ps1 -Update).
#
# Does NOT edit the post or results/productivity.md -- a human (or a follow-up
# pass) reads productivity.json and updates the prose to match; see
# check-post.ps1, which fails if the post's numbers and the JSON disagree.
#
# Usage (from companion-code/programming-languages-then-and-now):
#   docker compose build --no-cache   # optional warm-up, not required
#   ./scripts/measure-productivity.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

function Median($nums) {
    $sorted = $nums | Sort-Object
    $n = $sorted.Count
    if ($n % 2 -eq 1) { return $sorted[[math]::Floor($n / 2)] }
    return (($sorted[$n / 2 - 1] + $sorted[$n / 2]) / 2)
}

function Measure-Seconds([scriptblock]$action) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    & $action | Out-Null
    $sw.Stop()
    return [math]::Round($sw.Elapsed.TotalSeconds, 2)
}

# --- counting rules, stated plainly so they're reproducible ---
# "Line of code" = after trimming whitespace, the line is non-empty AND does not
# start with the language's line-comment marker ("//" or "#"). This does not
# special-case block comments or trailing inline comments; it's a simple,
# consistent rule applied identically to every language, not a semantic parse.
function Count-Loc($path, $commentPrefix) {
    $lines = Get-Content $path
    $count = 0
    foreach ($line in $lines) {
        $t = $line.Trim()
        if ($t -eq "") { continue }
        if ($t.StartsWith($commentPrefix)) { continue }
        $count++
    }
    return $count
}

Write-Host "=== Lines of code ===" -ForegroundColor Cyan
$loc = @{
    python     = Count-Loc "python/wordfreq.py" "#"
    go         = Count-Loc "go/wordfreq.go" "//"
    rust       = Count-Loc "rust/src/main.rs" "//"
    typescript = Count-Loc "typescript/src/wordfreq.ts" "//"
    kotlin     = Count-Loc "kotlin/src/main/kotlin/Wordfreq.kt" "//"
    csharp     = Count-Loc "csharp/Program.cs" "//"
}
$loc.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key): $($_.Value)" }

Write-Host "`n=== Direct dependency counts ===" -ForegroundColor Cyan
# Go: no require block in go.mod at all (stdlib only).
$goDeps = 0
# Python: no requirements.txt/pyproject.toml (stdlib only).
$pyDeps = 0
# Rust: lines inside Cargo.toml's [dependencies] table.
$cargoToml = Get-Content "rust/Cargo.toml"
$depSectionStart = ($cargoToml | Select-String -Pattern '^\[dependencies\]').LineNumber
$rustDeps = 0
if ($depSectionStart) {
    for ($i = $depSectionStart; $i -lt $cargoToml.Count; $i++) {
        $line = $cargoToml[$i].Trim()
        if ($line -match '^\[') { break }
        if ($line -ne "" -and $line -match '=') { $rustDeps++ }
    }
}
# TypeScript: "dependencies" (runtime) vs "devDependencies" -- 0 runtime, 2 dev-only.
$pkgJson = Get-Content "typescript/package.json" -Raw | ConvertFrom-Json
$tsRuntimeDeps = if ($pkgJson.dependencies) { ($pkgJson.dependencies.PSObject.Properties | Measure-Object).Count } else { 0 }
$tsDevDeps = if ($pkgJson.devDependencies) { ($pkgJson.devDependencies.PSObject.Properties | Measure-Object).Count } else { 0 }
# Kotlin: implementation(...)/api(...) lines inside build.gradle.kts's dependencies block.
$gradleKts = Get-Content "kotlin/build.gradle.kts"
$ktDepStart = ($gradleKts | Select-String -Pattern '^dependencies \{').LineNumber
$kotlinDeps = 0
if ($ktDepStart) {
    for ($i = $ktDepStart; $i -lt $gradleKts.Count; $i++) {
        $line = $gradleKts[$i].Trim()
        if ($line -eq "}") { break }
        if ($line -match '^(implementation|api)\(') { $kotlinDeps++ }
    }
}
# C#: <PackageReference> entries in the .csproj.
$csDeps = ([regex]::Matches((Get-Content "csharp/wordfreq.csproj" -Raw), '<PackageReference')).Count

$deps = @{
    python     = @{ count = $pyDeps; note = "stdlib only" }
    go         = @{ count = $goDeps; note = "stdlib only" }
    rust       = @{ count = $rustDeps; note = "ureq, clap" }
    typescript = @{ count = $tsRuntimeDeps; devCount = $tsDevDeps; note = "0 runtime; $tsDevDeps dev-only (typescript, @types/node)" }
    kotlin     = @{ count = $kotlinDeps; note = "kotlinx-coroutines-core" }
    csharp     = @{ count = $csDeps; note = "BCL only" }
}
$deps.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key): $($_.Value.count) ($($_.Value.note))" }

Write-Host "`n=== Build times (3 runs each: clean, then incremental) ===" -ForegroundColor Cyan
$languages = @("python", "go", "rust", "typescript", "kotlin", "csharp")
$buildTimes = @{}
foreach ($lang in $languages) {
    Write-Host "-- $lang --" -ForegroundColor DarkCyan
    $clean = @()
    for ($i = 1; $i -le 3; $i++) {
        $t = Measure-Seconds { docker compose build --no-cache $lang 2>&1 }
        Write-Host "  clean run $i`: ${t}s"
        $clean += $t
    }
    $incremental = @()
    for ($i = 1; $i -le 3; $i++) {
        $t = Measure-Seconds { docker compose build $lang 2>&1 }
        Write-Host "  incremental run $i`: ${t}s"
        $incremental += $t
    }
    $buildTimes[$lang] = @{
        cleanRuns        = $clean
        cleanMedian      = Median $clean
        cleanRange       = "$([math]::Min($clean[0],[math]::Min($clean[1],$clean[2])))-$([math]::Max($clean[0],[math]::Max($clean[1],$clean[2])))"
        incrementalRuns  = $incremental
        incrementalMedian = Median $incremental
        incrementalRange = "$([math]::Min($incremental[0],[math]::Min($incremental[1],$incremental[2])))-$([math]::Max($incremental[0],[math]::Max($incremental[1],$incremental[2])))"
    }
}

Write-Host "`n=== Image sizes ===" -ForegroundColor Cyan
$imageSizes = @{}
foreach ($lang in $languages) {
    $bytes = [int64](docker image inspect "programming-languages-then-and-now-${lang}:latest" --format '{{.Size}}')
    $mb = [math]::Round($bytes / 1MB, 1)
    $imageSizes[$lang] = @{ bytes = $bytes; mb = $mb }
    Write-Host "  $lang`: $mb MB"
}

# C# has three publish-mode variants, built directly (not through compose).
Write-Host "-- csharp variants (Native AOT, self-contained, framework-dependent) --" -ForegroundColor DarkCyan
$csharpVariants = @{
    "framework-dependent" = "csharp/Dockerfile"
    "self-contained"      = "csharp/Dockerfile.selfcontained"
    "native-aot"           = "csharp/Dockerfile.aot"
}
$csharpImageSizes = @{}
foreach ($variant in $csharpVariants.Keys) {
    $tag = "wordfreq-csharp-$variant"
    docker build -f $csharpVariants[$variant] -t $tag ./csharp 2>&1 | Out-Null
    $bytes = [int64](docker image inspect "${tag}:latest" --format '{{.Size}}')
    $mb = [math]::Round($bytes / 1MB, 1)
    $csharpImageSizes[$variant] = @{ bytes = $bytes; mb = $mb }
    Write-Host "  $variant`: $mb MB"
}
$imageSizes["csharp"] = @{ variants = $csharpImageSizes }

Write-Host "`n=== Tool versions ===" -ForegroundColor Cyan
$dockerVersion = (docker --version).Trim()
$composeVersion = (docker compose version).Trim()
Write-Host "  $dockerVersion"
Write-Host "  $composeVersion"

$productivity = @{
    capturedAt   = (Get-Date).ToString("o")
    toolVersions = @{ docker = $dockerVersion; compose = $composeVersion }
    linesOfCode  = $loc
    dependencies = $deps
    buildTimes   = $buildTimes
    imageSizes   = $imageSizes
}

$outFile = "results/productivity.json"
($productivity | ConvertTo-Json -Depth 8) | Set-Content -Path $outFile
Write-Host "`nWrote $outFile" -ForegroundColor Green

# Provenance entry, same mechanism as run-scenarios.ps1 -Update.
$hash = (Get-FileHash -Path $outFile -Algorithm SHA256).Hash.ToLower()
$provenanceFile = "results/provenance.json"
$existing = @()
if (Test-Path $provenanceFile) {
    $existing = @(Get-Content -Raw $provenanceFile | ConvertFrom-Json)
}
$existing = $existing | Where-Object { $_.file -ne $outFile }
$existing += @{
    file     = $outFile
    commands = @(
        "docker compose build --no-cache <service> (x3 per language)"
        "docker compose build <service> (x3 per language, incremental)"
        "docker image inspect <image> --format '{{.Size}}'"
        "docker build -f csharp/Dockerfile.aot|.selfcontained|(none) ./csharp"
    )
    images       = @($languages | ForEach-Object { "programming-languages-then-and-now-$_`:latest" })
    captured     = (Get-Date).ToString("o")
    sha256       = $hash
}
($existing | ConvertTo-Json -Depth 8) | Set-Content -Path $provenanceFile
Write-Host "Updated $provenanceFile" -ForegroundColor Green

Pop-Location
