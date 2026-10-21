import { test, expect } from '../fixtures';

// Spec.md end-to-end tests 5, 6, 14.
test('detail shows the recipe', async ({ page }) => {
  await page.goto('/recipes/1');
  await expect(page.getByTestId('detail-title')).toContainText('Lemon Pancakes');
  await expect(page.getByTestId('detail-image')).toBeVisible();
  await expect(page.getByTestId('ingredient-name-0')).toContainText('flour');
  await expect(page.getByTestId('step-0')).toContainText('Whisk the dry ingredients together.');
});

// shared/test-vectors.json's 0.3333-scaled-by-3 case, applied to the real
// seeded recipe spec.md requires for this exact test (Servings 3 / 0.3333
// cup flour).
test('servings scaler recalculates quantities and formats fractions', async ({ page }) => {
  await page.goto('/recipes/1');
  await expect(page.getByTestId('servings-value')).toHaveText('3');
  await expect(page.getByTestId('ingredient-quantity-0')).toHaveText('1/3');

  for (let i = 0; i < 6; i++) await page.getByTestId('servings-plus').click();

  await expect(page.getByTestId('servings-value')).toHaveText('9');
  await expect(page.getByTestId('ingredient-quantity-0')).toHaveText('1');
  // 1.5 tsp baking powder scaled 3 -> 9 is 4.5, i.e. "4 1/2".
  await expect(page.getByTestId('ingredient-quantity-1')).toHaveText('4 1/2');
});

test('detail page for a missing id shows not found', async ({ page }) => {
  await page.goto('/recipes/999');
  await expect(page.getByTestId('detail-not-found')).toBeVisible();
});
