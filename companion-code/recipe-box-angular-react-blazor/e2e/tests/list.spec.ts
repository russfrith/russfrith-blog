import { test, expect } from '../fixtures';

// Spec.md end-to-end tests 1-4.
test('shows seeded recipes and the header count', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByTestId('header-count')).toContainText('9 recipes');
  await expect(page.locator('a[data-testid^="recipe-card-"]:not([data-testid*="title"])')).toHaveCount(9);
});

test('search filters the list, and a no-match query shows the empty state', async ({ page }) => {
  await page.goto('/');
  await page.getByTestId('search-input').fill('lemon');
  await expect(page).toHaveURL(/search=lemon/);
  await expect(page.locator('a[data-testid^="recipe-card-"]:not([data-testid*="title"])')).toHaveCount(1);
  await expect(page.getByTestId('recipe-card-title-1')).toContainText('Lemon Pancakes');

  await page.getByTestId('search-input').fill('zzz-no-match');
  await expect(page.getByTestId('list-no-matches')).toBeVisible();
  await page.getByTestId('clear-filters-button').click();
  await expect(page.locator('a[data-testid^="recipe-card-"]:not([data-testid*="title"])')).toHaveCount(9);
});

test('tag chip filters the list and the URL reflects it', async ({ page }) => {
  await page.goto('/');
  await page.getByTestId('tag-chip-dessert').click();
  await expect(page).toHaveURL(/tag=dessert/);
  await expect(page.locator('a[data-testid^="recipe-card-"]:not([data-testid*="title"])')).toHaveCount(1);
  await expect(page.getByTestId('recipe-card-title-3')).toContainText('Chocolate Chip Cookies');
});

const isRecipeList = (url: URL) => url.pathname === '/api/recipes';

test('loading skeleton appears and the error state appears with retry', async ({ page }) => {
  // unroute(predicate) only removes a route if the predicate is the SAME
  // function reference passed to route() - pass `isRecipeList` directly
  // (not a new wrapper arrow each time), or routes stack instead of being
  // replaced.
  const delayThenContinue = async (route: import('@playwright/test').Route) => {
    await new Promise((resolve) => setTimeout(resolve, 1500));
    await route.continue();
  };
  await page.route(isRecipeList, delayThenContinue);
  const pending = page.goto('/');
  await expect(page.getByTestId('list-skeleton')).toBeVisible();
  await pending;
  await expect(page.locator('a[data-testid^="recipe-card-"]:not([data-testid*="title"])')).toHaveCount(9);

  await page.unroute(isRecipeList, delayThenContinue);
  const failOnce = (route: import('@playwright/test').Route) => route.fulfill({ status: 500, body: '{}' });
  await page.route(isRecipeList, failOnce);
  await page.reload();
  await expect(page.getByTestId('list-error')).toBeVisible();

  await page.unroute(isRecipeList, failOnce);
  await page.getByTestId('list-retry-button').click();
  await expect(page.locator('a[data-testid^="recipe-card-"]:not([data-testid*="title"])')).toHaveCount(9);
});
