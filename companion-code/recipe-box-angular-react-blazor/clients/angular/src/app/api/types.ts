// Thin aliases over the generated schema (api/schema.d.ts, committed,
// regenerated with tools/typegen). Kept separate so every other file
// imports from here, not from the generated file directly.
import type { components } from './schema';

export type Recipe = components['schemas']['Recipe'];
export type Ingredient = components['schemas']['Ingredient'];
export type Step = components['schemas']['Step'];
export type RecipeSummary = components['schemas']['RecipeSummary'];
export type TagCount = components['schemas']['TagCount'];
export type ValidationProblem = components['schemas']['HttpValidationProblemDetails'];

// The API has no documented request body for POST/PUT /api/recipes (it
// parses the body by hand - see api/RecipeBox.Api/Endpoints/JsonBody.cs).
// This is the shape the client actually sends: no id/imageUrl/timestamps
// (server-assigned), and steps carry only the instruction - order is
// array position, never sent by the client.
export type RecipeInput = Omit<Recipe, 'id' | 'imageUrl' | 'createdAt' | 'updatedAt' | 'steps'> & {
  steps: Pick<Step, 'instruction'>[];
};
