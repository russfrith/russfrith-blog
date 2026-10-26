// TypeScript port of shared/RecipeBox.Shared/Quantities.cs. Algorithm must
// stay identical: round to 4 decimal places (away from zero) BEFORE
// snapping to a fraction - that's what makes this agree with the C#
// `decimal` implementation at the non-finite 1/4-1/3 boundary. See
// spec.md's "Shared logic" section and shared/test-vectors.json.
const FRACTION_VALUES = [0, 0.125, 0.25, 1 / 3, 0.375, 0.5, 0.625, 2 / 3, 0.75, 0.875, 1];
const FRACTION_LABELS = ['', '1/8', '1/4', '1/3', '3/8', '1/2', '5/8', '2/3', '3/4', '7/8', ''];

export function scaledQuantity(quantity: number, originalServings: number, targetServings: number): number {
  return (quantity * targetServings) / originalServings;
}

export function formatQuantity(value: number): string {
  const rounded = Math.round(value * 10000) / 10000;
  return rounded < 10 ? formatUnderTen(rounded) : formatTenOrMore(rounded);
}

function formatUnderTen(rounded: number): string {
  const whole = Math.floor(rounded);
  const fractional = rounded - whole;

  let bestIndex = 0;
  let bestDistance = Infinity;
  for (let i = 0; i < FRACTION_VALUES.length; i++) {
    const distance = Math.abs(FRACTION_VALUES[i] - fractional);
    if (distance < bestDistance || (distance === bestDistance && FRACTION_VALUES[i] > FRACTION_VALUES[bestIndex])) {
      bestDistance = distance;
      bestIndex = i;
    }
  }

  let carriedWhole = whole;
  if (bestIndex === FRACTION_VALUES.length - 1) {
    carriedWhole += 1;
    bestIndex = 0;
  } else if (bestIndex === 0 && whole === 0) {
    bestIndex = 1; // the 1/8 floor: never show a bare "0"
  }

  return combine(carriedWhole, FRACTION_LABELS[bestIndex]);
}

function formatTenOrMore(rounded: number): string {
  const halves = Math.round(rounded * 2);
  const whole = Math.floor(halves / 2);
  const hasHalf = halves % 2 !== 0;
  return combine(whole, hasHalf ? '1/2' : '');
}

function combine(whole: number, fraction: string): string {
  if (fraction === '') return whole.toString();
  return whole === 0 ? fraction : `${whole} ${fraction}`;
}
