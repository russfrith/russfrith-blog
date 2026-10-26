import { Link } from 'react-router';
import { useRecipeCount } from '../recipeCount';

export function Header() {
  const count = useRecipeCount();

  return (
    <header className="border-b border-[var(--color-border)] bg-[var(--color-paper)]">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-4">
        <div>
          <Link to="/" className="font-serif text-xl text-[var(--color-ink)]">
            Recipe Box
          </Link>
          <p data-testid="header-count" className="text-sm text-[var(--color-ink-muted)]">
            {count !== undefined && `${count} ${count === 1 ? 'recipe' : 'recipes'}`}
          </p>
        </div>
        <Link
          to="/recipes/new"
          data-testid="new-recipe-button"
          className="rounded-[var(--radius-control)] bg-[var(--color-accent)] px-4 py-2 text-white hover:bg-[var(--color-accent-hover)]"
        >
          New recipe
        </Link>
      </div>
    </header>
  );
}
