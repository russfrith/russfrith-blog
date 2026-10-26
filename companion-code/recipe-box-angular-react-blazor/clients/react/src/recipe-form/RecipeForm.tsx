import { useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router';
import { recipeQuery } from '../queries';
import { fromRecipe, newRecipeValues } from './formValues';
import { RecipeFormBody } from './RecipeFormBody';

export function RecipeForm() {
  const params = useParams();
  const id = params.id ? Number(params.id) : undefined;
  const existing = useQuery({ ...recipeQuery(id ?? -1), enabled: id !== undefined });

  if (id !== undefined && existing.isLoading) {
    return <div className="h-96 animate-pulse rounded-[var(--radius-card)] bg-[var(--color-border)]" />;
  }

  // Keyed by id (or 'new') - React Router reuses the same RecipeForm
  // instance across /recipes/new <-> /recipes/:id/edit, so without this key
  // a previous edit's field values would leak into a subsequent new-recipe
  // form.
  return <RecipeFormBody key={id ?? 'new'} id={id} initial={existing.data ? fromRecipe(existing.data) : newRecipeValues()} />;
}
