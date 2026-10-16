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
docker compose up --build api
```

The API listens on `http://localhost:5080`, seeded automatically on first
start. `docker compose down -v` returns to a clean state (fresh reseed on
the next `up`). Angular, React, and Blazor client services are added in
later phases; this compose file currently only runs the API.

For local iteration (dotnet build/test, EF migrations, typegen), see
`docker-compose.dev.yml` — for example:

```
docker compose -f docker-compose.yml -f docker-compose.dev.yml run --rm dotnet dotnet build
```

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
