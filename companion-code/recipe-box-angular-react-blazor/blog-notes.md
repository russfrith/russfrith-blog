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

### Surprises and mistakes

### Decisions

(Idioms chosen: standalone components, signals, reactive forms, and why.)

### Snippet candidates

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
