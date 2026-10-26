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
- Blazor: Microsoft.AspNetCore.Components.WebAssembly 10.0.13 (standalone WebAssembly, no server/hosted mode); Tailwind standalone CLI 4.3.3 (no Node involved in Blazor's own styling pipeline)
- TypeScript 5.9.x, openapi-typescript 7.13.0 (`tools/typegen`)
- React 19.3.1, Vite 8.3.5 (`@vitejs/plugin-react` 6.1.2), react-router 8.5.0, `@tanstack/react-query` 5.104.5, oxlint 1.88.0, Vitest 5.0.5 (all confirmed via `npm view`/the actual `npm create vite@latest` scaffold output, not assumed)

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

- **Surprise/mistake:** `IBrowserFile.OpenReadStream()` defaults to a 512,000-byte (`maxAllowedSize`) limit and throws `IOException` past it. The seed images are all under 2 KB, so the e2e suite's own image test would never catch a missing explicit limit - confirmed separately by uploading a real ~1.3 MB photo through the running app, which only succeeded once `OpenReadStream(MaxImageBytes)` passed an explicit 5 MB limit matching the server's own cap.
- **Surprise/mistake:** nginx's 1 MB default `client_max_body_size` would have 413'd uploads under the app's own 5 MB limit - the same bug class Angular's nginx.conf needed fixing in Phase 2, hit again here and caught before it shipped, since we knew to check for it this time.
- **Surprise/mistake:** `data-testid` attributes on Razor *components* (`InputNumber`, `InputText`, `InputTextArea`) can't mix literal text and a C# expression in one value (`data-testid="ingredient-quantity-input-@index"` fails to compile with RZ9986 - "Component attributes do not support complex content"). This restriction is specific to component tags; the identical pattern works fine on plain HTML elements (`<div data-testid="step-row-@index">`). Fixed by wrapping the whole value in one interpolated expression: `data-testid="@($"ingredient-quantity-input-{index}")"`.
- **Surprise/mistake:** a real rendering bug, not just a Razor syntax error: `<span>...</span>@if (unit) { " " + unit } <span>...</span>` renders with NO space between the unit and the following element in the browser (`"1/3 cupflour"` instead of `"1/3 cup flour"`), because Razor trims the whitespace between a code block and the markup that follows it - unlike Angular's template compiler, which preserves it as a normal collapsed space. Caught by comparing a Blazor screenshot side-by-side with Angular's equivalent page, not by any functional test (Playwright's `getByTestId` assertions don't look at surrounding text layout). Fixed by making the space part of the interpolated string itself (`$" {unit} "` / `" "`) rather than relying on markup whitespace.
- **Surprise/mistake:** `.razor.cs` code-behind files (and plain `.cs` files like `RecipeCount.cs`) do **not** pick up `_Imports.razor`'s `@using` directives - that file only applies to `.razor` files. `GetFromJsonAsync`/`PostAsJsonAsync`/`PutAsJsonAsync`/`ReadFromJsonAsync` all come from `System.Net.Http.Json`, which had to be added as an explicit `using` in every `.cs`/`.razor.cs` file that calls them.
- **Surprise/mistake:** giving the tag-entry `<input>` a `form="none-such"` attribute (pointing at a nonexistent form id) successfully detaches it from the surrounding `<EditForm>`, so pressing Enter while typing a tag can't trigger implicit form submission - confirmed working in a real browser via the e2e run, with no JS interop needed and no risk of blocking normal typing (which a blanket `@onkeydown:preventDefault` would have done, since that modifier is static per-element, not conditional per key).
- **Surprise/mistake (documented, not "fixed"):** `screenshots.spec.ts`'s `page.goto()` triggers a full browser navigation (not an in-app route change), which means Blazor's entire WASM runtime cold-boots from scratch on every single screenshot-test navigation, even moving between two pages of the same app a few lines apart. Confirmed reproducible across three repeated runs: the form-page screenshot consistently captures the `h-96 animate-pulse` loading skeleton rather than the settled form, because `page.goto()` resolves on the browser's `load` event, well before the C# app has finished its first async data fetch. This is purely a screenshot-timing artifact, not a functional defect - every real assertion in the suite uses `getByTestId`, which auto-waits and passes reliably regardless of boot time. Per the ground rule for this suite ("same suite, zero edits, any client"), this is logged rather than worked around by touching `screenshots.spec.ts`; it is also a legitimate, bloggable contrast with Angular's near-instant hydration.

### Decisions

- **Decision:** Blazor takes spec.md's full image-upload path, not the documented reduced-test-12 fallback. Confirmed by reading `e2e/tests/image.spec.ts` directly before building anything: the suite only contains the full-path test and the wrong-file-type-rejection test, with no reduced variant, so the fallback was never actually available here without editing the shared suite.
- **Decision:** the header's "state container" (`RecipeCount.cs`) is a *scoped* DI service, not singleton - it depends on the scoped `HttpClient`, and a WASM app's root scope lives for the whole session anyway, so it behaves like a singleton in practice without fighting the DI container's own rules.
- **Decision:** both client-side (`RecipeValidator.Validate()`) and server (`ValidationProblem`) errors are written into **one** `ValidationMessageStore`, keyed by the exact same literal field-path strings the server already uses (`ingredients[0].quantity`, `tags[1]`, `file`) via `EditContext.Field(string)`, which accepts any string and doesn't require a real property of that name to exist. This needed zero bracket-to-dot-path translation, unlike Angular's `apply-server-errors.ts` - the single clearest piece of "Blazor's form validation model happens to fit this problem unusually well" for the post.
- **Decision:** `RecipeValidation`'s validator runs against a **deep clone** of the bound `Recipe`, never the live model bound to the form. `RecipeNormalizer.Normalize()` reassigns `recipe.Tags` to a brand-new list rather than mutating the existing one in place (confirmed by reading `RecipeNormalizer.cs` before writing any validation code) - normalizing the live model would have left `TagInput`'s own list reference silently pointing at stale data.
- **Decision:** no new unit test files for Blazor, deliberately. There is no ported logic to test - `Quantities`/`RecipeValidator` already have full xunit coverage in `shared/`, and the ~12-line `RecipeCount` state container is exercised end-to-end by the real e2e suite (tests 7 and 13). Adding bUnit purely to re-test framework glue would itself be the kind of unnecessary abstraction this project's conventions warn against. This absence is itself the point of contrast with Angular's `quantities.spec.ts` and `shared/test-vectors.json` - Blazor has nothing to port, so it has nothing to test here.
- **Decision:** single-file `.razor` components with an inline `@code` block by default; a `.razor.cs` code-behind partial only for the three screens with real data-loading lifecycle logic (List, Detail, Form) - mirrors Angular's own inline-template-vs-separate-file split rather than applying one rule uniformly.

### Snippet candidates

- `Form/RecipeValidation.cs` - the literal-key `ValidationMessageStore` that lets one validator serve both client rules and server errors with no path translation.
- `Form/FieldError.razor` - a ~20-line component resolving `data-testid="form-error-{key}"` directly from the literal error key.
- `Detail/RecipeDetail.razor` - the `@using static RecipeBox.Shared.Quantities` line and the `ScaledQuantity`/`FormatQuantity` call site: no `quantities.ts`, no port, same logic the API's own xunit tests already cover.
- `Detail/RecipeDetail.razor.cs`'s `OnParametersSetAsync` reset-on-route-change logic, worth showing directly alongside Angular's one-line `linkedSignal` equivalent.
- `clients/blazor/nginx.conf`'s `/_framework/` location block - the explicit `application/wasm` MIME type and `try_files $uri =404` fallback, with the reasoning for both in the comment.

## Phase 4: React

### Surprises and mistakes

- **Where Angular habits misled:** `fetch()` does not reject on 4xx/5xx the way Angular's `HttpClient` does - every response has to be checked with `response.ok` by hand, or a real server error silently "succeeds." Wrapped once in `http.ts`'s `request<T>()` so every call site can just `try`/`catch` like the other two clients do.
- **Where Angular habits misled:** a controlled `<input type="number">` always yields a *string* in `onChange`, never a parsed number - unlike Angular's `NumberValueAccessor`, which hides that conversion inside `formControlName`. Form state keeps every numeric field as a string (`formValues.ts`) and converts with `Number(...)` only at submit time; `Number('')` conveniently evaluates to `0`, matching the server's own "missing number defaults to 0" rule, so an emptied quantity field still fails the right validation rule instead of silently passing.
- **Where Angular habits misled:** the reflex to "reset state when a prop changes" is a documented React anti-pattern (react.dev's "You Might Not Need an Effect", checked live rather than assumed) - the fix for the servings scaler isn't a `useEffect` watching the recipe id, it's giving the stateful child component a `key={recipe.id}` so React remounts it fresh on navigation. Same `key` trick turned out to be load-bearing a second time: React Router reuses the same `RecipeForm` instance across `/recipes/new` and `/recipes/:id/edit`, so without `key={id ?? 'new'}` a previous edit's field values leaked straight into a subsequent "New recipe" click.
- **TanStack Query's default retry behavior (3 retries with backoff, several seconds) would have broken e2e timing** - test 4's forced-500 and test 14's 404 both need the error/not-found UI to appear well inside Playwright's default expect window, but a query that's still silently retrying never reaches its error state in time. Fixed with one `QueryClient` option, `retry: false`, which also brings React in line with Angular's `httpResource` and Blazor's `HttpClient`, neither of which retries either.
- **Surprise, not a bug:** oxlint (the create-vite scaffold's default linter, not ESLint) already ships `react/rules-of-hooks` as `error` out of the box - nothing extra to configure for the one lint rule that matters most to a React newcomer.
- **Confirmed before relying on it, not assumed:** declarative `<Routes>`/`<Route>` (not the data-router/`createBrowserRouter`+loaders API) was the deliberate choice, since loaders would be a second server-state mechanism running alongside TanStack Query, which CLAUDE.md's "TanStack Query, and nothing else for server state" rule rules out.

### Decisions

- **Decision:** the header count reads the TanStack Query cache directly (`recipeCount.ts`'s `useRecipeCount`, a two-line `useQuery` with a `select`) - no Context provider wraps it. The query cache already is a shared, subscribable store; a Context around it would be the redundant indirection CLAUDE.md's "no abstractions" rule warns about. It uses the exact same query key as the unfiltered recipe list, so on `/` with no filters the two dedupe into a single request automatically, and `queryClient.invalidateQueries({queryKey:['recipes']})` after create/delete is the entire "refresh" story.
- **Decision:** no form library (no React Hook Form, no Formik) - plain `useState` and controlled inputs throughout, matching CLAUDE.md's silence on a sanctioned React form library (unlike its explicit TanStack Query mandate for data) and keeping the comparison fair against Angular's reactive forms and Blazor's `EditContext`, neither of which pulls in a separate forms package either.
- **Decision:** server and client validation errors share **one flat dictionary shape**, `Record<string, string[]>` - exactly the server's own `ValidationProblem['errors']` type. Mapping a 400 response is literally `setServerErrors(err.errors)`: no bracket-to-dot-path regex like Angular's `apply-server-errors.ts`, and for a different reason than Blazor's `EditContext.Field(string)` trick - React needs no translation because there's no form model to resolve a path *into* in the first place, just a plain object indexed by the exact string the server sent. `FieldError`'s `data-testid="form-error-{key}"` reuses that same literal key a third time.
- **Decision:** `validateRecipe()` ports `RecipeValidator.cs`/`RecipeNormalizer.cs` directly from the C# source, not from Angular's existing TypeScript port - Angular's port is missing a per-ingredient-unit length check entirely, and flags *both* sides of a duplicate tag where the server (and this port) only flags the later occurrence. Both gaps are left as Phase 5 parity-pass flags for Angular, not silently fixed here.
- **Decision:** React takes spec.md's full image-upload path, same as Blazor and Angular before it - confirmed (not assumed) by reading `e2e/tests/image.spec.ts` directly: the shared suite has no reduced/fallback variant of test 12, so the full path was never optional for any client that has to pass this suite unedited.
- **Decision:** `queryOptions()` factories (`queries.ts`) instead of a custom data-fetching-hooks layer - this is TanStack v5's own documented idiom, not a homemade abstraction, and one definition per resource serves `useQuery`, `setQueryData`, and the header's shared-key trick all at once.

### Snippet candidates

- `src/quantities.ts` - the third hand-port of `scaledQuantity`/`formatQuantity`, textually close to Angular's, demonstrating the same 4-decimal-rounding-before-snapping step is load-bearing a third time (verified by temporarily removing it and watching the `0.29166` vector fail).
- `src/recipeCount.ts` - the entire header-count "shared state" solution in three lines; no service, no container, just the query cache.
- `src/recipe-form/FieldError.tsx` and `recipeRules.ts` - the literal-key error dictionary shared between client and server validation, no path translation at all.
- `src/recipe-detail/RecipeDetail.tsx` - the `key={recipe.id}` remount line, worth showing directly alongside Angular's `linkedSignal` and Blazor's `OnParametersSetAsync`.
- `src/recipe-form/RecipeForm.tsx` - the `key={id ?? 'new'}` line, the second place the same remount trick turned out to be load-bearing, this time for route-instance reuse rather than state reset.

## Cross-framework comparisons

Feature-by-feature notes for the side-by-side sections. Candidates for full side-by-side treatment:

### Derived state (servings scaler)

Three different answers to "reset local state when the user navigates to a different recipe," none of them a `useEffect`/lifecycle hack added after the fact:
- **Angular:** `linkedSignal(() => this.recipe.value()?.servings ?? 1)` - one declarative line, re-derives on its own.
- **Blazor:** `OnParametersSetAsync` compares the route id against the last-loaded id and explicitly resets state when it changes - no built-in primitive for this, so it's real, visible lifecycle code.
- **React:** `<RecipeDetailBody key={recipe.id} ... />` - the parent never manages the reset at all; keying the stateful child by id makes React remount (and so reinitialize) it for free.

### Repeating ingredients form and server error mapping

The same server dictionary (`{"ingredients[0].quantity": ["..."]}`) reaches the UI three different ways:
- **Angular:** a regex rewrites `ingredients[0].quantity` to `ingredients.0.quantity` and resolves it through `FormGroup#get` - translation is necessary because Angular's reactive forms model is a real nested object graph.
- **Blazor:** `EditContext.Field("ingredients[0].quantity")` accepts the literal string directly, no translation at all - the form model happens to not require one.
- **React:** the literal string is just an object key in a plain `Record<string, string[]>` - no translation either, but for a different reason than Blazor: there's no form model to resolve a path *into* in the first place.

### Fetching with loading, empty, and error states

All three use a resource/query primitive with its own `isLoading`/`isError` flags (`httpResource`, `useQuery`) rather than hand-rolled `useState` + manual fetch effects, so the loading-skeleton/error-with-retry states read almost identically across clients. Blazor is the exception: no equivalent built-in resource primitive, so its data-loading states are hand-written in each screen's code-behind.

### Shared model and validation (Blazor shared library vs. generated types and re-expressed rules)

Blazor references `shared/RecipeBox.Shared` directly - `Quantities`, `RecipeValidator`, and `RecipeNormalizer` are called, not ported, and have zero duplicate logic or duplicate tests. Angular and React each hand-port the same two functions and the same validation rules into TypeScript twice over, independently, against the same `shared/test-vectors.json` oracle - and the two ports already disagree with each other in minor ways (Angular's validation port has gaps React's doesn't - see Phase 4's Decisions). That divergence between two "faithful" ports of the same spec is itself worth a paragraph: duplication isn't just slower to write, it's a second place for drift to quietly creep in.

Lighter treatment or follow-up posts: routing, header summary (shared state - service vs. event container vs. query cache, already covered per-phase in each Decisions section above), image upload.

## Tailwind notes

Tailwind v4.3.3 throughout, CSS-first `@theme` config, one `theme.css` file copied byte-for-byte into each client (Angular is canonical; Blazor and React each hold their own copy). Angular and React both go through `@tailwindcss/postcss` via a `.postcssrc.json` Vite/Angular both already read natively; Blazor uses the standalone Tailwind CLI binary in its Dockerfile build stage instead, since it has no Node tooling at all. No client builds a Tailwind class name dynamically (a `bg-${color}-500`-style template literal) anywhere - the scanner can't see those, and all three use complete literal class strings or a ternary between two complete strings instead.

## Docker and Playwright notes

The same nginx `client_max_body_size 11m` fix was needed in all three client `nginx.conf` files (nginx's 1 MB default sits below the app's own 5 MB upload cap, so without raising it nginx itself 413s a request the API would have accepted) - caught once in Angular, then checked for and found proactively in both Blazor and React.

The one shared Playwright suite in `e2e/` passed against all three client containers with **zero suite edits**, confirmed by running it fresh against each of `angular`, `blazor`, and `react` in turn after React was built (16/16 every time). The suite is parameterized entirely by `BASE_URL` and selects purely by `data-testid`/role and route URL - nothing framework-specific ever leaked into it. The one true per-client variable is boot time: Blazor's WASM cold-boot is slow enough to occasionally race the `screenshots.spec.ts` navigation (see Phase 3's "documented, not fixed" entry); Angular and React both hydrate fast enough that this never showed up for them.

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
