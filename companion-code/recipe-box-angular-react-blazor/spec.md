# Recipe Box: Spec

This is the contract all three clients and the API are built against. It is a hypothesis: when something is ambiguous or wrong, flag it in the plan and fix it here as a visible edit.

## Overview

A personal recipe box. Browse and search recipes, view one with a servings scaler, and create, edit, and delete recipes with a hero image. One API, three identical frontends.

## Models

All models live in `shared/` (C#) and are mirrored as generated TypeScript for Angular and React.

All numeric fields below (`Servings`, `PrepMinutes`, `CookMinutes`,
`Quantity`) are **non-nullable** in both the shared C# model and the
generated TypeScript types. A well-behaved client always sends them. Two
different malformed-input cases behave differently:

- An **explicit `null`** for a numeric field, or a non-numeric value (a
  string, an object, an array) in its place, is a JSON parse error: 400
  keyed by the JSON path of that field (see Validation errors, below).
- A **missing numeric property** (the key absent entirely) deserializes to
  its default, `0`, and goes through ordinary field-level validation from
  there: `Servings: 0` and `Quantity: 0` both fail their range rules with a
  normal field-level 400, while `PrepMinutes: 0` and `CookMinutes: 0` are
  valid (0 is an allowed value for both).

The model is not widened to `int?`/`decimal?` to accommodate either case.

### Recipe

| Field | Type | Rules |
|---|---|---|
| Id | int | Server-assigned |
| Title | string | Required, 3 to 100 characters. Unique, case-insensitive, excluding the recipe being updated (a PUT that keeps its own title must not 400 against itself) |
| Description | string | Optional, max 500 characters |
| Servings | int | Required, 1 to 48 |
| PrepMinutes | int | 0 to 1440 |
| CookMinutes | int | 0 to 1440 |
| Tags | string[] | Max 8 tags, each 1 to 30 characters. Normalized to trimmed lowercase on save; uniqueness and the `tag` filter operate on the normalized form |
| Ingredients | Ingredient[] | At least 1, max 50. Order is array position; there is no Order field |
| Steps | Step[] | At least 1, max 50 |
| ImageUrl | string? | Nullable. Root-relative, under `/api/images/...` (not a bare `/images/...` path — clients only proxy `/api`). Set only by the image endpoint, never by client JSON |
| CreatedAt | DateTime (UTC) | Server-assigned, ISO 8601 with a trailing `Z` |
| UpdatedAt | DateTime (UTC) | Server-assigned, ISO 8601 with a trailing `Z` |

### Ingredient

| Field | Type | Rules |
|---|---|---|
| Quantity | decimal | Greater than 0, max 10000 |
| Unit | string | Optional, max 20 characters (for example "cup", "g", "tbsp") |
| Name | string | Required, 1 to 100 characters |

### Step

| Field | Type | Rules |
|---|---|---|
| Order | int | 1-based, contiguous. Server-assigned from array position; any Order value in the request body is ignored and overwritten. Test IDs and error-path indexes elsewhere are 0-based — only this field is 1-based |
| Instruction | string | Required, 1 to 1000 characters |

## Endpoints

| Method | Path | Behavior |
|---|---|---|
| GET | `/api/recipes?search=&tag=` | List, sorted by Title ascending. `search` matches title and ingredient names (case-insensitive); blank or omitted `search` is ignored. `tag` filters by exact (normalized) tag, single-select; an unknown tag returns an empty list, not 404. `search` and `tag` combine with AND. Returns summaries: Id, Title, Description, Tags, Servings, PrepMinutes, CookMinutes, ImageUrl. There is no separate count endpoint: the header's "N recipes" is the length of an unfiltered `GET /api/recipes` response. When filters are active, clients make a separate, unfiltered request for the header count, and refresh it after create and delete |
| GET | `/api/recipes/{id}` | Full recipe. 404 if missing |
| POST | `/api/recipes` | Create. 201 with the created recipe and Location header |
| PUT | `/api/recipes/{id}` | Replace. Full replace: the route id is authoritative (any `id` in the body is ignored), `Ingredients`/`Steps`/`Tags` are replaced wholesale (a null `Tags` is treated as an empty list; a null `Ingredients` or `Steps` is a validation error), `ImageUrl` and `CreatedAt` are preserved from the existing row, `UpdatedAt` is set to now. Last write wins; there is no ETag/concurrency check. 200 with the updated recipe. 404 if missing |
| DELETE | `/api/recipes/{id}` | 204. Also removes the image file |
| POST | `/api/recipes/{id}/image` | Multipart upload, field name `file`. 404 if the recipe doesn't exist (checked before reading the upload). Returns the updated recipe |
| DELETE | `/api/recipes/{id}/image` | 404 if the recipe doesn't exist. Otherwise removes the image and returns 204, including when there is no image to remove (idempotent) |
| GET | `/api/tags` | All tags with counts, sorted by name. Shape: `[{ "name": string, "count": int }]` |
| POST | `/api/test/reset` | Only mapped when `ASPNETCORE_ENVIRONMENT=Testing` (404 otherwise, including in the default `docker compose up`, which runs as Production). Clears and reseeds the database (recipe ids reset to a deterministic 1..N) and images (seed images are re-copied from the baked-in image, not the volume; the live image directory is emptied first) |

## Validation errors

Failures return 400 with `application/problem+json` (ProblemDetails with an `errors` dictionary). Grammar:

- Keys use camelCase JSON field names and 0-based collection indexes, for example `title`, `ingredients[2].quantity`, `steps[0].instruction`.
- A collection-level rule (too many tags, too many/too few ingredients or steps) is keyed by the collection name alone: `tags`, `ingredients`, `steps`.
- Malformed or mistyped JSON (a field of the wrong type, unparseable body) returns the same `application/problem+json` shape, keyed by the JSON path of the offending token in the same `field[index].field` grammar as above, with no `$.` or other root prefix (for example `ingredients[2].quantity` if that field isn't a number, not `$.ingredients[2].quantity`). A missing/unparseable body entirely is keyed `""`.
- Image validation errors are keyed `file`.
- Every validation rule has an explicit message string; message text isn't left to each implementation to invent, since all three clients map and display it.

Each client maps these onto the form fields, and the mapping appears in the post.

## Image rules

- Allowed types: JPEG, PNG, WebP. Max 5 MB (over-size requests are rejected; exactly 5 MB is accepted).
- Verify the file signature (magic bytes), not just the extension or content type. A wrong type, undersized/corrupt, or missing file, or a file over 5 MB but still under Kestrel's request body limit, all return 400 keyed `file`. Kestrel's request body size limit is set comfortably above 5 MB plus multipart overhead (so a 5 MB file never gets caught by the transport limit instead of the application's own check); a request that exceeds that transport limit returns 413.
- Store on disk in a configurable folder as `{guid}.{ext}` — this naming rule applies to uploads only. Seed images are pre-seeded placeholder images (not downloaded from the web) with fixed, committed filenames, so screenshots and the e2e suite stay deterministic and real photos can later be swapped in with no code changes. Never use the uploaded filename for an upload. Store the relative path in SQLite and serve through ASP.NET static files under `/api/images/...` (not a bare `/images/...` path, since clients only proxy `/api`).
- Replacing an image deletes the old file.
- No resize, crop, drag-and-drop, or preview-before-upload in scope. No thumbnails either — out of scope entirely, not just deferred.
- Fallback if the upload UI becomes unwieldy in a framework: ship seeded recipes with images and drop the upload form there. Record the decision in `blog-notes.md`, and say so plainly in the post. A client that takes this fallback runs the reduced form of end-to-end test 12 (see End-to-end tests).

## Shared logic (pure, unit tested)

Implemented in C# in `shared/`, ported by hand to TypeScript.

- `scaledQuantity(quantity, originalServings, targetServings)` returns `quantity * target / original`. C# computes this in `decimal`; TypeScript in `double` — the only difference between the two implementations is that number type.
- `formatQuantity(value)` formats to the nearest sensible fraction for cooking. **The algorithm is identical in C# and TypeScript:**
  1. **Round the input value to 4 decimal places first**, in both implementations (C# `decimal`, TypeScript `double`), before any snapping. This keeps floating-point/repeating-decimal noise in `scaledQuantity`'s output from affecting which fraction it snaps to, and makes the two implementations deterministically agree. Quantities are always positive, and both implementations round halves up (away from zero): C# uses `Math.Round(value, 4, MidpointRounding.AwayFromZero)`, and TypeScript uses `Math.round(value * 10000) / 10000`. The test vectors avoid constructing inputs that land on an exact fifth-decimal-place midpoint, since `double` can't represent most such midpoints reliably.
  2. For values under 10: snap the rounded value's fractional part to the nearest of `0, 1/8, 1/4, 1/3, 3/8, 1/2, 5/8, 2/3, 3/4, 7/8, 1` (carrying into the whole number when it snaps to `1`). A result that would show "0" shows `1/8` instead (the 1/8 floor applies only to this under-10 case).
  3. For values 10 and over: round to the nearest 1/2 instead (for example 10.75 shows "11").
  4. **Ties:** a tie is the exact midpoint between two adjacent allowed fractions, and a tie rounds up to the larger of the two. Because non-terminating fractions in the set (1/3, 2/3) have a repeating-decimal midpoint with their neighbors, **a tie can only actually occur between two fractions whose midpoint is itself a finite (terminating) decimal** — that's `0`/`1/8`, `1/8`/`1/4`, `3/8`/`1/2`, `1/2`/`5/8`, `3/4`/`7/8`, and `7/8`/`1`. (For example, 1/8 and 1/4 have the finite midpoint 0.1875, so 0.1875 is a real tie and shows "1/4". 1/4 and 1/3's midpoint is 0.291666..., which never occurs as an exact value after rounding to 4 decimal places, so there's no tie there — only a nearest-neighbor comparison.)
  5. Show whole numbers without a fraction, and mixed numbers as `1 1/2`.
  - Document any further rule change here; see `blog-notes.md` for the history of this rule.
- Quantity precision: decimal storage only. There's no "1/3"-style text parsing on input — a third is entered and stored as a decimal (seed data uses 0.3333).
- **Shared test vectors:** `shared/test-vectors.json` holds input/expected-output pairs for both `scaledQuantity` and `formatQuantity`, covering:
  - The real tie case: `0.1875` → `"1/4"` (finite midpoint of 1/8 and 1/4; ties round up).
  - The rounding step, near the non-finite 1/4–1/3 boundary: `0.29166` and `0.29167` both round to `0.2917` at 4 decimal places and both snap to `"1/3"` — demonstrating that the rounding step runs before snapping and that both implementations agree despite 1/4–1/3 having no finite tie point.
  - The identity case: `0.3333` scaled by a factor of 1 (originalServings 1, targetServings 1) → `"1/3"` — confirms scaling by 1 is a no-op and the baseline fraction itself formats correctly, independent of any scaling arithmetic.
  - `0.3333` scaled by a factor of 3 (for example originalServings 1, targetServings 3) → `"1"`
  - `0.3333` scaled by a factor of 2 (for example originalServings 1, targetServings 2) → `"2/3"`
  The scaled-by-3 case is used as the servings-scaler case in end-to-end test 6, replacing the previous bare "1/3 case" description. The C# tests run this file now (Phase 1); the Angular and React ports must run the same file once those clients exist, and must match every vector exactly.
- Unit conversion is out of scope.

## Screens

### 1. Recipe list (`/`)

- Header: app name, summary ("N recipes"), and a "New recipe" button.
- Search box (debounced, 250 ms) and tag filter chips. Both are reflected in the URL query string.
- Grid of `RecipeCard`s: hero image (`object-cover`, placeholder if none), title, tags, total time.
- States: loading (skeleton cards), empty with no recipes ("No recipes yet" and a create button), empty with no matches ("No matches" and a clear-filters button), and error (message and retry button).

### 2. Recipe detail (`/recipes/:id`)

- Hero image, title, description, tags, prep and cook time.
- **Servings scaler:** stepper with minus and plus buttons, from 1 to 48. Changing it recalculates every ingredient quantity with `scaledQuantity` and `formatQuantity`. Local state only, with no server call. Resets when navigating to a different recipe.
- Ingredients list, numbered steps.
- Edit and delete buttons. Delete asks for confirmation.
- States: loading skeleton, 404 ("Recipe not found"), error with retry.

### 3. Create and edit form (`/recipes/new`, `/recipes/:id/edit`)

- Fields: title, description, servings, prep and cook minutes, tags (type and press Enter to add, remove with a button), hero image upload.
- **Ingredients section:** repeating rows (quantity, unit, name). Add row, remove row (keep at least one).
- **Steps section:** repeating, reorderable with up and down buttons (no drag and drop). Add and remove.
- Client-side validation mirrors the model rules. Inline errors appear on blur and on submit.
- Server validation errors (ProblemDetails) are mapped onto the matching fields.
- Submit: save the recipe as JSON, then upload the image if one was chosen. Show a saving state and prevent double submit. On success, navigate to the detail page.
- Edit loads the existing recipe. Unsaved changes are not guarded in core scope.

### 4. Header summary

The header shows the live recipe count. It updates after create and delete without a full page reload. This is the shared-state demonstration, using each framework's idiomatic mechanism (an Angular signal service, React context or the query cache, a Blazor state container).

## Test IDs

Identical in all three clients.

**List:** `header-count`, `new-recipe-button`, `search-input`, `tag-chip-{tag}`, `clear-filters-button`, `recipe-card-{id}`, `recipe-card-title-{id}`, `list-skeleton`, `list-empty`, `list-no-matches`, `list-error`, `list-retry-button`

**Detail:** `detail-title`, `detail-image`, `servings-value`, `servings-minus`, `servings-plus`, `ingredient-quantity-{index}`, `ingredient-name-{index}`, `step-{index}`, `edit-button`, `delete-button`, `delete-confirm-button`, `detail-not-found`, `detail-error`

**Form:** `form-title`, `form-description`, `form-servings`, `form-prep`, `form-cook`, `form-tag-input`, `form-tag-{tag}`, `form-image-input`, `ingredient-row-{index}`, `ingredient-quantity-input-{index}`, `ingredient-unit-input-{index}`, `ingredient-name-input-{index}`, `ingredient-remove-{index}`, `add-ingredient-button`, `step-row-{index}`, `step-input-{index}`, `step-up-{index}`, `step-down-{index}`, `step-remove-{index}`, `add-step-button`, `form-submit`, `form-error-{field}`

## Seed data

About 10 realistic recipes with varied tags, ingredient counts, and quantities that exercise fractions when scaled (for example 1/3 cup, 1 1/2 tsp). **At least one seed recipe must have `Servings: 3` and an ingredient with `Quantity: 0.3333` ("1/3 cup")** — this is the exact recipe/ingredient end-to-end test 6 scales from 3 to 9 servings to see `"1"` (the `shared/test-vectors.json` scaled-by-3 case, applied to real seed data rather than a unit test in isolation). Data lives in `seed/recipes.json` (language-neutral, so the e2e suite can import the same expected values later). Each recipe has a small hero image committed in `seed/`, generated as a placeholder (not downloaded) with a fixed filename, at least one each of JPEG, PNG, and WebP across the set. Include one recipe with no image to cover the placeholder. Seed data is deterministic so the tests can rely on it; reseeding (via the test-only reset endpoint) always restores the same ids, rows, and image files.

## End-to-end tests (`e2e/`)

One suite, 10 to 15 tests, run against each client through `BASE_URL`:

1. List shows seeded recipes and the header count.
2. Search filters the list, and a no-match query shows the empty state.
3. Tag chip filters the list and the URL reflects it.
4. Loading skeleton appears (throttled response) and the error state appears with retry (forced failure).
5. Detail shows the recipe.
6. Servings scaler recalculates quantities and formats fractions (including the `shared/test-vectors.json` 0.3333-scaled-by-3 case, which shows "1").
7. Create a recipe with two ingredients and two steps, and the header count increments.
8. Create form shows client-side errors for invalid input.
9. Server validation error maps to the right field (a rule enforced only by the server: case-insensitive unique Title, excluding the recipe being edited).
10. Edit a recipe, and the change persists.
11. Reorder and remove steps.
12. Upload a hero image and see it on the detail page. Reject a wrong file type. **Reduced form** (for a client that took the upload-form fallback): the seeded image displays correctly on the detail page, and wrong-file-type rejection is verified at the API level instead of through that client's UI. Which client (if any) runs the reduced form is recorded in `blog-notes.md` and stated plainly in the post.
13. Delete a recipe with confirmation, and the header count decrements.
14. Detail page for a missing id shows not found.

The suite also captures matching screenshots of the list, detail, and form screens for each client, for use in the post.

## Out of scope (for now)

Authentication, multi-user data, step images, drag and drop, image cropping, unit conversion, pagination, offline support, internationalization. Auth is a planned follow-up post, built as a visible diff on top of this baseline.

## Notes

SQLite is a deliberate choice for this single-instance demo app, not an oversight — it isn't meant to scale beyond one API replica, and the README should say so. Don't read "SQLite" in the stack as something to fix.
