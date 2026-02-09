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
        // 5. Two Stage - auto-detects stage tabs (First/Second Stage, Apertura/Clausura, etc.)
        var newRecipes = new List<ScraperRecipe>
        {
            CreateFullWorkflowRecipe(),        // Priority 1 - full workflow via dropdown
            CreateDirectSortRecipe(),          // Priority 2 - direct URL fallback
            CreateTwoStageRecipe(),            // Priority 5 - auto-detect two stage tabs
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
                    existing.RequiresHint = newRecipe.RequiresHint;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // New recipe - add it
                    await context.ScraperRecipes.AddAsync(newRecipe);
                }
            }

            // Remove old recipes that are no longer needed
            var oldRecipeNames = new[] { "BetExplorer URL Sort", "BetExplorer Direct", "BetExplorer Sort Only", "BetExplorer First/Second Stage" };
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
        // Note: tabs have class "list-tabs__item__in"
        const string clickMainTabScript = @"
            (() => {
                // Look for stage tabs - BetExplorer uses list-tabs__item__in class
                const stageTabs = document.querySelectorAll('.list-tabs__item__in, .list-tabs a, .stages a, [class*=""stage""] a');
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

        // JavaScript to detect if page has supported stage tabs (hint for Two Stage recipe)
        const string detectStageTabsHintScript = @"
            (() => {
                const tabs = Array.from(document.querySelectorAll('.list-tabs__item__in, .list-tabs a'));
                const visibleTexts = tabs
                    .filter(t => t.offsetParent)
                    .map(t => t.textContent.trim().toLowerCase());

                const knownPairs = [
                    ['first stage', 'second stage'],
                    ['apertura', 'clausura']
                ];

                for (const [a, b] of knownPairs) {
                    if (visibleTexts.some(t => t.includes(a)) && visibleTexts.some(t => t.includes(b))) {
                        return 'true';
                    }
                }
                return 'false';
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

            // 5b. Detect stage tabs and store hint for Two Stage recipe
            new { type = "evaluate", script = detectStageTabsHintScript, storeAs = "hasStageTabsHint" },

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

    /// <summary>
    /// Generic recipe for leagues with two stage tabs (e.g., First Stage/Second Stage, Apertura/Clausura).
    /// Auto-detects stage tabs and collects content from both stages with markers.
    /// Priority 5 = fallback when other recipes fail (no "Main" tab).
    /// </summary>
    private static ScraperRecipe CreateTwoStageRecipe()
    {
        // JavaScript to find season URL from dropdown
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

        // JavaScript to detect and store stage tab names
        // Finds tabs that are actual stages (not navigation like Summary, Results, etc.)
        const string detectStageTabsScript = @"
            (() => {
                const tabs = Array.from(document.querySelectorAll('.list-tabs__item__in, .list-tabs a'));
                const excludePatterns = ['summary', 'results', 'fixtures', 'stats', 'head-to-head', 'h2h', 'play offs', 'play-offs', 'playoff'];

                const stageTabs = tabs.filter(tab => {
                    const text = tab.textContent.trim().toLowerCase();
                    // Exclude navigation tabs and playoff tabs
                    if (excludePatterns.some(p => text.includes(p))) return false;
                    // Must be visible
                    if (!tab.offsetParent) return false;
                    // Should have reasonable length (not empty, not too long)
                    if (text.length < 2 || text.length > 30) return false;
                    return true;
                }).map(tab => tab.textContent.trim());

                // Store first two stage names for later use
                if (stageTabs.length >= 2) {
                    sessionStorage.setItem('__stage1Name', stageTabs[0]);
                    sessionStorage.setItem('__stage2Name', stageTabs[1]);
                    return 'found stages: ' + stageTabs[0] + ', ' + stageTabs[1];
                }
                return 'not enough stages found: ' + stageTabs.join(', ');
            })()";

        // JavaScript to click first detected stage tab
        const string clickFirstStageScript = @"
            (() => {
                const stageName = sessionStorage.getItem('__stage1Name');
                if (!stageName) return 'no stage1 stored';

                const tabs = document.querySelectorAll('.list-tabs__item__in, .list-tabs a');
                for (const tab of tabs) {
                    if (tab.textContent.trim() === stageName) {
                        tab.click();
                        return 'clicked: ' + stageName;
                    }
                }
                return 'not found: ' + stageName;
            })()";

        // JavaScript to click second detected stage tab
        const string clickSecondStageScript = @"
            (() => {
                const stageName = sessionStorage.getItem('__stage2Name');
                if (!stageName) return 'no stage2 stored';

                const tabs = document.querySelectorAll('.list-tabs__item__in, .list-tabs a');
                for (const tab of tabs) {
                    if (tab.textContent.trim() === stageName) {
                        tab.click();
                        return 'clicked: ' + stageName;
                    }
                }
                return 'not found: ' + stageName;
            })()";

        // JavaScript to sort by round
        const string sortByRoundScript = @"
            (() => {
                const dropdown = document.querySelector('#js-leagueresults-sort + div.select');
                if (dropdown) {
                    dropdown.click();
                    setTimeout(() => {
                        const roundOption = document.querySelector('#js-leagueresults-sort + div.select li[rel=""r""]');
                        if (roundOption) roundOption.click();
                    }, 500);
                    return 'dropdown';
                }
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

        // Store first stage content - use sessionStorage (persists across page navigations)
        // Uses detected stage name from __stage1Name
        const string storeFirstStageScript = @"
            (() => {
                const stageName = sessionStorage.getItem('__stage1Name') || 'Stage 1';
                const table = document.querySelector('.table-main');
                if (!table) {
                    sessionStorage.setItem('__stageContent', '');
                    return 'no table found';
                }
                const content = '<!-- STAGE: ' + stageName + ' -->\n' + table.outerHTML;
                sessionStorage.setItem('__stageContent', content);
                return 'stored ' + stageName + ': ' + content.length + ' chars';
            })()";

        // Append second stage content and inject into page
        // Uses detected stage name from __stage2Name
        const string appendSecondStageAndInjectScript = @"
            (() => {
                const stageName = sessionStorage.getItem('__stage2Name') || 'Stage 2';
                const table = document.querySelector('.table-main');
                if (!table) return 'no table found';

                const firstStage = sessionStorage.getItem('__stageContent') || '';
                const secondStage = '<!-- STAGE: ' + stageName + ' -->\n' + table.outerHTML;
                const combined = firstStage + '\n' + secondStage;

                // Clear sessionStorage after use
                sessionStorage.removeItem('__stageContent');
                sessionStorage.removeItem('__stage1Name');
                sessionStorage.removeItem('__stage2Name');

                // Create visible container with combined content
                const container = document.createElement('div');
                container.id = 'combined-stages';
                container.innerHTML = combined;
                document.body.innerHTML = '';
                document.body.appendChild(container);

                return 'combined ' + combined.length + ' chars';
            })()";

        var actions = new object[]
        {
            // 1. Navigate to base league URL
            new { type = "navigate", url = "{baseUrl}" },
            new { type = "waitForLoadState", state = "load", timeout = 30000 },
            new { type = "wait", milliseconds = 1000 },

            // 2. Find season URL from dropdown
            new { type = "evaluate", script = findSeasonUrlScript, storeAs = "seasonUrl" },

            // 3. Navigate to season URL
            new { type = "navigateToVariable", variable = "seasonUrl" },
            new { type = "waitForLoadState", state = "load", timeout = 30000 },
            new { type = "wait", milliseconds = 1000 },

            // 4. Click Results tab
            new { type = "evaluate", script = clickResultsTabScript },
            new { type = "waitForLoadState", state = "networkidle", timeout = 15000 },
            new { type = "wait", milliseconds = 1000 },

            // 5. Auto-detect stage tabs (stores names in sessionStorage)
            new { type = "evaluate", script = detectStageTabsScript },

            // === FIRST STAGE ===
            // 6. Click first detected stage tab
            new { type = "evaluate", script = clickFirstStageScript },
            new { type = "wait", milliseconds = 2000 },
            new { type = "waitForLoadState", state = "networkidle", timeout = 15000 },

            // 6. Sort by round
            new { type = "evaluate", script = sortByRoundScript },
            new { type = "wait", milliseconds = 2000 },
            new { type = "waitForLoadState", state = "networkidle", timeout = 15000 },

            // 7. Show More
            new { type = "evaluate", script = showMoreScript },
            new { type = "wait", milliseconds = 1000 },

            // 8. Store First Stage content in JS variable
            new { type = "evaluate", script = storeFirstStageScript },

            // === SECOND STAGE ===
            // 9. Click Second Stage tab
            new { type = "evaluate", script = clickSecondStageScript },
            new { type = "wait", milliseconds = 2000 },
            new { type = "waitForLoadState", state = "networkidle", timeout = 15000 },

            // 10. Sort by round
            new { type = "evaluate", script = sortByRoundScript },
            new { type = "wait", milliseconds = 2000 },
            new { type = "waitForLoadState", state = "networkidle", timeout = 15000 },

            // 11. Show More
            new { type = "evaluate", script = showMoreScript },
            new { type = "wait", milliseconds = 1000 },

            // 12. Append Second Stage and inject combined content into page body
            new { type = "evaluate", script = appendSecondStageAndInjectScript },

            // 13. Extract full page HTML (now contains only combined stages)
            new { type = "extractHtml", maxLength = 2000000 }
        };

        return new ScraperRecipe
        {
            Name = "BetExplorer Two Stage",
            Description = "Auto-detects two stage tabs (First/Second Stage, Apertura/Clausura, etc.) - Priority 5",
            Provider = "betexplorer",
            PageType = "results",
            Priority = 5,
            IsActive = true,
            RequiresHint = "hasStageTabsHint",
            ActionsJson = JsonSerializer.Serialize(actions),
            RoundHeaderSelector = ".//th[contains(text(), 'Round')]",
            GroupPatternRegex = @"^(.+?)\s*-\s*(\d+)\.\s*Round$",
            MatchRowSelector = ".//tr[td[contains(@class, 'h-text-left')]]"
        };
    }
}
