import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { formatQuantity, scaledQuantity } from './quantities';

// Same file the C# tests run (shared/RecipeBox.Shared.Tests) - no vectors
// are hand-duplicated here. See spec.md's "Shared logic" section.
interface Vector {
  description: string;
  quantity: number;
  originalServings: number;
  targetServings: number;
  expected: string;
}

const vectorsPath = join(import.meta.dirname, '../../../../shared/test-vectors.json');
const vectors: Vector[] = JSON.parse(readFileSync(vectorsPath, 'utf-8'));

describe('formatQuantity(scaledQuantity(...))', () => {
  it.each(vectors)('$description', ({ quantity, originalServings, targetServings, expected }) => {
    const scaled = scaledQuantity(quantity, originalServings, targetServings);
    expect(formatQuantity(scaled)).toBe(expected);
  });
});
