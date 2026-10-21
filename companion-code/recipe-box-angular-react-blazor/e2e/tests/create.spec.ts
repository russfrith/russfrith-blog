import { test, expect } from '../fixtures';

// Spec.md end-to-end tests 7, 8.
test('create a recipe with two ingredients and two steps increments the header count', async ({ page }) => {
  await page.goto('/recipes/new');
  await page.getByTestId('form-title').fill('Test Soup');
  await page.getByTestId('form-servings').fill('4');

  await page.getByTestId('ingredient-name-input-0').fill('water');
  await page.getByTestId('ingredient-quantity-input-0').fill('2');
  await page.getByTestId('add-ingredient-button').click();
  await page.getByTestId('ingredient-name-input-1').fill('salt');
  await page.getByTestId('ingredient-quantity-input-1').fill('1');

  await page.getByTestId('step-input-0').fill('Boil the water.');
  await page.getByTestId('add-step-button').click();
  await page.getByTestId('step-input-1').fill('Add the salt.');

  await page.getByTestId('form-submit').click();
  await expect(page).toHaveURL(/\/recipes\/10$/);
  await expect(page.getByTestId('detail-title')).toContainText('Test Soup');

  await page.goto('/');
  await expect(page.getByTestId('header-count')).toContainText('10 recipes');
});

test('create form shows client-side errors for invalid input', async ({ page }) => {
  await page.goto('/recipes/new');
  await page.getByTestId('form-submit').click();

  await expect(page.getByTestId('form-error-title')).toBeVisible();
  await expect(page.getByTestId('form-error-ingredients[0].quantity')).toBeVisible();
  await expect(page.getByTestId('form-error-ingredients[0].name')).toBeVisible();
  await expect(page.getByTestId('form-error-steps[0].instruction')).toBeVisible();
  await expect(page).toHaveURL(/\/recipes\/new$/);
});
