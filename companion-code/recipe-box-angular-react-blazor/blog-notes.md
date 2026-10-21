# Recipe Box: Blog Notes

Running log for the post. Claude Code appends entries as it builds. The author adds reactions and corrections. Be specific and honest, and keep entries short.

**Entry format**

- **Surprise / mistake:** what happened, what fixed it
- **Decision:** what was chosen, and why
- **Snippet candidate:** `path/to/file:start-end`, and what it demonstrates

## Post framing

- Working title: Same App, Three Frameworks: A Recipe Box in Angular, React, and Blazor
- Slug (fixed): `recipe-box-angular-react-blazor`
- Built with Claude Code: say so plainly in the post
- Author's experience: Angular (strongest), Blazor, React (newest)
- Central idea: show real code side by side so readers get value without running anything

## Versions used

- .NET SDK / ASP.NET Core: 10.0 (SDK pinned to 10.0.100 via `global.json`)
- EF Core (Sqlite/Design), Microsoft.AspNetCore.OpenApi, Microsoft.AspNetCore.Mvc.Testing: 10.0.13
- xunit v3 4.0.2, Microsoft.NET.Test.Sdk 18.3.0
- Node 24 (typegen container only)
- TypeScript 5.9.x, openapi-typescript 7.13.0 (`tools/typegen`)

## Phase 1: API, shared library, Docker skeleton

### Surprises and mistakes

- **Surprise/mistake:** the first draft of `formatQuantity` in spec.md said "round to the nearest 1/8" for values under 10, but that rule can never produce 1/3 or 2/3 — 0.3333 would round to 3/8, not 1/3. This broke the spec's own planned e2e test asserting a 1/3 result. Fixed by snapping to the nearest of the full fraction set (`1/8, 1/4, 1/3, 3/8, 1/2, 5/8, 2/3, 3/4, 7/8`, plus whole numbers), not a strict 1/8 grid, before any code was written against the old rule.
- **Surprise/mistake:** `dotnet test` fails outright on the .NET 10 SDK with xunit v3 ("Testing with VSTest target is no longer supported"), even with `UseMicrosoftTestingPlatformRunner`/`TestingPlatformDotnetTestSupport` set. Worked around by building normally and running the produced test executable directly (`dotnet exec .../RecipeBox.Api.Tests.dll`), which uses xunit v3's own Microsoft.Testing.Platform runner and works cleanly, including `--filter-method`.
- **Surprise/mistake:** `WebApplicationFactory`'s `ConfigureAppConfiguration` test overrides (temp SQLite path, temp image folder) were silently ignored — Program.cs kept connecting to the hardcoded production default. Cause: Program.cs read `builder.Configuration[...]` into a local variable *before* `builder.Build()`, and the factory's config overrides only land once the host is actually built. Fixed by resolving `IConfiguration` lazily from DI inside `AddDbContext`/`AddSingleton` factory callbacks instead of reading it into a top-level variable.
- **Surprise/mistake:** PUT threw a 500 ("shadow key property 'Ingredient.__synthesizedOrdinal' is unknown") when replacing a recipe by fetching the existing row `AsNoTracking()` and calling `db.Recipes.Update(input)` on a brand-new detached graph. Owned JSON collections need a *tracked* owner to replace wholesale. Fixed by fetching the existing entity tracked (no `AsNoTracking`) and reassigning its `Ingredients`/`Steps`/`Tags` properties in place, rather than attaching a whole new detached graph.
- **Surprise/mistake:** the generated OpenAPI document had no `Recipe`, `Ingredient`, `Step`, or validation-problem schemas at all — only `IFormFile` — because every endpoint returned a bare `IResult` (`Results.Ok(...)`, `Results.ValidationProblem(...)`), which gives the minimal-API OpenAPI generator nothing to infer response types from. Fixed by chaining `.Produces<T>(status)` / `.ProducesValidationProblem()` on every route. Confirmed by generating real TypeScript types afterward and checking `imageUrl` came through as `null | string`, not widened.
- **Surprise/mistake:** proved the typegen type-level contract check (`tsc --noEmit type-checks.ts`) actually catches drift by deliberately asserting `imageUrl` as a plain `string` — it passed anyway, because plain `tsc` without `--strict` doesn't enable `strictNullChecks`, so `null` satisfies any type. The check is only meaningful with `--strict` explicitly on the command line (a bare `tsc --noEmit file.ts` ignores `tsconfig.json`, so strictness can't just live there).
- **Surprise/mistake:** Docker Desktop on Windows leaves empty stub `bin/`, `obj/`, and `node_modules` directories in the bind-mounted working tree when a named volume is nested at a subpath under a bind mount (e.g. `.:/workspace` plus `shared-bin:/workspace/shared/.../bin`). They contain zero files — the real build output lives in the named volume — but they're worth deleting and not mistaking for leaked build artifacts.
- **Surprise/mistake, since fixed:** `dotnet restore` reported known-vulnerability warnings (NU1902/NU1903) for a few transitive packages pulled in by the pinned 10.0.0 EF Core/OpenAPI packages (`Microsoft.OpenApi`, `SQLitePCLRaw.lib.e_sqlite3`, `System.Security.Cryptography.Pkcs`). Bumping `Microsoft.EntityFrameworkCore.Sqlite`/`.Design`, `Microsoft.AspNetCore.OpenApi`, and `Microsoft.AspNetCore.Mvc.Testing` from 10.0.0 to the latest patch, 10.0.13, pulled in patched versions of all three transitively and cleared every warning (confirmed via `dotnet list package --vulnerable --include-transitive` on all three projects).

### Decisions

- **Decision:** `Ingredients`/`Steps` are mapped as `OwnsMany(...).ToJson()` (a stable, long-documented EF Core JSON-column pattern), not EF Core's newer "complex type collections." Whole-document JSON replace semantics match the spec's full-replace PUT contract, and avoids betting Phase 1 on a brand-new EF 10 API with less field experience behind it.
- **Decision:** the recipe list and `/api/tags` endpoints fetch all rows and filter/group in memory rather than relying on EF's SQL translation of LINQ over JSON-mapped columns. This removes an entire category of translation risk for a demo-sized dataset (~10 recipes), and lines up with spec.md's own note that SQLite here is a deliberate non-scaling choice.
- **Decision:** title uniqueness is enforced in the API endpoint layer (a plain EF query against the `Recipes` table), not inside the shared `RecipeValidator`. Keeps `RecipeValidator` pure, DB-free, and fully unit-testable in `shared/`.
- **Decision:** Kestrel's `MaxRequestBodySize` is set to 10 MB, double the 5 MB upload cap, so a 5 MB file is always rejected by the application's own check (400) rather than the transport limit (413). Verified against a real running container (not `WebApplicationFactory`, whose in-memory `TestServer` doesn't enforce Kestrel limits at all) with a real >10 MB upload, confirming 413.
- **Decision:** malformed JSON bodies are parsed by hand (`Endpoints/JsonBody.cs`) instead of relying on minimal API's automatic parameter binding, so the exact error-path key (no `$.` prefix, `""` for a missing body) is under our control rather than dependent on undocumented framework internals.
- **Decision (the pattern behind the last two entries):** both `JsonBody.cs`'s manual parsing and the explicit `.Produces<T>()`/`.ProducesValidationProblem()` annotations exist because minimal API's defaults are tuned for "an API that works," not "an API whose wire contract exactly matches a written spec." Automatic body binding gives *some* 400 on bad JSON, and `Results.Ok(...)` gives *some* 200 response — but spec.md pins down the exact error key grammar and the exact response shapes (for typegen to consume later), and neither default gets there without being told explicitly.

### Snippet candidates

- `shared/RecipeBox.Shared/Quantities.cs:1-62` — `scaledQuantity`/`formatQuantity`, including the corrected fraction-snapping algorithm and the 4-decimal rounding step.
- `shared/RecipeBox.Shared/RecipeValidator.cs` — hand-rolled validator producing the exact camelCase, 0-based field-path error keys the spec and e2e suite depend on.
- `api/RecipeBox.Api/Endpoints/JsonBody.cs` — manual JSON body parsing for precise malformed-JSON error keys.
- `api/RecipeBox.Api/Images/ImageSignature.cs` — hand-rolled magic-byte image signature sniffing, no package dependency.
- `api/RecipeBox.Api/RecipeDbContext.cs` — `OwnsMany(...).ToJson()` mapping and the UTC `DateTime` value converter.

## Phase 2: Angular (reference implementation)

### Versions used

- Angular 22.2.3 (CLI/`@angular/build`/core, confirmed via `npm view` rather than assumed), scaffolded with the esbuild/Vite `@angular/build:application` builder
- Zoneless by default: a fresh `ng new` in this version installs no `zone.js` dependency at all; `provideZonelessChangeDetection()` is added explicitly in `app.config.ts` anyway, since relying on an absent polyfill to imply zoneless felt like the wrong thing to build on
- Unit tests: Vitest 5.0.5 via the `@angular/build:unit-test` builder (`ng test`) — Karma is gone from the default template
- Tailwind CSS 4.3.3 (CSS-first `@theme` config, no `tailwind.config.js`), wired through `@tailwindcss/postcss` 4.3.3
- TypeScript ~6.0.2 (Angular's own pin; separate from `tools/typegen`'s 5.9.x, which has its own `package.json`)
- Playwright 1.64.1, Docker image `mcr.microsoft.com/playwright:v1.64.1-noble` (confirmed the tag exists before building against it)

### Surprises and mistakes

- **Surprise/mistake:** `withComponentInputBinding()` sets a route/query-param-bound `input()` to the literal JS value `undefined` when the URL simply doesn't have that param — not the input's declared default. Passing that straight into `httpResource`'s `params` object serialized to the literal query string `search=undefined&tag=undefined`, which the API treated as actual (non-blank) filter values, breaking the recipe list and tag filtering entirely. Fixed by normalizing with `?? ''` everywhere a query-param-bound input value gets used (`recipe-list.ts`, `tag-filter.ts`). This only showed up once things ran in a real browser against the real API — unit tests and the strict `ng build` both stayed green throughout.
- **Surprise/mistake:** in the Playwright skeleton/error/retry test, calling `page.unroute(url => isRecipeList(url))` with a freshly-created wrapper arrow function did not remove the route registered earlier with a *different* wrapper arrow function — Playwright matches routes to unroute by function reference, not by behavior, so the old route handler silently stayed active and stacked with the new one. Fixed by holding the handler in a named `const` and passing that exact same reference to both `route()` and `unroute()`.
- **Surprise/mistake (same shape as Phase 1):** `docker compose run --rm ng ...` kept using a stale `node_modules` named volume after `package.json` changed (adding `@types/node` for `quantities.spec.ts`'s `node:fs`/`node:path` imports) — the volume isn't refreshed just because the underlying image changed. Fixed by `docker volume rm` before the next run, same as Phase 1's bin/obj lesson.
- **Surprise/mistake:** Docker Desktop's nested-named-volume-under-bind-mount trick (same mechanism documented in Phase 1) leaves a non-empty-looking `clients/angular/node_modules/` stub in the working tree — specifically a single `.package-lock.json` metadata file, not real `node_modules` content. It's already gitignored and harmless; worth not mistaking it for a leaked install.
- **Mistake caught by review, not by a failing test:** the Angular production build initially failed on two real type errors that the earlier unit-test run hadn't caught (different compilation unit): `WritableSignal` imported from `@angular/forms` instead of `@angular/core`, and `patchValue`-ing a nullable `unit: string | null` from the API straight into a non-nullable form control. Both are compile-time catches from the strict `ng build` itself — exactly the "build is the contract check" design the plan called for working as intended.

### Decisions

- **Standalone components, signals, zoneless, built-in `@if`/`@for` control flow, suffix-free file naming (`recipe-card.ts` / `RecipeCard`, not `recipe-card.component.ts`)** — all current Angular CLI defaults as of 22.2.3, not a deliberate swim against the grain.
- **Reactive forms, not Signal Forms.** `@angular/forms/signals` is not part of this Angular version's stable public API surface (not present in the scaffolded dependencies) — reactive forms stayed the default per the plan, with no idiom-swap decision actually needed.
- **`httpResource` for all reads, injected `HttpClient` for writes.** No service layer or store beyond `RecipeCount` — it's the one place CLAUDE.md's "no abstractions" rule meets spec.md's one explicit shared-state requirement (the header count), so it's the one service that exists.
- **`linkedSignal` for the servings stepper's current value**, keyed off the loaded recipe's own `servings` — handles "initialize once the recipe loads" and "reset when navigating to a different recipe" as one declarative line, no manual `ngOnChanges`-style reset logic.
- **Server-error-to-form-control mapping is pure string manipulation** (`ingredients[0].quantity` → `ingredients.0.quantity`, resolved via `FormGroup#get`) — works because `FormArray` index order is exactly what gets sent to the server, so reordering/removing rows can never misroute an error onto the wrong control.
- **Angular runs the full image-upload path**, not the spec's optional fallback — e2e test 12 covers both the real upload and the real wrong-type rejection against this client, not the reduced form.

### Snippet candidates

- `src/app/quantities.ts:1-50` — the TypeScript port of `scaledQuantity`/`formatQuantity`, line-for-line next to the C# original, including the 4-decimal-rounding-before-snapping step.
- `src/app/recipe-count.ts` — the full `RecipeCount` signal service, ~15 lines, the header-count cross-framework comparison point.
- `src/app/recipe-detail/recipe-detail.ts` — the `linkedSignal` servings-reset-on-navigation line.
- `src/app/recipe-form/apply-server-errors.ts` — the server-error-key-to-FormControl mapping.
- `e2e/tests/list.spec.ts` — Playwright route interception for the loading-skeleton/forced-error states, reusable unmodified against the other two clients later.

## Phase 3: Blazor WebAssembly

### Surprises and mistakes

(Watch for: `InputFile` max size, nginx MIME types for `.wasm`, Tailwind scanning `.razor` files.)

### Decisions

### Snippet candidates

## Phase 4: React

### Surprises and mistakes

(The author's learner's perspective matters most here. Note where Angular habits misled.)

### Decisions

(TanStack Query, controlled inputs, form approach.)

### Snippet candidates

## Cross-framework comparisons

Feature-by-feature notes for the side-by-side sections. Candidates for full side-by-side treatment:

### Derived state (servings scaler)

### Repeating ingredients form and server error mapping

### Fetching with loading, empty, and error states

### Shared model and validation (Blazor shared library vs. generated types and re-expressed rules)

Lighter treatment or follow-up posts: routing, header summary (shared state), image upload.

## Tailwind notes

(Gotchas that showed up in more than one framework, such as dynamic class names. Record the Tailwind version.)

## Docker and Playwright notes

(What it took to get one suite passing against all three containers.)

## Metrics (optional, with caveats)

File counts and lines of code per client. This measures verbosity, not quality, and the post should say so.

## Follow-up post ideas

- Auth as a visible diff (BFF with cookies vs. tokens in the browser)
- Forms deep dive
- State management
- Testing
- Deployment

## Author's reactions

(The author writes here. Claude Code does not.)
