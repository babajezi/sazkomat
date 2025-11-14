import { test, expect } from '@playwright/test';

test.describe('League CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to leagues page before each test
    await page.goto('/leagues');
    await expect(page.getByRole('heading', { name: /leagues/i })).toBeVisible();
  });

  test('should display leagues table', async ({ page }) => {
    // Wait for table to load
    await page.waitForSelector('table', { timeout: 10000 });

    // Check table headers
    await expect(page.getByText('Name')).toBeVisible();
    await expect(page.getByText('Country')).toBeVisible();
    await expect(page.getByText('Sport')).toBeVisible();
  });

  test('should create a new league', async ({ page }) => {
    // Click "Create League" button
    const createButton = page.getByRole('button', { name: /create league/i });
    await createButton.click();

    // Fill in the form
    const leagueName = `Test League ${Date.now()}`;
    await page.getByLabel(/league name/i).fill(leagueName);

    // Select country (assumes dropdown is available)
    await page.getByLabel(/country/i).click();
    await page.getByRole('option', { name: /england/i }).first().click();

    // Select sport
    await page.getByLabel(/sport/i).click();
    await page.getByRole('option', { name: /football/i }).first().click();

    // Fill provider URL
    await page.getByLabel(/provider url/i).fill('/football/test-league/');

    // Submit form
    await page.getByRole('button', { name: /create/i }).click();

    // Verify success
    await expect(page.getByText(leagueName)).toBeVisible({ timeout: 5000 });
  });

  test('should edit an existing league', async ({ page }) => {
    // Wait for table to load
    await page.waitForSelector('table', { timeout: 10000 });

    // Click first edit button (assuming there's at least one league)
    const editButton = page.getByRole('button', { name: /edit/i }).first();
    await editButton.click();

    // Wait for dialog to open
    await page.waitForSelector('dialog[open], [role="dialog"]', { timeout: 5000 });

    // Modify league name
    const nameInput = page.getByLabel(/league name/i);
    await nameInput.clear();
    const newName = `Updated League ${Date.now()}`;
    await nameInput.fill(newName);

    // Save changes
    await page.getByRole('button', { name: /save|update/i }).click();

    // Verify success
    await expect(page.getByText(newName)).toBeVisible({ timeout: 5000 });
  });

  test('should delete a league', async ({ page }) => {
    // Wait for table to load
    await page.waitForSelector('table', { timeout: 10000 });

    // Get initial row count
    const initialRows = await page.locator('table tbody tr').count();
    expect(initialRows).toBeGreaterThan(0);

    // Click first delete button
    const deleteButton = page.getByRole('button', { name: /delete/i }).first();
    await deleteButton.click();

    // Confirm deletion in dialog
    await page.getByRole('button', { name: /confirm|yes|delete/i }).click();

    // Wait for deletion to complete
    await page.waitForTimeout(1000);

    // Verify row count decreased
    const finalRows = await page.locator('table tbody tr').count();
    expect(finalRows).toBeLessThan(initialRows);
  });

  test('should toggle league enabled status', async ({ page }) => {
    // Wait for table to load
    await page.waitForSelector('table', { timeout: 10000 });

    // Find first toggle switch
    const toggle = page.locator('[role="switch"]').first();
    await expect(toggle).toBeVisible();

    // Get initial state
    const initialState = await toggle.getAttribute('aria-checked');

    // Click toggle
    await toggle.click();

    // Wait for API call to complete
    await page.waitForTimeout(500);

    // Verify state changed
    const newState = await toggle.getAttribute('aria-checked');
    expect(newState).not.toBe(initialState);
  });

  test('should filter leagues by search', async ({ page }) => {
    // Wait for table to load
    await page.waitForSelector('table', { timeout: 10000 });

    // Get initial row count
    const initialRows = await page.locator('table tbody tr').count();

    // Type in search box (if it exists)
    const searchInput = page.getByPlaceholder(/search/i);
    if (await searchInput.isVisible()) {
      await searchInput.fill('Premier');

      // Wait for filtering
      await page.waitForTimeout(500);

      // Verify results changed
      const filteredRows = await page.locator('table tbody tr').count();
      expect(filteredRows).toBeLessThanOrEqual(initialRows);

      // Verify filtered content
      await expect(page.getByText(/premier/i)).toBeVisible();
    }
  });
});
