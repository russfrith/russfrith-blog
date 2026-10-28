# Fetches the exact, pinned jars every Kotlin scenario needs, instead of
# downloading them "by hand" inside ad-hoc docker run invocations. Idempotent --
# skips a jar that's already present. Run once before any Kotlin scenario:
#
#   ./kotlin/scenarios/fetch-jars.ps1
#
# Versions pinned here must match companion-code/.../VERSIONS.md and the
# Toolchain: lines in kotlin/scenarios/*/output.txt.

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$jars = @(
    @{
        Path = "07_cpu_heavy_blocking/coroutines.jar"
        Url  = "https://repo1.maven.org/maven2/org/jetbrains/kotlinx/kotlinx-coroutines-core-jvm/1.10.1/kotlinx-coroutines-core-jvm-1.10.1.jar"
    },
    @{
        Path = "08_untrusted_json/serialization-plugin.jar"
        Url  = "https://repo1.maven.org/maven2/org/jetbrains/kotlin/kotlin-serialization-compiler-plugin-embeddable/2.4.0/kotlin-serialization-compiler-plugin-embeddable-2.4.0.jar"
    },
    @{
        Path = "08_untrusted_json/serialization-core.jar"
        Url  = "https://repo1.maven.org/maven2/org/jetbrains/kotlinx/kotlinx-serialization-core-jvm/1.7.3/kotlinx-serialization-core-jvm-1.7.3.jar"
    },
    @{
        Path = "08_untrusted_json/serialization-json.jar"
        Url  = "https://repo1.maven.org/maven2/org/jetbrains/kotlinx/kotlinx-serialization-json-jvm/1.7.3/kotlinx-serialization-json-jvm-1.7.3.jar"
    }
)

foreach ($jar in $jars) {
    $dest = Join-Path $root $jar.Path
    if (Test-Path $dest) {
        Write-Host "already present: $($jar.Path)"
        continue
    }
    Write-Host "fetching: $($jar.Path)"
    Invoke-WebRequest -Uri $jar.Url -OutFile $dest
}
