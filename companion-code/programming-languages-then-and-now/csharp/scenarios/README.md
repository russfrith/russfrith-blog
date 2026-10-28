# C# scenarios

Each scenario is its own tiny project (a `broken/` and/or `fixed/` folder, each
with its own `scenario.csproj` + `Program.cs`), because a single .NET project can
only have one entry point:

```
docker run --rm -v "${PWD}/01_absent_null/broken:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet run
docker run --rm -v "${PWD}/01_absent_null/fixed:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet run
```

`dotnet run` with no `-c` flag builds Debug by default. Scenario 7 is captured in
BOTH Debug and Release (`dotnet run -c Release`) specifically because the
difference matters for a timed result -- see its `output.txt` and `NOTES.md`.

Scenarios 7 and 8 need the mock server reachable
(`docker compose up -d mockserver` plus
`--network programming-languages-then-and-now_wordfreq` on the `docker run`).

See each scenario's own `output.txt` for the exact command(s) used.
