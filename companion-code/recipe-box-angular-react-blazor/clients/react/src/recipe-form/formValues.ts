import type { Recipe, RecipeInput } from '../api/types';

// Numeric fields are held as strings - a controlled <input type="number">
// always yields a string, and controlling it as a parsed number breaks
// partial input ("0.", an empty field).
export interface IngredientValues {
  quantity: string;
  unit: string;
  name: string;
}

export interface FormValues {
  title: string;
  description: string;
  servings: string;
  prepMinutes: string;
  cookMinutes: string;
  tags: string[];
  ingredients: IngredientValues[];
  steps: string[];
}

export function newRecipeValues(): FormValues {
  return {
    title: '',
    description: '',
    servings: '4',
    prepMinutes: '0',
    cookMinutes: '0',
    tags: [],
    ingredients: [{ quantity: '0', unit: '', name: '' }],
    steps: [''],
  };
}

export function fromRecipe(recipe: Recipe): FormValues {
  return {
    title: recipe.title,
    description: recipe.description ?? '',
    servings: String(recipe.servings),
    prepMinutes: String(recipe.prepMinutes),
    cookMinutes: String(recipe.cookMinutes),
    tags: recipe.tags,
    ingredients: recipe.ingredients.map((i) => ({ quantity: String(i.quantity), unit: i.unit ?? '', name: i.name })),
    steps: recipe.steps.map((s) => s.instruction),
  };
}

// Number('') is 0, matching the server's "missing number defaults to 0"
// semantics - an empty quantity field correctly fails the >0 rule.
export function toRecipeInput(values: FormValues): RecipeInput {
  return {
    title: values.title,
    description: values.description || null,
    servings: Number(values.servings),
    prepMinutes: Number(values.prepMinutes),
    cookMinutes: Number(values.cookMinutes),
    tags: values.tags,
    ingredients: values.ingredients.map((i) => ({ quantity: Number(i.quantity), unit: i.unit || null, name: i.name })),
    steps: values.steps.map((instruction) => ({ instruction })),
  };
}
