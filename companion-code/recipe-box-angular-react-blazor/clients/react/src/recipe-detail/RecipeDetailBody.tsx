import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router';
import { request } from '../http';
import { formatQuantity, scaledQuantity } from '../quantities';
import { ServingsStepper } from './ServingsStepper';
import type { Recipe } from '../api/types';

// Keyed by recipe.id from the parent (RecipeDetail) - React has no
// linkedSignal/explicit-lifecycle equivalent, so remounting on a new id is
// what resets `servings` and `confirming` to this recipe's own defaults.
export function RecipeDetailBody({ recipe }: { recipe: Recipe }) {
  const [servings, setServings] = useState(recipe.servings);
  const [confirming, setConfirming] = useState(false);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const deleteRecipe = useMutation({
    mutationFn: () => request(`/api/recipes/${recipe.id}`, { method: 'DELETE' }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['recipes'] });
      navigate('/');
    },
  });

  return (
    <>
      {recipe.imageUrl && (
        <img data-testid="detail-image" src={recipe.imageUrl} alt="" className="mb-6 h-64 w-full rounded-[var(--radius-card)] object-cover" />
      )}
      <h1 data-testid="detail-title" className="font-serif text-3xl">
        {recipe.title}
      </h1>
      <p className="mt-2 text-[var(--color-ink-muted)]">{recipe.description}</p>
      <p className="mt-1 text-sm text-[var(--color-ink-muted)]">{recipe.tags.join(', ')}</p>
      <p className="mt-1 text-sm text-[var(--color-ink-muted)]">
        Prep {recipe.prepMinutes} min / Cook {recipe.cookMinutes} min
      </p>

      <div className="mt-6">
        <ServingsStepper servings={servings} onServingsChange={setServings} />
      </div>

      <ul className="mt-4 space-y-1">
        {recipe.ingredients.map((ingredient, index) => (
          <li key={index}>
            <span data-testid={`ingredient-quantity-${index}`}>{formatQuantity(scaledQuantity(ingredient.quantity, recipe.servings, servings))}</span>{' '}
            {ingredient.unit && `${ingredient.unit} `}
            <span data-testid={`ingredient-name-${index}`}>{ingredient.name}</span>
          </li>
        ))}
      </ul>

      <ol className="mt-4 list-decimal space-y-2 pl-5">
        {recipe.steps.map((step, index) => (
          <li key={index} data-testid={`step-${index}`}>
            {step.instruction}
          </li>
        ))}
      </ol>

      <div className="mt-6 flex gap-3">
        <Link to={`/recipes/${recipe.id}/edit`} data-testid="edit-button" className="rounded-[var(--radius-control)] border px-4 py-2">
          Edit
        </Link>
        {!confirming ? (
          <button data-testid="delete-button" onClick={() => setConfirming(true)} className="rounded-[var(--radius-control)] border px-4 py-2">
            Delete
          </button>
        ) : (
          <button
            data-testid="delete-confirm-button"
            onClick={() => deleteRecipe.mutate()}
            className="rounded-[var(--radius-control)] bg-[var(--color-danger)] px-4 py-2 text-white"
          >
            Confirm delete
          </button>
        )}
      </div>
    </>
  );
}
