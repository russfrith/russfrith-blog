# C safety-baseline scenarios

Scenarios 1-5 only -- no CLI, no scenarios 6-8 (C isn't a core comparison
language, just the safety baseline).

```
docker run --rm -v "${PWD}/01-absent-null:/src" -w /src gcc:16.2 bash -c "gcc -O0 -g -Wall broken.c -o broken && ./broken"
docker run --rm -v "${PWD}/01-absent-null:/src" -w /src gcc:16.2 bash -c "gcc -O0 -g -Wall fixed.c -o fixed && ./fixed"
```

Scenario 2 adds `-lpthread`; scenario 5 additionally uses
`-fsanitize=address` for the leak-detector run. See each scenario's own
`output.txt` for the exact flags used.
