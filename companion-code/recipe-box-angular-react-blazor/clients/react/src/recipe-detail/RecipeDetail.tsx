import { useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router';
import { recipeQuery } from '../queries';
import { HttpError } from '../http';
import { RecipeDetailBody } from './RecipeDetailBody';

export function RecipeDetail() {
  const id = Number(useParams().id);
  const recipe = useQuery(recipeQuery(id));

  if (recipe.isLoading) {
    return <div className="h-96 animate-pulse rounded-[var(--radius-card)] bg-[var(--color-border)]" />;
  }

  if (recipe.isError) {
    if (recipe.error instanceof HttpError && recipe.error.status === 404) {
      return <p data-testid="detail-not-found" className="py-12 text-center text-[var(--color-ink-muted)]">Recipe not found.</p>;
    }
    return (
      <div data-testid="detail-error" className="rounded-[var(--radius-card)] bg-[var(--color-danger-bg)] p-6 text-center">
        <p className="mb-4 text-[var(--color-danger)]">Couldn't load this recipe.</p>
        <button onClick={() => recipe.refetch()} className="rounded-[var(--radius-control)] border px-4 py-2">
          Retry
        </button>
      </div>
    );
  }

  if (!recipe.data) return null;

  // Keyed by id - React Router reuses the same component instance across
  // different :id values, so the key forces RecipeDetailBody to remount
  // (and reinitialize its local `servings` state) for each new recipe.
  return <RecipeDetailBody key={recipe.data.id} recipe={recipe.data} />;
}
