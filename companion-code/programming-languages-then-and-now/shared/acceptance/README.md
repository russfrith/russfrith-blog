# Independent expected-output verification

`verify-expected` recomputes wordfreq's expected output using only `curl`, `tr`,
`sort`, `uniq`, `head`, and `awk` in a plain `debian:bookworm-slim` container --
no code shared with any of the six `wordfreq` implementations under test. Same
tie-break rule as the CLI contract: count descending, then word alphabetically
ascending.

## Running it

```
docker compose up -d mockserver
docker run --rm --network programming-languages-then-and-now_wordfreq \
  -v "${PWD}/shared/acceptance/verify-expected:/verify-expected" \
  debian:bookworm-slim sh -c \
  "apt-get update -qq && apt-get install -y -qq curl gawk >/dev/null 2>&1; \
   sh /verify-expected 5 http://mockserver:8080/fixed/alpha.html http://mockserver:8080/fixed/beta.html"
```

Swap the URL list and change the top-N argument for other fixtures.

## Result (2026-10-28)

Both existing expected-output files were recomputed independently and matched
exactly, with no disagreement:

**Happy path** (`alpha.html` + `beta.html`, top 5):
```
the 5
body 4
html 4
fox 2
in 2
```
Matches `shared/expected-output/happy-top5.txt` exactly.

**Partial failure** (`alpha.html` only, top 5):
```
the 3
body 2
fox 2
html 2
brown 1
```
Matches `shared/expected-output/partial-top5.txt` exactly.

No changes were made to `shared/expected-output/*.txt` as a result of this check
-- the files were already correct.
