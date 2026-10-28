# Python scenarios

Each `NN_name/` folder has `broken.py`/`fixed.py` (or `demo.py` where there's no
meaningful broken/fixed split), `output.txt` (verbatim captured output, command,
and interpreter version), and `NOTES.md` (commentary and investigation notes --
not part of the captured output).

Run any scenario directly against the pinned interpreter:

```
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src python:3.14.8-slim python broken.py
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src python:3.14.8-slim python fixed.py
```

Scenario 8 needs the mock server reachable:

```
docker compose -f ../../docker-compose.yml up -d mockserver
docker run --rm --network programming-languages-then-and-now_wordfreq -v "${PWD}/08_untrusted_json:/src" -w /src python:3.14.8-slim python broken.py
```

See each scenario's own `output.txt` for the exact command(s) used.
