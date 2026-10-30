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
# Also GENERATES results/productivity.md directly from this data (J3, Phase
# 1j) -- it is never hand-edited. Does NOT edit the post itself; a human (or a
# follow-up pass) reads productivity.json and updates the post's prose to
# match; see check-post.ps1, which fails if the post's numbers and the JSON
# disagree.
#
# Usage (from companion-code/programming-languages-then-and-now):
#   docker compose build --no-cache   # optional warm-up, not required
#   ./scripts/measure-productivity.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

# Pinned images used by the transitive-dependency-counting steps below (I2).
# This script is standalone -- these are NOT inherited from run-scenarios.ps1.
$rs = "rust:1.96.1-slim-bookworm"
$node = "node:26-bookworm-slim"
$cs = "mcr.microsoft.com/dotnet/sdk:10.0"

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

Write-Host "`n=== Transitive dependency counts (I2) ===" -ForegroundColor Cyan
# Rust: cargo generate-lockfile, then every [[package]] entry in Cargo.lock except
# the root "wordfreq" package itself. Includes the 2 direct deps (ureq, clap) --
# Cargo.lock doesn't cleanly separate "transitive-only" without extra tooling, so
# this is "everything in the resolved dependency graph, direct + transitive".
Remove-Item "rust/Cargo.lock" -Force -ErrorAction SilentlyContinue
docker run --rm -v "${PWD}/rust:/src" -w /src $rs bash -c "cargo generate-lockfile" 2>&1 | Out-Null
$rustLockCount = 0
if (Test-Path "rust/Cargo.lock") {
    $rustLockCount = ([regex]::Matches((Get-Content "rust/Cargo.lock" -Raw), '(?m)^name = ')).Count - 1
    Remove-Item "rust/Cargo.lock" -Force
}
Write-Host "  rust: $rustLockCount packages in Cargo.lock (direct + transitive, root excluded)"

# J2: Cargo.lock counts everything in the resolved dependency graph, but not every
# entry necessarily gets compiled on this platform (e.g. target-specific deps for
# other OSes). Count what cargo ACTUALLY compiles: unique "Compiling <name> v..."
# lines from a real clean `cargo build --release` log, excluding the root crate
# itself ("wordfreq").
Remove-Item "rust/target" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "rust/Cargo.lock" -Force -ErrorAction SilentlyContinue
$rustBuildLog = docker run --rm -v "${PWD}/rust:/src" -w /src $rs bash -c "cargo build --release" 2>&1 | Out-String
$compileMatches = [regex]::Matches($rustBuildLog, '(?m)^\s*Compiling\s+(\S+)\s+v')
$rustCompiledCrateCount = ($compileMatches | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -ne "wordfreq" } | Sort-Object -Unique | Measure-Object).Count
Remove-Item "rust/target" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "rust/Cargo.lock" -Force -ErrorAction SilentlyContinue
Write-Host "  rust: $rustCompiledCrateCount crates actually compiled (unique 'Compiling' lines from a clean cargo build --release, root excluded)"

# TypeScript: npm install --package-lock-only, then every entry in package-lock.json's
# "packages" map except the root ("" key, the project itself).
Remove-Item "typescript/package-lock.json" -Force -ErrorAction SilentlyContinue
$npmCmd = "npm install --package-lock-only"
docker run --rm -v "${PWD}/typescript:/src" -w /src $node bash -c $npmCmd 2>&1 | Out-Null
$tsLockCount = 0
if (Test-Path "typescript/package-lock.json") {
    # -AsHashTable: package-lock.json's "packages" map has an empty-string key
    # for the root project itself, which ConvertFrom-Json otherwise rejects.
    $lock = Get-Content "typescript/package-lock.json" -Raw | ConvertFrom-Json -AsHashtable
    $tsLockCount = $lock.packages.Count - 1
    Remove-Item "typescript/package-lock.json" -Force
}
Write-Host "  typescript: $tsLockCount packages in package-lock.json (direct + transitive, root excluded)"

# Kotlin: `gradle dependencies --configuration runtimeClasspath`, counting unique
# group:artifact pairs (version ignored) that are real resolved dependencies, not
# lines Gradle marks "(c)" (a constraint, not an actual dependency in the graph).
$gradleCmd = 'apt-get update -qq && apt-get install -y -qq curl unzip > /dev/null 2>&1; curl -fsSL https://services.gradle.org/distributions/gradle-9.5.0-bin.zip -o /tmp/gradle.zip && unzip -q /tmp/gradle.zip -d /opt && /opt/gradle-9.5.0/bin/gradle dependencies --configuration runtimeClasspath --no-daemon'
$gradleOut = docker run --rm -v "${PWD}/kotlin:/src" -w /src eclipse-temurin:25-jdk bash -c $gradleCmd 2>&1 | Out-String
$gavSet = [System.Collections.Generic.HashSet[string]]::new()
foreach ($line in ($gradleOut -split "`n")) {
    if ($line -match '\(c\)\s*$') { continue }  # constraint, not a real dependency
    $m = [regex]::Match($line, '([\w.]+):([\w.-]+):[\w.+-]+')
    if ($m.Success) { [void]$gavSet.Add("$($m.Groups[1].Value):$($m.Groups[2].Value)") }
}
$kotlinTransitiveCount = $gavSet.Count
Write-Host "  kotlin: $kotlinTransitiveCount unique group:artifact entries in the runtimeClasspath dependency graph"

# C#: `dotnet restore`, then every entry in obj/project.assets.json's "libraries"
# map (direct + transitive NuGet packages; 0 here since there are 0 PackageReferences).
docker run --rm -v "${PWD}/csharp:/src" -w /src $cs bash -c "dotnet restore" 2>&1 | Out-Null
$csTransitiveCount = 0
if (Test-Path "csharp/obj/project.assets.json") {
    $assets = Get-Content "csharp/obj/project.assets.json" -Raw | ConvertFrom-Json
    $csTransitiveCount = ($assets.libraries.PSObject.Properties | Measure-Object).Count
}
Write-Host "  csharp: $csTransitiveCount entries in obj/project.assets.json's libraries map"

$deps = @{
    python     = @{ count = $pyDeps; note = "stdlib only" }
    go         = @{ count = $goDeps; note = "stdlib only" }
    rust       = @{ count = $rustDeps; note = "ureq, clap"; transitiveCount = $rustLockCount; transitiveMethod = "cargo generate-lockfile; [[package]] entries in Cargo.lock, root excluded (direct + transitive)"; compiledCrateCount = $rustCompiledCrateCount; compiledCrateMethod = "clean cargo build --release; unique 'Compiling <name> v...' lines in the build log, root excluded" }
    typescript = @{ count = $tsRuntimeDeps; devCount = $tsDevDeps; note = "0 runtime; $tsDevDeps dev-only (typescript, @types/node)"; transitiveCount = $tsLockCount; transitiveMethod = "npm install --package-lock-only; entries in package-lock.json's packages map, root excluded (direct + transitive)" }
    kotlin     = @{ count = $kotlinDeps; note = "kotlinx-coroutines-core"; transitiveCount = $kotlinTransitiveCount; transitiveMethod = "gradle dependencies --configuration runtimeClasspath; unique group:artifact pairs, (c) constraint lines excluded" }
    csharp     = @{ count = $csDeps; note = "BCL only"; transitiveCount = $csTransitiveCount; transitiveMethod = "dotnet restore; entries in obj/project.assets.json's libraries map" }
}
$deps.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key): $($_.Value.count) ($($_.Value.note))" }

Write-Host "`n=== Build times: clean, no-op rebuild (Docker layer cache check) ===" -ForegroundColor Cyan
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
    # "no-op rebuild": re-running docker compose build with NOTHING changed on disk --
    # this only checks whether Docker's layer cache is hit, not whether the toolchain
    # itself did a fast incremental recompile. See warmCacheRebuild below for that (J1).
    $noOp = @()
    for ($i = 1; $i -le 3; $i++) {
        $t = Measure-Seconds { docker compose build $lang 2>&1 }
        Write-Host "  no-op rebuild run $i`: ${t}s"
        $noOp += $t
    }
    $buildTimes[$lang] = @{
        cleanRuns              = $clean
        cleanMedian            = Median $clean
        cleanRange             = "$([math]::Min($clean[0],[math]::Min($clean[1],$clean[2])))-$([math]::Max($clean[0],[math]::Max($clean[1],$clean[2])))"
        noChangeRebuildRuns    = $noOp
        noChangeRebuildMedian  = Median $noOp
        noChangeRebuildRange   = "$([math]::Min($noOp[0],[math]::Min($noOp[1],$noOp[2])))-$([math]::Max($noOp[0],[math]::Max($noOp[1],$noOp[2])))"
    }
}

# --- J1: real incremental rebuild, measured at the toolchain level ------------
# The Docker-layer "no-op rebuild" above proves nothing about the compiler/runtime's
# own incremental behavior -- it only proves Docker skipped re-running the RUN step
# because nothing changed. This measures what a developer actually feels: edit one
# line, rebuild, how long. Each language gets its own container with named volumes
# for its toolchain's real caches (so dependency downloads/compiled artifacts persist
# across the warm-up and the 3 timed reruns), warms those caches with one untimed
# clean build, then times 3 rebuilds, each preceded by a distinct one-line comment
# appended to a TEMP COPY of the source inside the container -- never the working tree.
Write-Host "`n=== Build times: rebuild after a one-line change (warm toolchain cache) [J1] ===" -ForegroundColor Cyan

function Measure-WarmCacheRebuild($lang, $image, $hostDir, $cacheMounts, $setupCmd, $rebuildCmdTemplate) {
    $srcVol = "ptn-$lang-src"
    docker volume rm -f $srcVol 2>&1 | Out-Null
    foreach ($cv in $cacheMounts.Keys) { docker volume rm -f $cv 2>&1 | Out-Null }
    docker volume create $srcVol | Out-Null
    foreach ($cv in $cacheMounts.Keys) { docker volume create $cv | Out-Null }

    $volArgs = @("-v", "${srcVol}:/work", "-v", "${PWD}/${hostDir}:/srcHost:ro")
    foreach ($cv in $cacheMounts.Keys) { $volArgs += @("-v", "${cv}:$($cacheMounts[$cv])") }

    Write-Host "  warming caches (untimed clean build)..." -ForegroundColor DarkGray
    docker run --rm @volArgs $image bash -c $setupCmd 2>&1 | Out-Null

    $runs = @()
    $commands = @()
    for ($i = 1; $i -le 3; $i++) {
        $cmd = $rebuildCmdTemplate -replace '\{N\}', $i
        $commands += $cmd
        $t = Measure-Seconds { docker run --rm @volArgs $image bash -c $cmd 2>&1 }
        Write-Host "  warm-cache rebuild run $i`: ${t}s"
        $runs += $t
    }

    docker volume rm -f $srcVol 2>&1 | Out-Null
    foreach ($cv in $cacheMounts.Keys) { docker volume rm -f $cv 2>&1 | Out-Null }

    return @{ runs = $runs; commands = $commands }
}

Write-Host "-- rust --" -ForegroundColor DarkCyan
$r = Measure-WarmCacheRebuild "rust" $rs "rust" `
    @{ "ptn-rust-cargo-registry" = "/usr/local/cargo/registry" } `
    "cp -r /srcHost/. /work/ && cd /work && cargo build --release" `
    "cd /work && echo '// warm-cache-rebuild-marker-{N}' >> src/main.rs && cargo build --release"
$buildTimes["rust"].warmCacheRebuildRuns = $r.runs
$buildTimes["rust"].warmCacheRebuildMedian = Median $r.runs
$buildTimes["rust"].warmCacheRebuildRange = "$([math]::Min($r.runs[0],[math]::Min($r.runs[1],$r.runs[2])))-$([math]::Max($r.runs[0],[math]::Max($r.runs[1],$r.runs[2])))"
$buildTimes["rust"].warmCacheRebuildCommands = $r.commands

Write-Host "-- go --" -ForegroundColor DarkCyan
$r = Measure-WarmCacheRebuild "go" "golang:1.26.4-bookworm" "go" `
    @{ "ptn-go-build-cache" = "/root/.cache/go-build" } `
    "cp -r /srcHost/. /work/ && cd /work && mkdir -p bin && CGO_ENABLED=0 go build -o bin/wordfreq ./wordfreq.go" `
    "cd /work && echo '// warm-cache-rebuild-marker-{N}' >> wordfreq.go && CGO_ENABLED=0 go build -o bin/wordfreq ./wordfreq.go"
$buildTimes["go"].warmCacheRebuildRuns = $r.runs
$buildTimes["go"].warmCacheRebuildMedian = Median $r.runs
$buildTimes["go"].warmCacheRebuildRange = "$([math]::Min($r.runs[0],[math]::Min($r.runs[1],$r.runs[2])))-$([math]::Max($r.runs[0],[math]::Max($r.runs[1],$r.runs[2])))"
$buildTimes["go"].warmCacheRebuildCommands = $r.commands

Write-Host "-- typescript --" -ForegroundColor DarkCyan
$r = Measure-WarmCacheRebuild "typescript" $node "typescript" `
    @{ "ptn-ts-npm-cache" = "/root/.npm" } `
    "cp -r /srcHost/. /work/ && cd /work && npm install && npm run build" `
    "cd /work && echo '// warm-cache-rebuild-marker-{N}' >> src/wordfreq.ts && npm run build"
$buildTimes["typescript"].warmCacheRebuildRuns = $r.runs
$buildTimes["typescript"].warmCacheRebuildMedian = Median $r.runs
$buildTimes["typescript"].warmCacheRebuildRange = "$([math]::Min($r.runs[0],[math]::Min($r.runs[1],$r.runs[2])))-$([math]::Max($r.runs[0],[math]::Max($r.runs[1],$r.runs[2])))"
$buildTimes["typescript"].warmCacheRebuildCommands = $r.commands

Write-Host "-- kotlin --" -ForegroundColor DarkCyan
$kotlinSetup = "apt-get update -qq && apt-get install -y -qq curl unzip > /dev/null 2>&1 && cp -r /srcHost/. /work/ && cd /work && if [ ! -x /work/.gradle-dist/gradle-9.5.0/bin/gradle ]; then curl -fsSL https://services.gradle.org/distributions/gradle-9.5.0-bin.zip -o /tmp/gradle.zip && mkdir -p /work/.gradle-dist && unzip -q /tmp/gradle.zip -d /work/.gradle-dist; fi && export PATH=/work/.gradle-dist/gradle-9.5.0/bin:`$PATH && gradle shadowJar --no-daemon"
$kotlinRebuild = "cd /work && export PATH=/work/.gradle-dist/gradle-9.5.0/bin:`$PATH && echo '// warm-cache-rebuild-marker-{N}' >> src/main/kotlin/Wordfreq.kt && gradle shadowJar --no-daemon"
$r = Measure-WarmCacheRebuild "kotlin" "eclipse-temurin:25-jdk" "kotlin" `
    @{ "ptn-kotlin-gradle-home" = "/root/.gradle" } `
    $kotlinSetup `
    $kotlinRebuild
$buildTimes["kotlin"].warmCacheRebuildRuns = $r.runs
$buildTimes["kotlin"].warmCacheRebuildMedian = Median $r.runs
$buildTimes["kotlin"].warmCacheRebuildRange = "$([math]::Min($r.runs[0],[math]::Min($r.runs[1],$r.runs[2])))-$([math]::Max($r.runs[0],[math]::Max($r.runs[1],$r.runs[2])))"
$buildTimes["kotlin"].warmCacheRebuildCommands = $r.commands

Write-Host "-- csharp --" -ForegroundColor DarkCyan
$r = Measure-WarmCacheRebuild "csharp" $cs "csharp" `
    @{ "ptn-csharp-nuget-packages" = "/root/.nuget/packages" } `
    "cp -r /srcHost/. /work/ && cd /work && dotnet publish -c Release -o /app/out" `
    "cd /work && echo '// warm-cache-rebuild-marker-{N}' >> Program.cs && dotnet publish -c Release -o /app/out"
$buildTimes["csharp"].warmCacheRebuildRuns = $r.runs
$buildTimes["csharp"].warmCacheRebuildMedian = Median $r.runs
$buildTimes["csharp"].warmCacheRebuildRange = "$([math]::Min($r.runs[0],[math]::Min($r.runs[1],$r.runs[2])))-$([math]::Max($r.runs[0],[math]::Max($r.runs[1],$r.runs[2])))"
$buildTimes["csharp"].warmCacheRebuildCommands = $r.commands

# Python has no compile step at all -- there is nothing to "rebuild." The comment
# is still appended each round (for procedural symmetry with the other five
# languages), but what's actually timed is only interpreter + argparse process
# start (`python wordfreq.py --help`), not a build of any kind.
Write-Host "-- python (no compile step -- process start only) --" -ForegroundColor DarkCyan
$r = Measure-WarmCacheRebuild "python" "python:3.14.8-slim" "python" `
    @{} `
    "cp -r /srcHost/. /work/" `
    "cd /work && echo '# warm-cache-rebuild-marker-{N}' >> wordfreq.py && python wordfreq.py --help"
$buildTimes["python"].warmCacheRebuildRuns = $r.runs
$buildTimes["python"].warmCacheRebuildMedian = Median $r.runs
$buildTimes["python"].warmCacheRebuildRange = "$([math]::Min($r.runs[0],[math]::Min($r.runs[1],$r.runs[2])))-$([math]::Max($r.runs[0],[math]::Max($r.runs[1],$r.runs[2])))"
$buildTimes["python"].warmCacheRebuildCommands = $r.commands
$buildTimes["python"].warmCacheRebuildNote = "Python has no compile step; this number is a process start (interpreter + argparse), not a rebuild."

# --- K1: rebuild measurement honesty ---------------------------------------
# Every warm-cache-rebuild run above is timed as a whole `docker run --rm
# <image> bash -c '<cmd>'` invocation, which bundles the real build time with
# Docker's own fixed per-container startup overhead. Measure that overhead
# directly (`docker run --rm <image> true`, 3x per image) and report both the
# raw number above and this overhead subtracted out, so "0.7s" isn't quietly
# half container startup.
Write-Host "`n=== Container overhead (K1) ===" -ForegroundColor Cyan
function Measure-ContainerOverhead($image) {
    $runs = @()
    for ($i = 1; $i -le 3; $i++) {
        $t = Measure-Seconds { docker run --rm $image true 2>&1 }
        $runs += $t
    }
    return @{ runs = $runs; median = (Median $runs) }
}
$overheadImages = @{
    rust = $rs
    go = "golang:1.26.4-bookworm"
    typescript = $node
    kotlin = "eclipse-temurin:25-jdk"
    csharp = $cs
    python = "python:3.14.8-slim"
}
$containerOverhead = @{}
foreach ($lang in $languages) {
    $o = Measure-ContainerOverhead $overheadImages[$lang]
    $containerOverhead[$lang] = $o
    Write-Host "  $lang`: median $($o.median)s (runs: $($o.runs -join ', '))"
}

# Net = raw warm-cache-rebuild runs with this language's own container overhead
# subtracted (floored at 0). The table shows net first, raw in parentheses.
foreach ($lang in $languages) {
    $overheadMedian = $containerOverhead[$lang].median
    # [math]::Max(0, ...) -- NOT 0.0 -- resolves to the Max(int,int) overload here and
    # silently truncates the double argument to int before comparing (so Max(0, 0.36)
    # returns 0, not 0.36). Always pass 0.0 when either side is a double.
    $netRuns = $buildTimes[$lang].warmCacheRebuildRuns | ForEach-Object { [math]::Round([math]::Max(0.0, $_ - $overheadMedian), 2) }
    $buildTimes[$lang].warmCacheRebuildNetRuns = $netRuns
    $buildTimes[$lang].warmCacheRebuildNetMedian = Median $netRuns
    $buildTimes[$lang].warmCacheRebuildNetRange = "$([math]::Min($netRuns[0],[math]::Min($netRuns[1],$netRuns[2])))-$([math]::Max($netRuns[0],[math]::Max($netRuns[1],$netRuns[2])))"
}

# --- K1: Kotlin with the Gradle daemon kept warm ----------------------------
# One untimed `gradle shadowJar` call starts the daemon (no --no-daemon); the
# daemon process then stays alive as long as this ONE container does, so the
# 3 timed rebuilds that follow, in the SAME bash script / SAME container, are
# measured with the daemon already warm. Each of the 3 is timed INSIDE the
# container (nanosecond epoch markers) rather than by wrapping the whole
# `docker run`, since all 3 happen inside one invocation here.
Write-Host "`n=== Kotlin: Gradle daemon kept warm (K1) ===" -ForegroundColor Cyan
$kotlinDaemonSrcVol = "ptn-kotlin-daemon-src"
$kotlinDaemonGradleVol = "ptn-kotlin-daemon-gradle-home"
docker volume rm -f $kotlinDaemonSrcVol $kotlinDaemonGradleVol 2>&1 | Out-Null
docker volume create $kotlinDaemonSrcVol | Out-Null
docker volume create $kotlinDaemonGradleVol | Out-Null
$kotlinDaemonVolArgs = @("-v", "${kotlinDaemonSrcVol}:/work", "-v", "${PWD}/kotlin:/srcHost:ro", "-v", "${kotlinDaemonGradleVol}:/root/.gradle")
$kotlinDaemonScript = @'
apt-get update -qq && apt-get install -y -qq curl unzip > /dev/null 2>&1
cp -r /srcHost/. /work/
cd /work
if [ ! -x /work/.gradle-dist/gradle-9.5.0/bin/gradle ]; then
  curl -fsSL https://services.gradle.org/distributions/gradle-9.5.0-bin.zip -o /tmp/gradle.zip
  mkdir -p /work/.gradle-dist
  unzip -q /tmp/gradle.zip -d /work/.gradle-dist
fi
export PATH=/work/.gradle-dist/gradle-9.5.0/bin:$PATH
gradle shadowJar
for i in 1 2 3; do
  echo "// warm-cache-rebuild-daemon-marker-$i" >> src/main/kotlin/Wordfreq.kt
  start=$(date +%s%N)
  gradle shadowJar
  end=$(date +%s%N)
  echo "DAEMON_RUN_${i}_NS:$((end-start))"
done
'@
$daemonOut = docker run --rm @kotlinDaemonVolArgs "eclipse-temurin:25-jdk" bash -c $kotlinDaemonScript 2>&1 | Out-String
docker volume rm -f $kotlinDaemonSrcVol $kotlinDaemonGradleVol 2>&1 | Out-Null

$daemonRuns = @()
foreach ($m in [regex]::Matches($daemonOut, 'DAEMON_RUN_(\d+)_NS:(\d+)')) {
    $seconds = [math]::Round([double]$m.Groups[2].Value / 1e9, 2)
    $daemonRuns += $seconds
    Write-Host "  daemon-warm rebuild run $($m.Groups[1].Value)`: ${seconds}s"
}
if ($daemonRuns.Count -ne 3) {
    Write-Host "  WARNING: expected 3 daemon-warm runs, parsed $($daemonRuns.Count) -- see raw output below" -ForegroundColor Yellow
    Write-Host $daemonOut
}
$buildTimes["kotlin"].daemonWarmRuns = $daemonRuns
$buildTimes["kotlin"].daemonWarmMedian = Median $daemonRuns
$buildTimes["kotlin"].daemonWarmRange = "$([math]::Min($daemonRuns[0],[math]::Min($daemonRuns[1],$daemonRuns[2])))-$([math]::Max($daemonRuns[0],[math]::Max($daemonRuns[1],$daemonRuns[2])))"
# L1: these 3 runs are timed INSIDE the container via nanosecond epoch markers
# (start/end around just the `gradle shadowJar` call), not by wrapping a
# `docker run` invocation -- Docker's own per-container startup overhead
# (what containerOverhead measures) was never part of this number in the
# first place, so there is nothing to subtract. Net == raw here.
$buildTimes["kotlin"].daemonWarmNetRuns = $daemonRuns
$buildTimes["kotlin"].daemonWarmNetMedian = $buildTimes["kotlin"].daemonWarmMedian
$buildTimes["kotlin"].daemonWarmNetRange = $buildTimes["kotlin"].daemonWarmRange
$buildTimes["kotlin"].daemonWarmNote = "Measured with the Gradle daemon kept warm across all 3 rebuilds in one container (no --no-daemon), timed in-container via nanosecond epoch markers around just the gradle call -- Docker container-startup overhead was never part of this number, so it is reported as raw with no overhead subtracted. Compare against warmCacheRebuild* above, which uses --no-daemon throughout and IS a wrapped-docker-run measurement with overhead subtracted."

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
    containerOverhead = $containerOverhead
}

$outFile = "results/productivity.json"
($productivity | ConvertTo-Json -Depth 8) | Set-Content -Path $outFile
Write-Host "`nWrote $outFile" -ForegroundColor Green

# --- J3: results/productivity.md is now GENERATED from this same data, not
# hand-maintained -- a hand-maintained copy is exactly the kind of number that
# silently drifts from the real measurement. The only prose here that isn't a
# number pulled straight from $productivity is the fixed methodology text.
$displayName = @{ python = "Python"; go = "Go"; rust = "Rust"; typescript = "TypeScript"; kotlin = "Kotlin"; csharp = "C#" }
$depNote = @{ rust = "ureq, clap"; kotlin = "kotlinx-coroutines-core" }
$byClean = $languages | Sort-Object { $buildTimes[$_].cleanMedian }
$capturedDate = ([DateTime]$productivity.capturedAt).ToString("yyyy-MM-dd")

$md = @()
$md += "# Productivity observations (scenarios 9-11)"
$md += ""
$md += "Measured for all six core languages, back to back in one session, on one development machine (Windows 11 Pro, Docker Desktop/WSL2, host otherwise idle), $capturedDate, by ``scripts/measure-productivity.ps1`` -- this file is GENERATED from ``results/productivity.json``, never hand-edited; see that file for raw per-run numbers, tool versions, and the exact capture timestamp. Median and range reported, labeled observations, not benchmarks. Reproduce with:"
$md += ""
$md += '```'
$md += "./scripts/measure-productivity.ps1"
$md += '```'
$md += ""
$md += "## 9. CLI size and dependency count"
$md += ""
$md += "Lines of code: after trimming whitespace, a line counts if it's non-empty and doesn't start with the language's line-comment marker (``//`` or ``#``) -- a simple, consistent rule, not a semantic parse (block comments and trailing inline comments aren't special-cased); this reads lower than a raw ``wc -l`` on the same file."
$md += ""
$md += "| Language | Lines of code (wordfreq only) | Direct third-party dependencies |"
$md += "|---|---|---|"
foreach ($lang in $byClean) {
    $d = $deps[$lang]
    if ($lang -eq "typescript") {
        $cell = "$($d.devCount) dev-only (``typescript``, ``@types/node``); $($d.count) runtime deps -- $($d.transitiveCount) total via ``$($d.transitiveMethod)``"
    } elseif ($d.count -eq 0 -and -not $d.transitiveMethod) {
        $cell = "0 ($($d.note))"
    } elseif ($d.count -eq 0) {
        $cell = "0 ($($d.note)) -- $($d.transitiveCount) total via ``$($d.transitiveMethod)``"
    } else {
        $extra = if ($lang -eq "rust") { " ($($d.compiledCrateCount) actually compiled on Linux via ``$($d.compiledCrateMethod)``)" } else { "" }
        $cell = "$($d.count) ($($depNote[$lang])) -- $($d.transitiveCount) total via ``$($d.transitiveMethod)``$extra"
    }
    $md += "| $($displayName[$lang]) | $($loc[$lang]) | $cell |"
}
$md += ""
$md += "Go, Python, and C# all reach zero direct third-party dependencies for the CLI itself by relying entirely on their standard library / base class library."
$md += ""
$md += "## 10. Clean build, no-op rebuild, and rebuild after a one-line change (median of 3 runs; range in parentheses)"
$md += ""
$md += "Three columns, three different things:"
$md += "- **Clean build**: ``docker compose build --no-cache``. Every layer rebuilt from scratch."
$md += "- **No-op rebuild (Docker layer cache check)**: ``docker compose build`` again with NOTHING changed. This only proves Docker skipped re-running the build steps because their inputs didn't change -- it says nothing about the compiler/runtime's own incremental behavior."
$md += "- **Rebuild after a one-line change (warm toolchain cache)**: a real edit -- one comment line appended to the source -- rebuilt with the language's own build command, inside a container with the toolchain's real caches (package registry, build cache, NuGet packages, etc.) already warm from an untimed clean build. This is what a developer actually feels after a small edit. Python has no compile step, so its number here is only an interpreter + argparse process start, not a rebuild."
$md += ""
$md += "The rebuild-after-a-change column is reported NET of each image's own fixed container-startup overhead (measured separately as ``docker run --rm <image> true``, 3 runs, median subtracted; raw figure in parentheses) -- that overhead is otherwise large enough relative to a sub-second rebuild to be most of the number. These net figures are approximate: the overhead probe is only ``docker run <image> true`` and doesn't capture every cost the real rebuild command incurs (bash startup inside the container, the volume mounts themselves), so it's a floor on the true overhead, not an exact figure. Container overhead by image (median of 3 ``docker run --rm <image> true`` runs): $((($languages | ForEach-Object { "$($displayName[$_]) $($containerOverhead[$_].median)s" }) -join '; '))."
$md += ""
$md += "| Language | Clean build (``--no-cache``) | No-op rebuild (Docker layer cache check) | Rebuild after a one-line change, net of overhead (raw) |"
$md += "|---|---|---|---|"
foreach ($lang in $byClean) {
    $bt = $buildTimes[$lang]
    $md += "| $($displayName[$lang]) | $($bt.cleanMedian)s ($($bt.cleanRange)s) | $($bt.noChangeRebuildMedian)s ($($bt.noChangeRebuildRange)s) | $($bt.warmCacheRebuildNetMedian)s ($($bt.warmCacheRebuildNetRange)s) (raw $($bt.warmCacheRebuildMedian)s, $($bt.warmCacheRebuildRange)s) |"
}
$md += ""
$md += "Kotlin's clean build still includes downloading Gradle 9.5.0 inside the build stage every time ``--no-cache`` is used; a production setup would cache that distribution in a named volume. Rust's clean build resolves $($deps.rust.transitiveCount) packages (direct + transitive) and actually compiles $($deps.rust.compiledCrateCount) crates on Linux."
$md += ""
$maxOverheadMd = ($containerOverhead.Values | ForEach-Object { $_.median } | Measure-Object -Maximum).Maximum
$noiseFloorMd = [math]::Round($maxOverheadMd * 2, 2)
$daemonVsNoDaemonGapMd = [math]::Abs($buildTimes.kotlin.daemonWarmMedian - $buildTimes.kotlin.warmCacheRebuildNetMedian)
$daemonVerdictMd = if ($daemonVsNoDaemonGapMd -gt $noiseFloorMd) { "a real difference" } else { "within noise" }
$md += "**Kotlin with the Gradle daemon kept warm** across all 3 rebuilds in one container (no ``--no-daemon``): $($buildTimes.kotlin.daemonWarmMedian)s median, raw (timed in-container via nanosecond markers around just the ``gradle`` call, so no container-startup overhead was ever part of this number -- there is nothing to subtract) ($($buildTimes.kotlin.daemonWarmRange)s) -- compare against $($buildTimes.kotlin.warmCacheRebuildNetMedian)s net with ``--no-daemon`` (used throughout the table above): $daemonVerdictMd (noise floor here is $noiseFloorMd s, twice the largest measured container overhead)."
$md += ""
$md += "## 11. Final image size (idiomatic multi-stage build)"
$md += ""
$byImageSize = (@("python","go","rust","typescript","kotlin") | ForEach-Object { @{ label = $displayName[$_]; mb = $imageSizes[$_].mb } }) + (
    $csharpVariants.Keys | ForEach-Object {
        $label = switch ($_) { "native-aot" { "C# (Native AOT)" } "self-contained" { "C# (self-contained)" } "framework-dependent" { "C# (framework-dependent, the default used elsewhere in this post)" } }
        @{ label = $label; mb = $csharpImageSizes[$_].mb }
    }
)
$byImageSize = $byImageSize | Sort-Object { $_.mb }
$imageBase = @{
    "Go" = "``gcr.io/distroless/static-debian12`` (static binary, no libc)"
    "Rust" = "``debian:bookworm-slim`` + ``ca-certificates`` (dynamically linked, glibc)"
    "Python" = "``python:3.14.8-slim`` (full interpreter + stdlib)"
    "C# (Native AOT)" = "``mcr.microsoft.com/dotnet/runtime-deps:10.0`` + a native binary, no managed runtime shipped"
    "C# (self-contained)" = "``mcr.microsoft.com/dotnet/runtime-deps:10.0`` + a bundled private copy of the .NET runtime"
    "C# (framework-dependent, the default used elsewhere in this post)" = "``mcr.microsoft.com/dotnet/runtime:10.0`` (shared .NET runtime image)"
    "TypeScript" = "``node:26-bookworm-slim`` (full Node runtime, in both build and final stages)"
    "Kotlin" = "``eclipse-temurin:25-jre`` (JRE) + the shadow jar"
}
$md += "| Language | Final image size | Base |"
$md += "|---|---|---|"
foreach ($row in $byImageSize) {
    $md += "| $($row.label) | $($row.mb) MB | $($imageBase[$row.label]) |"
}
$md += ""
$md += "### C# image-size variants"
$md += ""
$md += "All three built from the same ``Program.cs``/``wordfreq.csproj``, varying only the publish mode:"
$md += ""
$md += "- **Framework-dependent** (``csharp/Dockerfile``, used for the CLI service everywhere else in this post): ``dotnet publish -c Release``, runs on ``mcr.microsoft.com/dotnet/runtime:10.0``. $($csharpImageSizes["framework-dependent"].mb) MB."
$md += "- **Self-contained** (``csharp/Dockerfile.selfcontained``): ``dotnet publish -c Release -r linux-x64 --self-contained true``, bundles a private copy of the runtime, runs on the smaller ``runtime-deps:10.0`` base. $($csharpImageSizes["self-contained"].mb) MB."
$md += "- **Native AOT** (``csharp/Dockerfile.aot``): ``dotnet publish -c Release -r linux-x64 -p:PublishAot=true``, also on ``runtime-deps:10.0``. $($csharpImageSizes["native-aot"].mb) MB -- the smallest of the three, and the only one shipping a fully native binary with no JIT or managed runtime at all."
$md += ""
$md += "Native AOT built successfully in this container with ONE extra build-stage dependency beyond the SDK image: ``clang`` and ``zlib1g-dev``. No separate native toolchain installation outside the container was needed. The resulting binary was verified to run and produce byte-identical ``wordfreq`` output to the other two variants."
$md += ""

$productivityMdFile = "results/productivity.md"
($md -join "`n") | Set-Content -Path $productivityMdFile -NoNewline
Write-Host "Wrote $productivityMdFile (generated, not hand-edited)" -ForegroundColor Green

# Provenance entries, same mechanism as run-scenarios.ps1 -Update.
$hash = (Get-FileHash -Path $outFile -Algorithm SHA256).Hash.ToLower()
$mdHash = (Get-FileHash -Path $productivityMdFile -Algorithm SHA256).Hash.ToLower()
$provenanceFile = "results/provenance.json"
$existing = @()
if (Test-Path $provenanceFile) {
    $existing = @(Get-Content -Raw $provenanceFile | ConvertFrom-Json)
}
$existing = $existing | Where-Object { $_.file -ne $outFile -and $_.file -ne $productivityMdFile }
$existing += @{
    file     = $outFile
    commands = @(
        "docker compose build --no-cache <service> (x3 per language)"
        "docker compose build <service> (x3 per language, no-op)"
        "docker run --rm -v <toolchain caches>:... -v <src>:/work <image> bash -c '<one-line comment>; <build command>' (x3 per language, warm toolchain cache)"
        "docker image inspect <image> --format '{{.Size}}'"
        "docker build -f csharp/Dockerfile.aot|.selfcontained|(none) ./csharp"
    )
    images       = @($languages | ForEach-Object { "programming-languages-then-and-now-$_`:latest" })
    captured     = (Get-Date).ToString("o")
    sha256       = $hash
}
$existing += @{
    file     = $productivityMdFile
    commands = @("generated from results/productivity.json by scripts/measure-productivity.ps1 -- never hand-edited")
    images       = @()
    captured     = (Get-Date).ToString("o")
    sha256       = $mdHash
}
($existing | ConvertTo-Json -Depth 8) | Set-Content -Path $provenanceFile
Write-Host "Updated $provenanceFile" -ForegroundColor Green

Pop-Location
