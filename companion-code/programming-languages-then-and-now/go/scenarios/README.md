# Go scenarios

Each `NN_name/` folder has `broken.go`/`fixed.go` (or `demo.go`), `output.txt`
(verbatim captured output, command, and toolchain version), and `NOTES.md`
(commentary -- not part of the captured output).

Run any scenario directly against the pinned toolchain:

```
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src golang:1.26.4-bookworm go run broken.go
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src golang:1.26.4-bookworm go run fixed.go
```

Scenario 2 uses the race detector:

```
docker run --rm -v "${PWD}/02_data_race:/src" -w /src golang:1.26.4-bookworm go run -race broken.go
```

Scenarios 7 and 8 need the mock server reachable:

```
docker compose -f ../../docker-compose.yml up -d mockserver
docker run --rm --network programming-languages-then-and-now_wordfreq -v "${PWD}/08_untrusted_json:/src" -w /src golang:1.26.4-bookworm go run broken.go
```

See each scenario's own `output.txt` for the exact command(s) used.
