import { test, expect } from '../fixtures';

// Spec.md end-to-end test 12. Angular takes the full upload path, not the
// fallback (see CLAUDE.md/blog-notes.md), so this covers both halves.
test('upload a hero image and see it on the detail page; reject a wrong file type', async ({ page }) => {
  await page.goto('/recipes/9/edit'); // Classic Vinaigrette - the seeded recipe with no image
  await page.getByTestId('form-image-input').setInputFiles('../seed/images/veggie-stirfry.png');
  await page.getByTestId('form-submit').click();

  await expect(page.getByTestId('detail-image')).toHaveAttribute('src', /\/api\/images\/.+\.png$/);
});

test('reject a wrong file type', async ({ page }) => {
  await page.goto('/recipes/9/edit');
  await page.getByTestId('form-image-input').setInputFiles({
    name: 'not-an-image.txt',
    mimeType: 'text/plain',
    buffer: Buffer.from('this is not an image'),
  });
  await page.getByTestId('form-submit').click();

  await expect(page.getByTestId('form-error-file')).toBeVisible();
});
