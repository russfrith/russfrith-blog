import { Link } from 'react-router';
import type { RecipeSummary } from '../api/types';

export function RecipeCard({ recipe }: { recipe: RecipeSummary }) {
  const totalMinutes = recipe.prepMinutes + recipe.cookMinutes;

  return (
    <Link
      to={`/recipes/${recipe.id}`}
      data-testid={`recipe-card-${recipe.id}`}
      className="block overflow-hidden rounded-[var(--radius-card)] border border-[var(--color-border)] bg-white shadow-[var(--shadow-card)]"
    >
      {recipe.imageUrl ? (
        <img src={recipe.imageUrl} alt="" className="h-40 w-full object-cover" />
      ) : (
        <div className="flex h-40 w-full items-center justify-center bg-[var(--color-border)] text-[var(--color-ink-muted)]">
          No photo
        </div>
      )}
      <div className="p-4">
        <h2 data-testid={`recipe-card-title-${recipe.id}`} className="font-serif text-lg">
          {recipe.title}
        </h2>
        <p className="text-sm text-[var(--color-ink-muted)]">{recipe.tags.join(', ')}</p>
        <p className="text-sm text-[var(--color-ink-muted)]">{totalMinutes} min</p>
      </div>
    </Link>
  );
}
