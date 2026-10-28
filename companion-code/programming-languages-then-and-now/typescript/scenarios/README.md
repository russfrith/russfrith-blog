# TypeScript scenarios

Node 26 runs `.ts` files directly via built-in type stripping -- no build step
needed to execute them:

```
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src node:26-bookworm-slim node broken.ts
docker run --rm -v "${PWD}/01_absent_null:/src" -w /src node:26-bookworm-slim node fixed.ts
```

Type stripping does NOT type-check. Every scenario's `output.txt` also includes a
real `tsc --noEmit --strict` run (A2) using the exact TypeScript/@types/node
versions pinned in `package.json` here:

```
docker run --rm -v "${PWD}:/src" -w /src node:26-bookworm-slim bash check-all.sh
```

Scenarios 2/7 (worker_threads) and 8 (mock server) have no extra setup beyond
`docker compose up -d mockserver` for 8. See each scenario's own `output.txt` for
the exact command(s) used.
