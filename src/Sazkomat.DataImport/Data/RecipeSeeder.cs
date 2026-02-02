using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Entities;
using System.Text.Json;

namespace Sazkomat.DataImport.Data;

/// <summary>
/// Seeds default scraper recipes for BetExplorer.
/// Recipes navigate via season dropdown (not direct URL) because BetExplorer
/// uses inconsistent URL formats across different leagues.
/// </summary>
public static class RecipeSeeder
{
    public static async Task SeedDefaultRecipesAsync(DataImportDbContext context)
    {
        var existingRecipes = await context.ScraperRecipes
            .Where(r => r.Provider == "betexplorer")
            .ToListAsync();

        // Priority order (most reliable first):
        // 1. Full Workflow - navigate via dropdown, click Results, sort by round, Show More
        // 2. Direct Sort - try direct URL with ?s=r parameter as fallback
        var newRecipes = new List<ScraperRecipe>
        {
            CreateFullWorkflowRecipe(),   // Priority 1 - full workflow via dropdown
            CreateDirectSortRecipe(),      // Priority 2 - direct URL fallback
        };

        if (!existingRecipes.Any())
        {
            // First time seeding
            await context.ScraperRecipes.AddRangeAsync(newRecipes);
        }
        else
        {
            // Update existing recipes - match by name and update actions/priority
            foreach (var newRecipe in newRecipes)
            {
                var existing = existingRecipes.FirstOrDefault(e => e.Name == newRecipe.Name);
                if (existing != null)
                {
                    existing.ActionsJson = newRecipe.ActionsJson;
                    existing.Priority = newRecipe.Priority;
                    existing.Description = newRecipe.Description;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // New recipe - add it
                    await context.ScraperRecipes.AddAsync(newRecipe);
                }
            }

            // Remove old recipes that are no longer needed
            var oldRecipeNames = new[] { "BetExplorer URL Sort", "BetExplorer Direct", "BetExplorer Sort Only" };
            var recipesToRemove = existingRecipes.Where(e => oldRecipeNames.Contains(e.Name)).ToList();
            if (recipesToRemove.Any())
            {
                context.ScraperRecipes.RemoveRange(recipesToRemove);
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Full workflow:
    /// 1. Navigate to base league URL
    /// 2. Find season in dropdown via JavaScript and get URL
    /// 3. Navigate to season URL
    /// 4. Click on Results tab
    /// 5. Click on Main stage tab (if exists) - ensures all regular season rounds
    /// 6. Sort by round via custom dropdown
    /// 7. Click Show More repeatedly
    /// 8. Extract HTML
    ///
    /// Note: {season} placeholder is replaced at runtime by RecipeExecutorService.
    /// Season format from DB is "2023-2024", but BetExplorer dropdown shows "2023/2024".
    /// </summary>
    private static ScraperRecipe CreateFullWorkflowRecipe()
    {
        // JavaScript to find season URL from dropdown
        // {season} will be replaced with season like "2023-2024" by RecipeExecutorService
        // We convert it to "2023/2024" format for dropdown matching
        const string findSeasonUrlScript = @"
            (() => {
                const seasonText = '{season}'.replace('-', '/');
                const select = document.querySelector('.wrap-section__header__select select');
                if (!select) return null;
                for (const option of select.options) {
                    if (option.text.includes(seasonText)) {
                        return option.value.startsWith('http') ? option.value : 'https://www.betexplorer.com' + option.value;
                    }
                }
                return null;
            })()";

        // JavaScript to click Results tab
        const string clickResultsTabScript = @"
            (() => {
                const links = Array.from(document.querySelectorAll('a'));
                const resultsLink = links.find(a => {
                    const href = a.getAttribute('href') || '';
                    return href.includes('/results') && !href.includes('/football/results/') && a.offsetParent !== null;
                });
                if (resultsLink) {
                    resultsLink.click();
                    return true;
                }
                return false;
            })()";

        // JavaScript to click Main stage tab (if exists and not already selected)
        // Some leagues have multiple stages (Main, Playoff, Relegation, etc.)
        // We want to ensure Main is selected to get all regular season rounds
        const string clickMainTabScript = @"
            (() => {
                // Look for stage tabs - they're usually in a list with class containing 'stages' or similar
                const stageTabs = document.querySelectorAll('.list-tabs a, .stages a, [class*=""stage""] a');
                for (const tab of stageTabs) {
                    const text = tab.textContent.toLowerCase().trim();
                    // Click on 'Main' or 'All' tab if found and visible
                    if ((text === 'main' || text === 'all' || text.includes('main stage')) && tab.offsetParent !== null) {
                        // Check if already selected (has 'active' class or similar)
                        if (!tab.classList.contains('active') && !tab.parentElement?.classList.contains('active')) {
                            tab.click();
                            return 'clicked: ' + text;
                        }
                        return 'already selected: ' + text;
                    }
                }
                return 'no main tab found';
            })()";

        // JavaScript to sort by round
        const string sortByRoundScript = @"
            (() => {
                // Try custom dropdown first
                const dropdown = document.querySelector('#js-leagueresults-sort + div.select');
                if (dropdown) {
                    dropdown.click();
                    setTimeout(() => {
                        const roundOption = document.querySelector('#js-leagueresults-sort + div.select li[rel=""r""]');
                        if (roundOption) roundOption.click();
                    }, 500);
                    return 'dropdown';
                }
                // Fallback: change select value directly
                const select = document.querySelector('#js-leagueresults-sort');
                if (select) {
                    select.value = 'r';
                    select.dispatchEvent(new Event('change'));
                    return 'select';
                }
                return null;
            })()";

        // JavaScript to click Show More repeatedly
        const string showMoreScript = @"
            (async () => {
                for (let i = 0; i < 10; i++) {
                    const links = Array.from(document.querySelectorAll('a'));
                    const showMore = links.find(a =>
                        a.textContent.toLowerCase().includes('show more') &&
                        a.offsetParent !== null
                    );
                    if (showMore) {
                        showMore.click();
                        await new Promise(r => setTimeout(r, 1500));
                    } else {
                        break;
                    }
                }
            })()";

        var actions = new object[]
        {
            // 1. Navigate to base league URL
            new { type = "navigate", url = "{baseUrl}" },
            new { type = "waitForLoadState", state = "load", timeout = 30000 },
            new { type = "wait", milliseconds = 1000 },

            // 2. Find season URL from dropdown and store in variable
            new { type = "evaluate", script = findSeasonUrlScript, storeAs = "seasonUrl" },

            // 3. Navigate to season URL (uses stored variable)
            new { type = "navigateToVariable", variable = "seasonUrl" },
            new { type = "waitForLoadState", state = "load", timeout = 30000 },
            new { type = "wait", milliseconds = 1000 },

            // 4. Click Results tab
            new { type = "evaluate", script = clickResultsTabScript },
            new { type = "waitForLoadState", state = "networkidle", timeout = 15000 },
            new { type = "wait", milliseconds = 1000 },

            // 5. Click Main stage tab (if exists) to ensure we get all regular season rounds
            new { type = "evaluate", script = clickMainTabScript },
            new { type = "wait", milliseconds = 1500 },
            new { type = "waitForLoadState", state = "networkidle", timeout = 10000 },

            // 6. Sort by round
            new { type = "evaluate", script = sortByRoundScript },
            new { type = "wait", milliseconds = 2000 },
            new { type = "waitForLoadState", state = "networkidle", timeout = 15000 },

            // 7. Click Show More repeatedly
            new { type = "evaluate", script = showMoreScript },
            new { type = "wait", milliseconds = 1000 },

            // 8. Extract HTML
            new { type = "extractHtml", maxLength = 1000000 }
        };

        return new ScraperRecipe
        {
            Name = "BetExplorer Full Workflow",
            Description = "Full workflow: navigate via dropdown, Results tab, sort by round, Show More (Priority 1)",
            Provider = "betexplorer",
            PageType = "results",
            Priority = 1,
            IsActive = true,
            ActionsJson = JsonSerializer.Serialize(actions),
            RoundHeaderSelector = ".//th[contains(text(), 'Round')]",
            GroupPatternRegex = @"^(.+?)\s*-\s*(\d+)\.\s*Round$",
            MatchRowSelector = ".//tr[td[contains(@class, 'h-text-left')]]"
        };
    }

    /// <summary>
    /// Direct URL fallback - tries to construct direct URL with season.
    /// Less reliable but faster when it works.
    /// </summary>
    private static ScraperRecipe CreateDirectSortRecipe()
    {
        // JavaScript to click Show More repeatedly
        var showMoreScript = @"
            (async () => {
                for (let i = 0; i < 10; i++) {
                    const links = Array.from(document.querySelectorAll('a'));
                    const showMore = links.find(a =>
                        a.textContent.toLowerCase().includes('show more') &&
                        a.offsetParent !== null
                    );
                    if (showMore) {
                        showMore.click();
                        await new Promise(r => setTimeout(r, 1500));
                    } else {
                        break;
                    }
                }
            })()";

        var actions = new object[]
        {
            // Try direct URL with season in league slug format (league-YYYY-YYYY/results/?s=r)
            // This works for some leagues where URL is predictable
            new { type = "navigate", url = "{baseUrl}results/?s=r" },
            new { type = "waitForLoadState", state = "load", timeout = 30000 },
            new { type = "wait", milliseconds = 2000 },

            // Click Show More
            new { type = "evaluate", script = showMoreScript },
            new { type = "wait", milliseconds = 1000 },

            // Extract HTML
            new { type = "extractHtml", maxLength = 1000000 }
        };

        return new ScraperRecipe
        {
            Name = "BetExplorer Direct Sort",
            Description = "Direct URL with ?s=r - fallback for current season (Priority 2)",
            Provider = "betexplorer",
            PageType = "results",
            Priority = 2,
            IsActive = true,
            ActionsJson = JsonSerializer.Serialize(actions),
            RoundHeaderSelector = ".//th[contains(text(), 'Round')]",
            GroupPatternRegex = @"^(.+?)\s*-\s*(\d+)\.\s*Round$",
            MatchRowSelector = ".//tr[td[contains(@class, 'h-text-left')]]"
        };
    }
}
