export function ServingsStepper({
  servings,
  onServingsChange,
}: {
  servings: number;
  onServingsChange: (next: number) => void;
}) {
  function step(delta: number) {
    onServingsChange(Math.min(48, Math.max(1, servings + delta)));
  }

  return (
    <div className="flex items-center gap-3">
      <button data-testid="servings-minus" onClick={() => step(-1)} disabled={servings <= 1} className="h-8 w-8 rounded-full border">
        &minus;
      </button>
      <span data-testid="servings-value" className="w-8 text-center">
        {servings}
      </span>
      <button data-testid="servings-plus" onClick={() => step(1)} disabled={servings >= 48} className="h-8 w-8 rounded-full border">
        +
      </button>
    </div>
  );
}
