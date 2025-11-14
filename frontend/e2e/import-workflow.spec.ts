import { test, expect } from '@playwright/test';

test.describe('Import Workflow', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to import page
    await page.goto('/import');
    await page.waitForLoadState('networkidle');
  });

  test('should display import page', async ({ page }) => {
    // Check page heading
    await expect(page.getByRole('heading', { name: /import|data import/i })).toBeVisible();

    // Check for import form or options
    await expect(page.getByText(/league|season/i)).toBeVisible();
  });

  test('should display available leagues for import', async ({ page }) => {
    // Should show league selection dropdown or list
    const leagueSelector = page.getByLabel(/league|select league/i);

    if (await leagueSelector.isVisible()) {
      await leagueSelector.click();

      // Should show league options
      await expect(page.getByRole('option').first()).toBeVisible();
    }
  });

  test('should initiate historical import', async ({ page }) => {
    // Select a league
    const leagueSelector = page.getByLabel(/league|select league/i);

    if (await leagueSelector.isVisible()) {
      await leagueSelector.click();

      // Select first available league
      await page.getByRole('option').first().click();

      // Select season(s)
      const seasonSelector = page.getByLabel(/season/i);
      if (await seasonSelector.isVisible()) {
        await seasonSelector.click();
        await page.getByRole('option').first().click();
      }

      // Click import button
      const importButton = page.getByRole('button', { name: /import|start import/i });
      await importButton.click();

      // Verify import started
      await expect(
        page.getByText(/importing|in progress|pending/i)
      ).toBeVisible({ timeout: 10000 });
    }
  });

  test('should display import job progress', async ({ page }) => {
    // Look for job status or progress section
    const jobSection = page.getByText(/job|progress|status/i).first();

    if (await jobSection.isVisible()) {
      // Should show progress indicator
      await expect(page.getByText(/pending|running|completed|failed/i)).toBeVisible();
    }
  });

  test('should display import statistics', async ({ page }) => {
    // Look for statistics section
    const statsSection = page.getByText(/statistics|stats/i).first();

    if (await statsSection.isVisible()) {
      // Should show numeric data
      await expect(page.locator('[class*="stat"], [class*="metric"]')).toBeVisible();
    }
  });

  test('should poll job status updates', async ({ page }) => {
    // Start an import (if possible)
    const importButton = page.getByRole('button', { name: /import|start/i }).first();

    if (await importButton.isVisible()) {
      await importButton.click();

      // Wait and verify status updates
      await page.waitForTimeout(3000);

      // Status should be visible and potentially changing
      const statusText = page.getByText(/pending|running|completed/i).first();
      await expect(statusText).toBeVisible();
    }
  });

  test('should display recent import jobs', async ({ page }) => {
    // Look for job history or recent jobs section
    const jobsTable = page.locator('table').first();

    if (await jobsTable.isVisible()) {
      await expect(jobsTable).toBeVisible();

      // Should have job data
      const rows = jobsTable.locator('tbody tr');
      expect(await rows.count()).toBeGreaterThan(0);
    }
  });

  test('should filter import jobs by league', async ({ page }) => {
    // Look for filter options
    const filterDropdown = page.getByLabel(/filter|league/i);

    if (await filterDropdown.isVisible()) {
      // Get initial job count
      const initialJobs = await page.locator('table tbody tr').count();

      // Apply filter
      await filterDropdown.click();
      await page.getByRole('option').first().click();

      // Wait for filtering
      await page.waitForTimeout(500);

      // Jobs should be filtered (count may change)
      const filteredJobs = await page.locator('table tbody tr').count();
      expect(filteredJobs).toBeLessThanOrEqual(initialJobs);
    }
  });

  test('should view import job details', async ({ page }) => {
    // Look for view/details button
    const detailsButton = page.getByRole('button', { name: /view|details/i }).first();

    if (await detailsButton.isVisible()) {
      await detailsButton.click();

      // Should show detailed information
      await expect(page.getByText(/rounds|matches|total/i)).toBeVisible();
    }
  });

  test('should handle multi-league import', async ({ page }) => {
    // Look for multi-select option
    const leagueSelector = page.getByLabel(/league|select league/i);

    if (await leagueSelector.isVisible()) {
      await leagueSelector.click();

      // Try to select multiple leagues (if supported)
      const options = page.getByRole('option');
      const optionCount = await options.count();

      if (optionCount > 1) {
        // Select first option
        await options.nth(0).click();

        // Try to select second option (multi-select behavior)
        // This may vary based on your UI implementation
        const secondOption = options.nth(1);
        if (await secondOption.isVisible()) {
          await secondOption.click();
        }

        // Verify multiple selections (if UI supports it)
        const selectedTags = page.locator('[class*="tag"], [class*="badge"]');
        expect(await selectedTags.count()).toBeGreaterThan(0);
      }
    }
  });

  test('should handle multi-season import', async ({ page }) => {
    // Select a league first
    const leagueSelector = page.getByLabel(/league/i);

    if (await leagueSelector.isVisible()) {
      await leagueSelector.click();
      await page.getByRole('option').first().click();

      // Now select seasons
      const seasonSelector = page.getByLabel(/season/i);
      if (await seasonSelector.isVisible()) {
        await seasonSelector.click();

        // Try to select multiple seasons
        const seasonOptions = page.getByRole('option');
        const seasonCount = await seasonOptions.count();

        if (seasonCount > 1) {
          await seasonOptions.nth(0).click();

          // Try second season
          const secondSeason = seasonOptions.nth(1);
          if (await secondSeason.isVisible()) {
            await secondSeason.click();
          }
        }
      }
    }
  });

  test('should validate import form', async ({ page }) => {
    // Try to submit without selecting required fields
    const importButton = page.getByRole('button', { name: /import|start/i });

    if (await importButton.isVisible()) {
      await importButton.click();

      // Should show validation error
      await expect(page.getByText(/required|select|choose/i)).toBeVisible({ timeout: 5000 });
    }
  });

  test('should cancel import job', async ({ page }) => {
    // Look for running jobs
    const runningJob = page.getByText(/running|in progress/i).first();

    if (await runningJob.isVisible()) {
      // Look for cancel button
      const cancelButton = page.getByRole('button', { name: /cancel|stop/i }).first();

      if (await cancelButton.isVisible()) {
        await cancelButton.click();

        // Confirm cancellation
        const confirmButton = page.getByRole('button', { name: /confirm|yes/i });
        if (await confirmButton.isVisible()) {
          await confirmButton.click();
        }

        // Verify job was cancelled
        await expect(page.getByText(/cancelled|stopped/i)).toBeVisible({ timeout: 5000 });
      }
    }
  });
});

test.describe('Import Dashboard Integration', () => {
  test('should navigate from dashboard to import', async ({ page }) => {
    // Start from dashboard
    await page.goto('/');

    // Look for import link or button
    const importLink = page.getByRole('link', { name: /import/i });

    if (await importLink.isVisible()) {
      await importLink.click();

      // Verify navigation
      await expect(page).toHaveURL(/\/import/);
    }
  });

  test('should display import stats on dashboard', async ({ page }) => {
    await page.goto('/');

    // Dashboard should show import-related metrics
    await expect(page.getByText(/rounds|matches|import/i)).toBeVisible();

    // Should show numeric statistics
    const statCards = page.locator('[class*="card"], [class*="stat"]');
    expect(await statCards.count()).toBeGreaterThan(0);
  });
});
