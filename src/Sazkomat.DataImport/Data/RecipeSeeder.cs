using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Entities;
using System.Text.Json;

namespace Sazkomat.DataImport.Data;

/// <summary>
/// Seeds default scraper recipes for BetExplorer.
/// Recipes are based on actual PlaywrightHttpClient.GetBetExplorerMultiSeasonResultsAsync implementation.
/// </summary>
public static class RecipeSeeder
{
    public static async Task SeedDefaultRecipesAsync(DataImportDbContext context)
    {
        // Check if we already have recipes
        if (await context.ScraperRecipes.AnyAsync())
        {
            return;
        }

        var recipes = new List<ScraperRecipe>
        {
            // Recipe 1: Full BetExplorer workflow with sort and show more
            CreateFullWorkflowRecipe(),

            // Recipe 2: Sort click only (no show more - for smaller leagues)
            CreateSortOnlyRecipe(),

            // Recipe 3: Direct navigation without sort (fallback for pages without sort)
            CreateDirectRecipe(),

            // Recipe 4: URL-based sort (uses ?s=r parameter)
            CreateUrlSortRecipe()
        };

        await context.ScraperRecipes.AddRangeAsync(recipes);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Full workflow matching PlaywrightHttpClient.GetBetExplorerMultiSeasonResultsAsync:
    /// 1. Navigate to results page
    /// 2. Click sort dropdown and select "round"
    /// 3. Click "Show more" repeatedly
    /// 4. Extract HTML
    /// </summary>
    private static ScraperRecipe CreateFullWorkflowRecipe()
    {
        var actions = new object[]
        {
            // 1. Navigate to season results page
            new { type = "navigate", url = "{baseUrl}{season}/results/" },
            new { type = "waitForLoadState", state = "networkidle", timeout = 30000 },
            new { type = "wait", milliseconds = 1000 },

            // 2. Click sort dropdown to open it
            new { type = "click", selector = "#js-leagueresults-sort + div.select" },
            new { type = "wait", milliseconds = 500 },

            // 3. Select "round" option (li with rel="r")
            new { type = "click", selector = "li[rel='r']" },
            new { type = "wait", milliseconds = 2000 },
            new { type = "waitForLoadState", state = "networkidle", timeout = 15000 },

            // 4. Click "Show more" up to 10 times (for leagues with many rounds)
            // Each click loads more results
            new { type = "evaluate", script = @"
                (async () => {
                    for (let i = 0; i < 10; i++) {
                        // Find 'Show more' link by text content
                        const links = Array.from(document.querySelectorAll('a'));
                        const showMore = links.find(a =>
                            a.textContent.toLowerCase().includes('show more') ||
                            a.classList.contains('show-more')
                        );
                        if (showMore && showMore.offsetParent !== null) {
                            showMore.click();
                            await new Promise(r => setTimeout(r, 1500));
                        } else {
                            break;
                        }
                    }
                })()
            " },
            new { type = "wait", milliseconds = 1000 },

            // 5. Extract the results table
            new { type = "extractHtml", selector = "table.table-main", maxLength = 1000000 }
        };

        return new ScraperRecipe
        {
            Name = "BetExplorer Full Workflow",
            Description = "Full workflow: navigate, sort by round, load all results with Show more, extract table",
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
    /// Sort by round only, without Show more clicking.
    /// Good for smaller leagues or when Show more isn't needed.
    /// </summary>
    private static ScraperRecipe CreateSortOnlyRecipe()
    {
        var actions = new object[]
        {
            // 1. Navigate to season results page
            new { type = "navigate", url = "{baseUrl}{season}/results/" },
            new { type = "waitForLoadState", state = "networkidle", timeout = 30000 },
            new { type = "wait", milliseconds = 1000 },

            // 2. Click sort dropdown to open it
            new { type = "click", selector = "#js-leagueresults-sort + div.select" },
            new { type = "wait", milliseconds = 500 },

            // 3. Select "round" option
            new { type = "click", selector = "li[rel='r']" },
            new { type = "wait", milliseconds = 2000 },
            new { type = "waitForLoadState", state = "networkidle", timeout = 15000 },

            // 4. Extract full page HTML (table might not have proper class on older pages)
            new { type = "extractHtml", maxLength = 1000000 }
        };

        return new ScraperRecipe
        {
            Name = "BetExplorer Sort Only",
            Description = "Sort by round and extract - no Show more clicking (for smaller leagues)",
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

    /// <summary>
    /// Direct navigation without sort clicking.
    /// Fallback for pages where sort dropdown doesn't exist.
    /// </summary>
    private static ScraperRecipe CreateDirectRecipe()
    {
        var actions = new object[]
        {
            // 1. Navigate directly to results page
            new { type = "navigate", url = "{baseUrl}{season}/results/" },
            new { type = "waitForLoadState", state = "networkidle", timeout = 30000 },

            // 2. Wait for table to appear
            new { type = "waitForSelector", selector = "table.table-main", timeout = 10000 },
            new { type = "wait", milliseconds = 1000 },

            // 3. Extract full page HTML
            new { type = "extractHtml", maxLength = 1000000 }
        };

        return new ScraperRecipe
        {
            Name = "BetExplorer Direct",
            Description = "Direct navigation without sort - for pages without sort dropdown",
            Provider = "betexplorer",
            PageType = "results",
            Priority = 3,
            IsActive = true,
            ActionsJson = JsonSerializer.Serialize(actions),
            RoundHeaderSelector = ".//th[contains(text(), 'Round')]",
            GroupPatternRegex = @"^(.+?)\s*-\s*(\d+)\.\s*Round$",
            MatchRowSelector = ".//tr[td[contains(@class, 'h-text-left')]]"
        };
    }

    /// <summary>
    /// URL-based sort using ?s=r parameter.
    /// Used when JavaScript-based sort doesn't work.
    /// </summary>
    private static ScraperRecipe CreateUrlSortRecipe()
    {
        var actions = new object[]
        {
            // 1. Navigate to results page with sort parameter in URL
            new { type = "navigate", url = "{baseUrl}{season}/results/?s=r" },
            new { type = "waitForLoadState", state = "networkidle", timeout = 30000 },

            // 2. Wait for table
            new { type = "waitForSelector", selector = "table.table-main", timeout = 10000 },
            new { type = "wait", milliseconds = 1000 },

            // 3. Extract full page HTML
            new { type = "extractHtml", maxLength = 1000000 }
        };

        return new ScraperRecipe
        {
            Name = "BetExplorer URL Sort",
            Description = "Uses ?s=r URL parameter for sorting - fallback when JS sort fails",
            Provider = "betexplorer",
            PageType = "results",
            Priority = 4,
            IsActive = true,
            ActionsJson = JsonSerializer.Serialize(actions),
            RoundHeaderSelector = ".//th[contains(text(), 'Round')]",
            GroupPatternRegex = @"^(.+?)\s*-\s*(\d+)\.\s*Round$",
            MatchRowSelector = ".//tr[td[contains(@class, 'h-text-left')]]"
        };
    }
}
