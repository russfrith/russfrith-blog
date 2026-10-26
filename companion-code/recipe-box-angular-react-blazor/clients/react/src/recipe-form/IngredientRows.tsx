import { FieldError } from './FieldError';
import type { IngredientValues } from './formValues';

export function IngredientRows({
  items,
  onChange,
  errors,
}: {
  items: IngredientValues[];
  onChange: (next: IngredientValues[]) => void;
  errors: Record<string, string[]>;
}) {
  function update(index: number, patch: Partial<IngredientValues>) {
    onChange(items.map((row, i) => (i === index ? { ...row, ...patch } : row)));
  }

  return (
    <div className="space-y-2">
      {items.map((row, index) => (
        <div key={index} data-testid={`ingredient-row-${index}`} className="flex items-start gap-2">
          <div>
            <input
              type="number"
              step="any"
              value={row.quantity}
              onChange={(e) => update(index, { quantity: e.target.value })}
              name={`ingredients[${index}].quantity`}
              data-testid={`ingredient-quantity-input-${index}`}
              className="w-20 rounded-[var(--radius-control)] border border-[var(--color-border)] px-2 py-1"
            />
            <FieldError fieldKey={`ingredients[${index}].quantity`} errors={errors} />
          </div>
          <input
            value={row.unit}
            onChange={(e) => update(index, { unit: e.target.value })}
            name={`ingredients[${index}].unit`}
            data-testid={`ingredient-unit-input-${index}`}
            className="w-20 rounded-[var(--radius-control)] border border-[var(--color-border)] px-2 py-1"
          />
          <div className="flex-1">
            <input
              value={row.name}
              onChange={(e) => update(index, { name: e.target.value })}
              name={`ingredients[${index}].name`}
              data-testid={`ingredient-name-input-${index}`}
              className="w-full rounded-[var(--radius-control)] border border-[var(--color-border)] px-2 py-1"
            />
            <FieldError fieldKey={`ingredients[${index}].name`} errors={errors} />
          </div>
          <button
            type="button"
            data-testid={`ingredient-remove-${index}`}
            onClick={() => onChange(items.filter((_, i) => i !== index))}
            disabled={items.length <= 1}
            className="rounded-[var(--radius-control)] border px-2 py-1"
          >
            Remove
          </button>
        </div>
      ))}
      <FieldError fieldKey="ingredients" errors={errors} />
      <button
        type="button"
        data-testid="add-ingredient-button"
        onClick={() => onChange([...items, { quantity: '0', unit: '', name: '' }])}
        className="rounded-[var(--radius-control)] border px-3 py-1"
      >
        Add ingredient
      </button>
    </div>
  );
}
