import { test, expect } from '../fixtures';

// Spec.md end-to-end test 13.
test('delete a recipe with confirmation, and the header count decrements', async ({ page }) => {
  await page.goto('/recipes/9');
  await page.getByTestId('delete-button').click();
  await page.getByTestId('delete-confirm-button').click();

  await expect(page).toHaveURL('/');
  await expect(page.getByTestId('header-count')).toContainText('8 recipes');
  await expect(page.getByTestId('recipe-card-9')).toHaveCount(0);
});
