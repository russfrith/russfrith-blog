import { defineConfig, devices } from '@playwright/test';

if (!process.env['BASE_URL']) throw new Error('BASE_URL must be set (e.g. http://angular:8080)');

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  globalSetup: './global-setup.ts',
  use: {
    baseURL: process.env['BASE_URL'],
    viewport: { width: 1280, height: 800 },
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  reporter: [['html', { outputFolder: 'playwright-report', open: 'never' }]],
});
