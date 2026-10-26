import { FieldError } from './FieldError';

export function StepRows({
  items,
  onChange,
  errors,
}: {
  items: string[];
  onChange: (next: string[]) => void;
  errors: Record<string, string[]>;
}) {
  function moveUp(index: number) {
    if (index === 0) return;
    const next = [...items];
    [next[index - 1], next[index]] = [next[index], next[index - 1]];
    onChange(next);
  }

  function moveDown(index: number) {
    if (index === items.length - 1) return;
    const next = [...items];
    [next[index], next[index + 1]] = [next[index + 1], next[index]];
    onChange(next);
  }

  return (
    <div className="space-y-2">
      {items.map((instruction, index) => (
        <div key={index} data-testid={`step-row-${index}`} className="flex items-start gap-2">
          <div className="flex-1">
            <textarea
              value={instruction}
              onChange={(e) => onChange(items.map((s, i) => (i === index ? e.target.value : s)))}
              name={`steps[${index}].instruction`}
              data-testid={`step-input-${index}`}
              className="w-full rounded-[var(--radius-control)] border border-[var(--color-border)] px-2 py-1"
            />
            <FieldError fieldKey={`steps[${index}].instruction`} errors={errors} />
          </div>
          <button type="button" data-testid={`step-up-${index}`} onClick={() => moveUp(index)} disabled={index === 0} className="rounded-[var(--radius-control)] border px-2 py-1">
            &uarr;
          </button>
          <button
            type="button"
            data-testid={`step-down-${index}`}
            onClick={() => moveDown(index)}
            disabled={index === items.length - 1}
            className="rounded-[var(--radius-control)] border px-2 py-1"
          >
            &darr;
          </button>
          <button
            type="button"
            data-testid={`step-remove-${index}`}
            onClick={() => onChange(items.filter((_, i) => i !== index))}
            disabled={items.length <= 1}
            className="rounded-[var(--radius-control)] border px-2 py-1"
          >
            Remove
          </button>
        </div>
      ))}
      <FieldError fieldKey="steps" errors={errors} />
      <button type="button" data-testid="add-step-button" onClick={() => onChange([...items, ''])} className="rounded-[var(--radius-control)] border px-3 py-1">
        Add step
      </button>
    </div>
  );
}
