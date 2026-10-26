# Recipe Box: Angular, React, and Blazor

Companion code for the blog post "Same App, Three Frameworks: A Recipe Box in
Angular, React, and Blazor" (working title). One small recipe-management app,
built three times (Angular, React, Blazor WebAssembly) against one shared
ASP.NET Core API, so the frameworks can be compared with real, side-by-side
code.

See `spec.md` for the full contract (models, endpoints, screens, test IDs)
and `CLAUDE.md` for the conventions this code follows.

## Layout

```
api/            ASP.NET Core minimal API (EF Core + SQLite)
shared/         C# models, validation, scaling and fraction logic
clients/
  angular/
  react/
  blazor/
e2e/            One Playwright suite for all three clients
seed/           Committed seed images
tools/typegen/  Containerized OpenAPI-to-TypeScript generation
```

## Running it

Docker with Compose is the only requirement; no .NET SDK, Node, or
Playwright needs to be installed on the host.

```
docker compose up --build
```

The API listens on `http://localhost:5080`, seeded automatically on first
start. The Angular client listens on `http://localhost:5081`, Blazor on
`http://localhost:5082`, and React on `http://localhost:5083` (all three
proxying `/api` to the API). `docker compose down -v` returns to a clean
state (fresh reseed on the next `up`). Port scheme: api 5080, angular
5081, blazor 5082, react 5083.

For local iteration (dotnet build/test, EF migrations, typegen, `ng
serve`/`ng test`, Vite dev server/`vitest`), see `docker-compose.dev.yml`
— for example:

```
docker compose -f docker-compose.yml -f docker-compose.dev.yml run --rm dotnet dotnet build
docker compose -f docker-compose.yml -f docker-compose.dev.yml run --rm ng npx ng test --watch=false
docker compose -f docker-compose.yml -f docker-compose.dev.yml up ng
docker compose -f docker-compose.yml -f docker-compose.dev.yml run --rm vite npx vitest run
docker compose -f docker-compose.yml -f docker-compose.dev.yml up vite
```

Blazor has no equivalent local dev-serve loop (its WASM dev server has no
`/api` proxy) — iterate with `dotnet build`/`dotnet publish` via the
`dotnet` service above, and use `docker compose up --build blazor` to see
it running end to end.

To run the shared Playwright e2e suite against a client, layer
`docker-compose.e2e.yml` on top of the default file — this puts the API
in `Testing` mode, which is required for the suite's reset endpoint and
is never exposed under the plain default compose file:

```
docker compose -f docker-compose.yml -f docker-compose.e2e.yml down -v
docker compose -f docker-compose.yml -f docker-compose.e2e.yml up -d --build api angular
docker compose -f docker-compose.yml -f docker-compose.e2e.yml run --rm --build e2e
```

Swap `angular` for `blazor` or `react` (and pass
`-e BASE_URL=http://blazor:8080` or `-e BASE_URL=http://react:8080` to
the final `run`) to test that client instead — the suite itself needs no
changes either way.

Screenshots land in `e2e/screenshots/<client>/` (gitignored, overwritten
each run) for copying into the post.

SQLite is a deliberate choice for this single-instance demo; it isn't
meant to scale beyond one API replica.

You may notice empty `bin/`, `obj/`, or `node_modules/` folders appear in
the working tree after running dev commands. They're a harmless side
effect of Docker Desktop on Windows mounting a named volume at a subpath
under a bind mount — the real build output lives in the named volume, not
there. They're gitignored and safe to delete.

## Post tags

TODO: map each git tag (`recipe-box-phase-N`) to the section of the post it
corresponds to, once phases are tagged.

| Tag | Phase | Post section |
|---|---|---|
| `recipe-box-phase-0` | Scaffolding | TODO |
| `recipe-box-phase-1` | API, shared library, Docker skeleton | TODO |
| `recipe-box-phase-2` | Angular | TODO |
| `recipe-box-phase-3` | Blazor | TODO |
| `recipe-box-phase-4` | React | TODO |
| `recipe-box-phase-5` | Parity pass and polish | TODO |
