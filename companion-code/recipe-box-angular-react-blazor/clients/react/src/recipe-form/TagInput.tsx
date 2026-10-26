import { useState } from 'react';
import { FieldError } from './FieldError';

export function TagInput({ tags, onChange, errors }: { tags: string[]; onChange: (next: string[]) => void; errors: Record<string, string[]> }) {
  const [value, setValue] = useState('');

  function add() {
    const tag = value.trim().toLowerCase();
    setValue('');
    if (tag) onChange([...tags, tag]);
  }

  return (
    <div>
      <div className="flex flex-wrap items-center gap-2">
        {tags.map((tag, index) => (
          <button
            key={index}
            type="button"
            data-testid={`form-tag-${tag}`}
            onClick={() => onChange(tags.filter((_, i) => i !== index))}
            className="rounded-full border border-[var(--color-border)] px-3 py-1 text-sm"
          >
            {tag} &times;
          </button>
        ))}
        <input
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault();
              add();
            }
          }}
          data-testid="form-tag-input"
          placeholder="Add a tag and press Enter"
          className="rounded-[var(--radius-control)] border border-[var(--color-border)] px-2 py-1"
        />
      </div>
      <FieldError fieldKey="tags" errors={errors} />
    </div>
  );
}
