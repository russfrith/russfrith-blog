// Type-level contract check against the generated schema, compiled with
// `tsc --noEmit`. Run `./generate.sh schema.d.ts` first (schema.d.ts is
// generated, not committed). Phase 1 only has the API, so this checks the
// shapes spec.md's "Models" and "Validation errors" sections promise;
// Phase 2/4 add client-specific checks alongside their own generated path.
import type { components } from "./schema";

type Recipe = components["schemas"]["Recipe"];
type Ingredient = components["schemas"]["Ingredient"];
type Step = components["schemas"]["Step"];
type RecipeSummary = components["schemas"]["RecipeSummary"];
type TagCount = components["schemas"]["TagCount"];
type ValidationProblem = components["schemas"]["HttpValidationProblemDetails"];

// imageUrl is nullable, never widened to include anything else.
const imageUrl: Recipe["imageUrl"] = null;
const imageUrlSet: Recipe["imageUrl"] = "/api/images/x.jpg";

// Numeric fields stay `number`, never widened to `number | string` (see
// spec.md's note on Strict JSON number handling).
const servings: Recipe["servings"] = 4;
const quantity: Ingredient["quantity"] = 0.3333;
const order: Step["order"] = 1;

// Validation errors: a field path to a list of messages, no `$.` prefix
// baked into the type (it's just `string`, the grammar is a runtime rule).
const errors: ValidationProblem["errors"] = {
  title: ["Title must be 3 to 100 characters."],
  "ingredients[0].quantity": ["Quantity must be greater than 0 and at most 10000."],
};

// Summaries carry no ingredients/steps.
const summary: RecipeSummary = {
  id: 1,
  title: "Lemon Pancakes",
  description: null,
  tags: ["breakfast"],
  servings: 3,
  prepMinutes: 10,
  cookMinutes: 15,
  imageUrl: imageUrlSet,
};

const tagCount: TagCount = { name: "breakfast", count: 3 };

void imageUrl;
void servings;
void quantity;
void order;
void errors;
void summary;
void tagCount;
