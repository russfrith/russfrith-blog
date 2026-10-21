import { test } from '../fixtures';

// Working artifacts for the post, not committed - gitignored, overwritten
// on every run. {client} comes from BASE_URL's hostname.
const client = new URL(process.env['BASE_URL']!).hostname;
const dir = `screenshots/${client}`;

test('capture list, detail, and form screenshots', async ({ page }) => {
  await page.goto('/');
  await page.screenshot({ path: `${dir}/list.png`, fullPage: true });

  await page.goto('/recipes/1');
  await page.screenshot({ path: `${dir}/detail.png`, fullPage: true });

  await page.goto('/recipes/1/edit');
  await page.screenshot({ path: `${dir}/form.png`, fullPage: true });
});
