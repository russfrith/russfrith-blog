# Recipe Box: Code Conventions

This file applies to everything under `companion-code/recipe-box-angular-react-blazor/`. The root `CLAUDE.md` (prose and voice rules) still applies on top of it, including when you write `blog-notes.md` or draft post content.

## Purpose

Recipe Box is one small app built three times (Angular, React, Blazor WebAssembly) against one ASP.NET Core API. It exists to support a blog post that compares the frameworks with real, side-by-side code. **The post is the product. The repo is proof that the snippets are real and work.** Most readers will never open the repo, so the code must read well when quoted.

The author is an experienced C# and TypeScript developer: strongest in Angular, then Blazor, then React. Do not build everything silently. Explain React and Blazor choices briefly, and log surprises (see Blog notes).

## Fixed slug, flexible title

- The slug `recipe-box-angular-react-blazor` is fixed. Never rename the folder, even if the post title changes.
- The working title is "Same App, Three Frameworks: A Recipe Box in Angular, React, and Blazor". It may change. Do not "fix" the folder or any paths to match it.
- The Hugo post lives in the blog's content folder. Never put code in `content/`, `static/`, or `assets/`.

## Layout

```
recipe-box-angular-react-blazor/
  CLAUDE.md
  spec.md
  blog-notes.md
  README.md
  docker-compose.yml
  docker-compose.dev.yml
  docker-compose.e2e.yml
  .gitignore
  .gitattributes
  api/            ASP.NET Core minimal API (EF Core + SQLite)
  shared/         C# models, validation, scaling and fraction logic
  clients/
    angular/
    react/
    blazor/
  e2e/            One Playwright suite for all three clients
  seed/           Committed seed images (small, compressed)
  tools/typegen/  Containerized OpenAPI-to-TypeScript generation (openapi-typescript)
```

## Versions

Pin versions and record them here before writing code. Use current stable releases, and the current .NET LTS.

- .NET SDK / ASP.NET Core: 10.0 LTS, SDK pinned to 10.0.100 in `global.json`
- Microsoft.EntityFrameworkCore.Sqlite / .Design, Microsoft.AspNetCore.OpenApi, Microsoft.AspNetCore.Mvc.Testing: 10.0.13 (latest 10.0.x patch as of this check; bumped from 10.0.0 to clear transitive NU1902/NU1903 vulnerability warnings in `Microsoft.OpenApi`, `SQLitePCLRaw.lib.e_sqlite3`, and `System.Security.Cryptography.Pkcs` — see blog-notes.md)
- dotnet-ef (local tool, `.config/dotnet-tools.json`): 10.0.13
- Test framework: xunit v3 4.0.2, xunit.runner.visualstudio 4.0.1, Microsoft.NET.Test.Sdk 18.3.0
- Angular: 22.2.3 (`@angular/build:application` esbuild/Vite builder, zoneless, standalone components, built-in `@if`/`@for` control flow)
- Angular unit tests: Vitest 5.0.5 via the `@angular/build:unit-test` builder (`ng test`)
- React: 19.3.1. Build tool: Vite 8.3.5 (`@vitejs/plugin-react` 6.1.2, scaffolded via `npm create vite@latest -- --template react-ts`). Router: react-router 8.5.0 (declarative `<Routes>`/`<Route>`, not the data/loader API — a second server-state mechanism would conflict with "TanStack Query, and nothing else"). Data fetching/shared state: `@tanstack/react-query` 5.104.5, `QueryClient` configured with `retry: false` (e2e tests 4/14 need the error/not-found state to show well inside Playwright's default timeout; none of the three clients retry). Lint: oxlint 1.88.0 (the create-vite default, not ESLint) — `react/rules-of-hooks` is on by default. Unit tests: Vitest 5.0.5 (no test runner is scaffolded by default; added explicitly, matching Angular's pin)
- Blazor render mode: standalone WebAssembly. Microsoft.AspNetCore.Components.WebAssembly: 10.0.13 (no `.DevServer` package — Blazor has no local dev-watch loop against the API, see Docker section)
- Tailwind CSS: 4.3.3 (CSS-first `@theme` config). Angular/React: `@tailwindcss/postcss` 4.3.3. Blazor: the standalone Tailwind CLI (no Node involved), same 4.3.3
- Playwright: 1.64.1, Docker image `mcr.microsoft.com/playwright:v1.64.1-noble` (tag confirmed to match the npm package version)
- Node: 24.x LTS (typegen container and later client build containers only — never a host install)
- TypeScript: 5.9.x in `tools/typegen` (openapi-typescript's peer range is `^5.x`; do not take the current TypeScript 7.x there)
- openapi-typescript: 7.13.0

## TypeScript generation: openapi-typescript, not NSwag

Decided in the Phase 1 plan, as flagged above:

- NSwag generates framework-specific clients (an Angular service vs. a fetch client). That difference would be an artifact of the generator, not the framework, and would contaminate the actual comparison the post is making. openapi-typescript emits types only, so Angular keeps idiomatic `HttpClient` and React keeps `fetch` plus TanStack Query — both idiomatic, and differing only where the frameworks do.
- NSwag's CLI needs a .NET runtime even from npm. openapi-typescript runs on a plain Node image, which keeps the typegen container simple and dependency-free.
- .NET 10 emits OpenAPI 3.1 by default. openapi-typescript 7 handles 3.1 natively (including nullable unions); NSwag/NJsonSchema's 3.1 support lags and would force downgrading the document to 3.0.

**Rule:** before using any API you are not certain about in Angular, React, Blazor, Tailwind, EF Core, or Playwright, check the current official docs. Do not rely on memory. Framework idioms change quickly. Use modern idioms (Angular standalone components and signals, React function components and hooks, current Blazor patterns) and say which idiom you chose and why in `blog-notes.md`.

## Code written to be quoted

Readers see excerpts, not the repo. Write accordingly.

- One concept per component or file. Key files should be short enough to paste whole (about 40 lines).
- No clever abstractions, shared helper layers, or indirection that forces a reader to open a second file to understand the first.
- Use identical names for the same concept in all three clients (`RecipeCard`, `RecipeForm`, `scaledQuantity`, `formatQuantity`, and so on) so side-by-side snippets differ only where the frameworks do.
- Keep code comments minimal. The prose around the snippet does the explaining.
- Prefer the most idiomatic, readable approach in each framework over the most compact one.
- No component libraries (no Material, Headless UI, MudBlazor, and so on). Hand-build components with Tailwind so the comparison stays fair.
- React data fetching: TanStack Query, and nothing else for server state.
- Do not add features beyond `spec.md`.

## Shared code

- `shared/` is a C# class library referenced by both the API and the Blazor client. It holds `Recipe`, `Ingredient`, `Step`, a hand-written `RecipeValidator` (returns a field-path-keyed error dictionary, not attribute-based validation) and `RecipeNormalizer`, and the pure scaling and fraction-formatting functions (unit tested).
- Angular and React get TypeScript types generated from the API's OpenAPI document via openapi-typescript (see "TypeScript generation" below).
- Validation rules and the scaling and fraction logic must be re-expressed by hand in TypeScript for Angular and React, matching the shared test vectors in `shared/test-vectors.json` exactly. Keep those ports faithful and obvious. The duplication is a point the post makes.
- The server is always the source of truth for validation. Client-side validation is a convenience.
- File upload types (`IFormFile`, `IBrowserFile`) never go in `shared/`. Image upload is a separate endpoint from recipe JSON.

## Styling

- Tailwind CSS in all three clients, using one shared theme token file (colors, fonts, radii, shadows) copied identically into each client.
- Same utility classes in all three clients. Only the attribute differs (`class` vs `className`).
- Never build class names dynamically (no `` `bg-${color}-500` ``). Tailwind's scanner will not find them. Use complete class strings.
- Blazor uses the standalone Tailwind CLI, scanning `.razor` files.
- Design target: warm, restrained, product-like, not demoware. Serif for recipe titles, clean sans for UI, off-white backgrounds, generous whitespace, visible focus styles, sufficient contrast.
- Every screen needs real loading (skeleton), empty, and error states.

## Test IDs

Every interactive or asserted element has a `data-testid` taken from `spec.md`. The names are identical across all three clients. Do not invent new ones without adding them to `spec.md`.

## Docker

- Docker with Compose is the only prerequisite for readers. `docker compose up` must work with no .NET SDK, Node, or Playwright installed, and must not change anything on the host.
- All builds are multi-stage inside Dockerfiles. No step assumes host-installed tools.
- The SQLite database and uploaded images live in a named volume. A fresh container creates and seeds the database on first start, and `docker compose down -v` returns to a clean state.
- The default compose file has no bind mounts that write into the repo.
- Type generation, Playwright runs, and screenshots run in containers.
- Do not use or install host toolchains (`dotnet`, `node`, `npm`, `ng`, `playwright`, `nswag`, and so on). Every build, test, type generation, and script runs through Docker, using `docker compose run` or compose services.
- `docker-compose.dev.yml` is a separate override for iteration: source sync or watch, with `node_modules` and `bin`/`obj` kept in named volumes so nothing generated lands in the working tree. The default `docker-compose.yml` stays free of bind mounts that write into the repo.
- Shell scripts use LF line endings, enforced by `.gitattributes`, since the host is Windows and the containers are Linux.
- Each client builds to static files and is served by nginx with SPA fallback routing and `/api` proxied to the API.
- Blazor needs correct MIME types for `.wasm` and framework files in nginx, in addition to SPA fallback.
- `docker-compose.yml` runs the API and all three clients. SQLite lives on a volume.
- A test-only reset-and-reseed endpoint exists, enabled only when `ASPNETCORE_ENVIRONMENT=Testing`. The suite calls it before each run.
- One Playwright suite in `e2e/` runs against any client through a `BASE_URL` variable, using the official Playwright image.
- `docker-compose.e2e.yml` is a third compose file, layered on top of the default (`docker compose -f docker-compose.yml -f docker-compose.e2e.yml ...`): it sets `ASPNETCORE_ENVIRONMENT=Testing` on the `api` service (the default file runs Production, so the reset endpoint is 404 under a plain `docker compose up` by design) and adds the `e2e` service that runs the Playwright suite.

**Definition of done for a client:** the shared Playwright suite passes against that client's container from a clean `docker compose -f docker-compose.yml -f docker-compose.e2e.yml up --build`, and its entries in `blog-notes.md` are complete. (A plain `docker compose up --build` with no override is the reader-facing "just run it" path and intentionally can't run the suite — it has no Testing-only reset endpoint to call.)

## Workflow

1. Use plan mode at the start of each phase. Wait for approval before writing files.
2. Phases: (0) scaffolding, (1) API, shared library, Docker skeleton, (2) Angular plus the Playwright suite, (3) Blazor, (4) React, (5) parity pass and polish.
3. End each phase with a short summary of what changed, and propose the commit message and git tag (`recipe-box-phase-N`). Never commit or tag until the author says so.
4. Keep commits small and readable. Follow the Conventional Commits rules in the root `CLAUDE.md` (for example `feat(api): ...`, `test(shared): ...`, `chore(recipe-box): ...`). Do not restate or override them here.

## Blog notes

Append to `blog-notes.md` as you work, in the framework's section:

- Surprises, mistakes, and "what I got wrong first" moments, honestly and specifically.
- Decisions and their reasons.
- **Snippet candidates:** file path, line range, and one line on what it demonstrates.

The author adds their own reactions. Do not write the author's reactions for them.

## Snippet integrity

Post snippets are copied from the tagged commit. When asked, verify that every code block in the post matches the file at the tag, and report any drift.
