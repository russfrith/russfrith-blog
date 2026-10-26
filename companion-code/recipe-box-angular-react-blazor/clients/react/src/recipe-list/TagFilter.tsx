import { useQuery } from '@tanstack/react-query';
import { useSearchParams } from 'react-router';
import { tagsQuery } from '../queries';

export function TagFilter({ active }: { active: string }) {
  const { data: tags } = useQuery(tagsQuery());
  const [, setSearchParams] = useSearchParams();

  function toggle(name: string) {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev);
        if (active === name) next.delete('tag');
        else next.set('tag', name);
        return next;
      },
      { replace: true },
    );
  }

  return (
    <div className="flex flex-wrap gap-2">
      {tags?.map((tag) => (
        <button
          key={tag.name}
          type="button"
          onClick={() => toggle(tag.name)}
          data-testid={`tag-chip-${tag.name}`}
          className={
            active === tag.name
              ? 'rounded-full border border-[var(--color-accent)] bg-[var(--color-accent)] px-3 py-1 text-sm text-white'
              : 'rounded-full border border-[var(--color-border)] px-3 py-1 text-sm text-[var(--color-ink-muted)]'
          }
        >
          {tag.name} ({tag.count})
        </button>
      ))}
    </div>
  );
}
