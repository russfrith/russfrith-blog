import { useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router';
import { recipesQuery } from '../queries';
import { RecipeCard } from '../recipe-card/RecipeCard';
import { TagFilter } from './TagFilter';

export function RecipeList() {
  const [searchParams, setSearchParams] = useSearchParams();
  const search = searchParams.get('search') ?? '';
  const tag = searchParams.get('tag') ?? '';

  const [searchText, setSearchText] = useState(search);
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  useEffect(() => {
    clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          if (searchText) next.set('search', searchText);
          else next.delete('search');
          return next;
        },
        { replace: true },
      );
    }, 250);
    return () => clearTimeout(debounceRef.current);
  }, [searchText]);

  const recipes = useQuery(recipesQuery(search, tag));

  function clearFilters() {
    setSearchText('');
    setSearchParams({});
  }

  return (
    <>
      <div className="mb-6 space-y-4">
        <input
          data-testid="search-input"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          placeholder="Search recipes..."
          className="w-full rounded-[var(--radius-control)] border border-[var(--color-border)] px-3 py-2"
        />
        <TagFilter active={tag} />
      </div>

      {recipes.isLoading ? (
        <div data-testid="list-skeleton" className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="h-56 animate-pulse rounded-[var(--radius-card)] bg-[var(--color-border)]" />
          ))}
        </div>
      ) : recipes.isError ? (
        <div data-testid="list-error" className="rounded-[var(--radius-card)] bg-[var(--color-danger-bg)] p-6 text-center">
          <p className="mb-4 text-[var(--color-danger)]">Couldn't load recipes.</p>
          <button
            data-testid="list-retry-button"
            onClick={() => recipes.refetch()}
            className="rounded-[var(--radius-control)] border px-4 py-2"
          >
            Retry
          </button>
        </div>
      ) : recipes.data?.length === 0 && !search && !tag ? (
        <div data-testid="list-empty" className="py-12 text-center">
          <p className="mb-4 text-[var(--color-ink-muted)]">No recipes yet.</p>
          <Link to="/recipes/new" className="rounded-[var(--radius-control)] bg-[var(--color-accent)] px-4 py-2 text-white">
            New recipe
          </Link>
        </div>
      ) : recipes.data?.length === 0 ? (
        <div data-testid="list-no-matches" className="py-12 text-center">
          <p className="mb-4 text-[var(--color-ink-muted)]">No matches.</p>
          <button data-testid="clear-filters-button" onClick={clearFilters} className="rounded-[var(--radius-control)] border px-4 py-2">
            Clear filters
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {recipes.data?.map((recipe) => (
            <RecipeCard key={recipe.id} recipe={recipe} />
          ))}
        </div>
      )}
    </>
  );
}
