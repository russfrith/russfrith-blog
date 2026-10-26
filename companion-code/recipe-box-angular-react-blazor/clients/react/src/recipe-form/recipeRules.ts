import type { RecipeInput } from '../api/types';

// Hand-ports shared/RecipeBox.Shared/RecipeNormalizer.cs and
// RecipeValidator.cs - same rules, same exact message strings, so client
// and server errors read identically. Title uniqueness is server-only
// (needs the database) and never appears here.
function normalize(input: RecipeInput): RecipeInput {
  return {
    ...input,
    title: input.title.trim(),
    description: input.description?.trim() || null,
    tags: input.tags.map((t) => t.trim().toLowerCase()),
    ingredients: input.ingredients.map((i) => ({ ...i, name: i.name.trim(), unit: i.unit?.trim() || null })),
    steps: input.steps.map((s) => ({ instruction: s.instruction.trim() })),
  };
}

export function validateRecipe(input: RecipeInput): Record<string, string[]> {
  const recipe = normalize(input);
  const errors: Record<string, string[]> = {};
  const fail = (key: string, message: string) => (errors[key] ??= []).push(message);

  if (recipe.title.length < 3 || recipe.title.length > 100) fail('title', 'Title must be 3 to 100 characters.');
  if (recipe.description !== null && recipe.description.length > 500) fail('description', 'Description must be at most 500 characters.');
  if (recipe.servings < 1 || recipe.servings > 48) fail('servings', 'Servings must be between 1 and 48.');
  if (recipe.prepMinutes < 0 || recipe.prepMinutes > 1440) fail('prepMinutes', 'PrepMinutes must be between 0 and 1440.');
  if (recipe.cookMinutes < 0 || recipe.cookMinutes > 1440) fail('cookMinutes', 'CookMinutes must be between 0 and 1440.');

  if (recipe.tags.length > 8) fail('tags', 'At most 8 tags are allowed.');
  const seenTags = new Set<string>();
  recipe.tags.forEach((tag, i) => {
    if (tag.length < 1 || tag.length > 30) fail(`tags[${i}]`, 'Each tag must be 1 to 30 characters.');
    else if (seenTags.has(tag)) fail(`tags[${i}]`, 'Tags must be unique.');
    else seenTags.add(tag);
  });

  if (recipe.ingredients.length < 1 || recipe.ingredients.length > 50)
    fail('ingredients', 'At least 1 and at most 50 ingredients are required.');
  recipe.ingredients.forEach((ingredient, i) => {
    if (ingredient.quantity <= 0 || ingredient.quantity > 10000)
      fail(`ingredients[${i}].quantity`, 'Quantity must be greater than 0 and at most 10000.');
    if (ingredient.unit !== null && ingredient.unit.length > 20) fail(`ingredients[${i}].unit`, 'Unit must be at most 20 characters.');
    if (ingredient.name.length < 1 || ingredient.name.length > 100) fail(`ingredients[${i}].name`, 'Name must be 1 to 100 characters.');
  });

  if (recipe.steps.length < 1 || recipe.steps.length > 50) fail('steps', 'At least 1 and at most 50 steps are required.');
  recipe.steps.forEach((step, i) => {
    if (step.instruction.length < 1 || step.instruction.length > 1000)
      fail(`steps[${i}].instruction`, 'Instruction must be 1 to 1000 characters.');
  });

  return errors;
}
