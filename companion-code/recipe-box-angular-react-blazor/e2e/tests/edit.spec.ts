import { test, expect } from '../fixtures';

// Spec.md end-to-end tests 9, 10, 11.
test('server validation error maps to the right field: title uniqueness excludes self', async ({ page }) => {
  // Colliding with a DIFFERENT recipe's title fails.
  await page.goto('/recipes/2/edit');
  await page.getByTestId('form-title').fill('Lemon Pancakes');
  await page.getByTestId('form-submit').click();
  await expect(page.getByTestId('form-error-title')).toContainText('already exists');

  // Resaving a recipe's OWN (unchanged) title must not 400 against itself.
  await page.goto('/recipes/1/edit');
  await page.getByTestId('form-submit').click();
  await expect(page).toHaveURL(/\/recipes\/1$/);
});

test('edit a recipe and the change persists', async ({ page }) => {
  await page.goto('/recipes/5/edit');
  await page.getByTestId('form-title').fill('Updated Banana Bread');
  await page.getByTestId('form-submit').click();
  await expect(page.getByTestId('detail-title')).toContainText('Updated Banana Bread');

  await page.reload();
  await expect(page.getByTestId('detail-title')).toContainText('Updated Banana Bread');
});

test('reorder and remove steps', async ({ page }) => {
  await page.goto('/recipes/3/edit');
  await expect(page.getByTestId('step-row-4')).toBeVisible();

  await page.getByTestId('step-down-0').click(); // step 0 and 1 swap
  await page.getByTestId('step-remove-4').click(); // drop the last step
  await page.getByTestId('form-submit').click();

  await expect(page.getByTestId('step-0')).toContainText('Beat in the eggs and vanilla.');
  await expect(page.getByTestId('step-1')).toContainText('Cream the butter and sugars together.');
  await expect(page.getByTestId('step-3')).toContainText('Fold in the chocolate chips.');
});
