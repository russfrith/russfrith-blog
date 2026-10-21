import { test as base } from '@playwright/test';

// Resets the database and seed images before every test, not just once
// per run (cheap at 9 rows/5 images) - every test gets deterministic ids
// regardless of execution order.
export const test = base.extend({
  page: async ({ page, request, baseURL }, use) => {
    const res = await request.post(`${baseURL}/api/test/reset`);
    if (res.status() !== 204) throw new Error(`reset failed: ${res.status()}`);
    await use(page);
  },
});

export { expect } from '@playwright/test';
