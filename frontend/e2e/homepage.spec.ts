import { test, expect } from '@playwright/test';

test.describe('Homepage', () => {
  test('should load dashboard successfully', async ({ page }) => {
    await page.goto('/');

    // Check page title
    await expect(page).toHaveTitle(/Sazkomat/);

    // Check main heading
    await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();
  });

  test('should display navigation links', async ({ page }) => {
    await page.goto('/');

    // Check for main navigation items
    await expect(page.getByRole('link', { name: /dashboard/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /leagues/i })).toBeVisible();
  });

  test('should navigate to leagues page', async ({ page }) => {
    await page.goto('/');

    // Click on leagues link
    await page.getByRole('link', { name: /leagues/i }).click();

    // Verify URL changed
    await expect(page).toHaveURL(/\/leagues/);

    // Verify leagues page loaded
    await expect(page.getByRole('heading', { name: /leagues/i })).toBeVisible();
  });
});
