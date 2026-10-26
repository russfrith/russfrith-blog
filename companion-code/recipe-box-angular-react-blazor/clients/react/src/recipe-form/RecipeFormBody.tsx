import { useState, type FocusEvent, type FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import { request, HttpError } from '../http';
import { recipeQuery } from '../queries';
import { validateRecipe } from './recipeRules';
import { toRecipeInput, type FormValues } from './formValues';
import { FieldError } from './FieldError';
import { IngredientRows } from './IngredientRows';
import { StepRows } from './StepRows';
import { TagInput } from './TagInput';
import type { Recipe } from '../api/types';

const COLLECTION_KEYS = new Set(['tags', 'ingredients', 'steps']);

export function RecipeFormBody({ id, initial }: { id?: number; initial: FormValues }) {
  const [values, setValues] = useState(initial);
  const [touched, setTouched] = useState(new Set<string>());
  const [submitted, setSubmitted] = useState(false);
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});
  const [file, setFile] = useState<File | null>(null);
  const [createdId, setCreatedId] = useState<number>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const recipeId = id ?? createdId;
  const save = useMutation({
    mutationFn: (body: ReturnType<typeof toRecipeInput>) =>
      recipeId
        ? request<Recipe>(`/api/recipes/${recipeId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
        : request<Recipe>('/api/recipes', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
  });
  const upload = useMutation({
    mutationFn: ({ recipeId, file }: { recipeId: number; file: File }) => {
      const body = new FormData();
      body.append('file', file);
      return request<Recipe>(`/api/recipes/${recipeId}/image`, { method: 'POST', body });
    },
  });
  const saving = save.isPending || upload.isPending;

  const clientErrors = validateRecipe(toRecipeInput(values));
  const visibleClientErrors = Object.fromEntries(
    Object.entries(clientErrors).filter(([key]) => submitted || touched.has(key) || (COLLECTION_KEYS.has(key) && touched.size > 0)),
  );
  const errors = { ...visibleClientErrors, ...serverErrors };

  function update(next: FormValues) {
    setValues(next);
    setServerErrors({});
  }

  function onBlur(e: FocusEvent<HTMLFormElement>) {
    const name = e.target.getAttribute('name');
    if (name) setTouched((t) => new Set(t).add(name));
  }

  function onFileSelected(selected: File | null) {
    if (selected && selected.size > 5 * 1024 * 1024) {
      setServerErrors({ file: ['File must be at most 5 MB.'] });
      setFile(null);
      return;
    }
    setServerErrors((e) => ({ ...e, file: [] }));
    setFile(selected);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitted(true);
    if (saving || Object.keys(clientErrors).length > 0) return;

    try {
      const saved = await save.mutateAsync(toRecipeInput(values));
      setCreatedId(saved.id);
      queryClient.setQueryData(recipeQuery(saved.id).queryKey, saved);
      await queryClient.invalidateQueries({ queryKey: ['recipes'] });

      if (file) {
        const withImage = await upload.mutateAsync({ recipeId: saved.id, file });
        queryClient.setQueryData(recipeQuery(saved.id).queryKey, withImage);
      }

      navigate(`/recipes/${saved.id}`);
    } catch (err) {
      if (err instanceof HttpError && err.errors) setServerErrors(err.errors);
    }
  }

  return (
    <form onSubmit={handleSubmit} onBlur={onBlur} className="space-y-6">
      <div>
        <label className="block text-sm font-medium">Title</label>
        <input
          name="title"
          value={values.title}
          onChange={(e) => update({ ...values, title: e.target.value })}
          data-testid="form-title"
          className="w-full rounded-[var(--radius-control)] border border-[var(--color-border)] px-3 py-2"
        />
        <FieldError fieldKey="title" errors={errors} />
      </div>

      <div>
        <label className="block text-sm font-medium">Description</label>
        <textarea
          name="description"
          value={values.description}
          onChange={(e) => update({ ...values, description: e.target.value })}
          data-testid="form-description"
          className="w-full rounded-[var(--radius-control)] border border-[var(--color-border)] px-3 py-2"
        />
        <FieldError fieldKey="description" errors={errors} />
      </div>

      <div className="flex gap-4">
        <div>
          <label className="block text-sm font-medium">Servings</label>
          <input
            type="number"
            name="servings"
            value={values.servings}
            onChange={(e) => update({ ...values, servings: e.target.value })}
            data-testid="form-servings"
            className="w-24 rounded-[var(--radius-control)] border border-[var(--color-border)] px-3 py-2"
          />
          <FieldError fieldKey="servings" errors={errors} />
        </div>
        <div>
          <label className="block text-sm font-medium">Prep (min)</label>
          <input
            type="number"
            name="prepMinutes"
            value={values.prepMinutes}
            onChange={(e) => update({ ...values, prepMinutes: e.target.value })}
            data-testid="form-prep"
            className="w-24 rounded-[var(--radius-control)] border border-[var(--color-border)] px-3 py-2"
          />
          <FieldError fieldKey="prepMinutes" errors={errors} />
        </div>
        <div>
          <label className="block text-sm font-medium">Cook (min)</label>
          <input
            type="number"
            name="cookMinutes"
            value={values.cookMinutes}
            onChange={(e) => update({ ...values, cookMinutes: e.target.value })}
            data-testid="form-cook"
            className="w-24 rounded-[var(--radius-control)] border border-[var(--color-border)] px-3 py-2"
          />
          <FieldError fieldKey="cookMinutes" errors={errors} />
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium">Tags</label>
        <TagInput tags={values.tags} onChange={(tags) => update({ ...values, tags })} errors={errors} />
      </div>

      <div>
        <label className="block text-sm font-medium">Hero image</label>
        <input
          type="file"
          accept="image/jpeg,image/png,image/webp"
          data-testid="form-image-input"
          onChange={(e) => onFileSelected(e.target.files?.[0] ?? null)}
        />
        <FieldError fieldKey="file" errors={errors} />
      </div>

      <div>
        <h2 className="font-serif text-lg">Ingredients</h2>
        <IngredientRows items={values.ingredients} onChange={(ingredients) => update({ ...values, ingredients })} errors={errors} />
      </div>

      <div>
        <h2 className="font-serif text-lg">Steps</h2>
        <StepRows items={values.steps} onChange={(steps) => update({ ...values, steps })} errors={errors} />
      </div>

      <button type="submit" data-testid="form-submit" disabled={saving} className="rounded-[var(--radius-control)] bg-[var(--color-accent)] px-5 py-2 text-white">
        {saving ? 'Saving...' : 'Save recipe'}
      </button>
    </form>
  );
}
