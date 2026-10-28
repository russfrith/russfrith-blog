# Kotlin scenarios

## One-time setup

Build the `kotlinc` helper image (not an official image -- `kotlinc` has no
official Docker image, so this builds one from the pinned compiler zip):

```
docker build -t kotlinc-2.4.0 -f Dockerfile.kotlinc .
```

Scenarios 7 and 8 need extra jars (kotlinx-coroutines, kotlinx.serialization +
its compiler plugin) that aren't bundled with `kotlinc`. Fetch them once,
pinned and documented, instead of downloading by hand:

```
pwsh ./fetch-jars.ps1
```

## Running a scenario

```
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src kotlinc-2.4.0 bash -c "kotlinc Broken.kt -include-runtime -d broken.jar && java -jar broken.jar"
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src kotlinc-2.4.0 bash -c "kotlinc Fixed.kt -include-runtime -d fixed.jar && java -jar fixed.jar"
```

Scenario 7 and 8 need the extra jars on the classpath at both compile and run
time, e.g.:

```
docker run --rm -v "${PWD}/07_cpu_heavy_blocking:/src" -w /src kotlinc-2.4.0 bash -c "kotlinc -cp coroutines.jar SingleThreadBroken.kt -include-runtime -d single.jar"
docker run --rm -v "${PWD}/07_cpu_heavy_blocking:/src" -w /src kotlinc-2.4.0 java -cp "single.jar:coroutines.jar" SingleThreadBrokenKt
```

Scenario 8 also needs the mock server reachable (`docker compose up -d mockserver`
plus `--network programming-languages-then-and-now_wordfreq` on the `docker run`).

See each scenario's own `output.txt` for the exact command(s) used.
