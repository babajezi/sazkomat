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
    /// <summary>
    /// Whitelist of stage tab names known to contain rounds (not knockout brackets).
    /// Shared between Full Workflow (hint detection) and Stage Tabs recipe (tab selection).
    /// </summary>
    private const string KnownStageNamesJs = @"['first stage', 'second stage', 'apertura', 'clausura', 'taca guanabara', 'taca rio', 'opening stage', 'closing stage', 'phase 1', 'phase 2', 'primera fase', 'segunda fase', 'first phase', 'second phase']";

    public static async Task SeedDefaultRecipesAsync(DataImportDbContext context)
    {
        var existingRecipes = await context.ScraperRecipes
            .Where(r => r.Provider == "betexplorer")
            .ToListAsync();

        // Priority order (most reliable first):
        // 1. Full Workflow - navigate via dropdown, click Results, sort by round, Show More
        // 5. Stage Tabs - processes known stage tabs (First/Second Stage, Apertura/Clausura, etc.)
        var newRecipes = new List<ScraperRecipe>
        {
            CreateFullWorkflowRecipe(),        // Priority 1 - full workflow via dropdown
            CreateStageTabsRecipe(),           // Priority 5 - whitelist-based stage tabs
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
            var oldRecipeNames = new[] { "BetExplorer URL Sort", "BetExplorer Direct", "BetExplorer Sort Only", "BetExplorer First/Second Stage", "BetExplorer Direct Sort", "BetExplorer Two Stage" };
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

        // JavaScript to find Results tab URL (returns URL for navigateToVariable)
        // Using navigateToVariable instead of click() to avoid potential context destruction
        const string findResultsTabUrlScript = @"
            (() => {
                const links = Array.from(document.querySelectorAll('a'));
                const resultsLink = links.find(a => {
                    const href = a.getAttribute('href') || '';
                    return href.includes('/results') && !href.includes('/football/results/') && a.offsetParent !== null;
                });
                return resultsLink ? resultsLink.href : null;
            })()";

        // JavaScript to find Main stage tab URL (if exists and not already selected)
        // Some leagues have multiple stages (Main, Playoff, Relegation, etc.)
        // We want to navigate to Main to get all regular season rounds
        // Returns the full URL for navigateToVariable (avoids context destruction from click())
        // Note: tabs have class "list-tabs__item__in", selected tab has "current" class
        const string findMainTabUrlScript = @"
            (() => {
                const stageTabs = document.querySelectorAll('.list-tabs__item__in, .list-tabs a, .stages a, [class*=""stage""] a');
                for (const tab of stageTabs) {
                    const text = tab.textContent.toLowerCase().trim();
                    if ((text === 'main' || text === 'all' || text.includes('main stage')) && tab.offsetParent !== null) {
                        // Check if already selected (BetExplorer uses 'current' class)
                        if (!tab.classList.contains('active') && !tab.classList.contains('current') &&
                            !tab.parentElement?.classList.contains('active') && !tab.parentElement?.classList.contains('current')) {
                            return tab.href || null;
                        }
                        return null; // Already selected
                    }
                }
                return null; // No main tab found
            })()";

        // JavaScript to detect if page has whitelisted stage tabs (hint for Stage Tabs recipe)
        const string detectStageTabsHintScript = @"
            (() => {
                const knownStageNames = " + KnownStageNamesJs + @";
                const tabs = Array.from(document.querySelectorAll('.list-tabs__item__in, .list-tabs a'));
                const found = tabs.filter(tab => {
                    if (!tab.offsetParent || !tab.href) return false;
                    const text = tab.textContent.trim().toLowerCase();
                    return knownStageNames.some(name => text.includes(name));
                });
                return found.length >= 1 ? 'true' : 'false';
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

            // 4. Navigate to Results tab via Playwright
            new { type = "evaluate", script = findResultsTabUrlScript, storeAs = "resultsUrl" },
            new { type = "navigateToVariable", variable = "resultsUrl" },
            new { type = "waitForLoadState", state = "load", timeout = 30000 },
            new { type = "wait", milliseconds = 1000 },

            // 5. Find Main stage tab URL (if exists) and navigate via Playwright
            // Using navigateToVariable instead of click() to avoid Playwright context destruction
            new { type = "evaluate", script = findMainTabUrlScript, storeAs = "mainStageUrl" },
            new { type = "navigateToVariable", variable = "mainStageUrl" },
            new { type = "waitForLoadState", state = "load", timeout = 30000 },
            new { type = "wait", milliseconds = 1000 },

            // 5b. Detect whitelisted stage tabs and store hint for Stage Tabs recipe
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
            RoundHeaderSelector = ".//th[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'round')]",
            GroupPatternRegex = @"^(.+?)\s*-\s*(\d+)\.\s*Round$",
            MatchRowSelector = ".//tr[td[contains(@class, 'h-text-left')]]"
        };
    }

    /// <summary>
    /// Stage Tabs recipe: processes known stage tabs that contain rounds.
    /// Uses a whitelist of tab names (First/Second Stage, Apertura/Clausura, Taca Guanabara, etc.)
    /// to identify tabs with round data, skipping knockout brackets (Taca Rio, Play Offs).
    /// Supports up to 4 stage tabs via slot-based actions.
    /// Priority 5 = fallback when Full Workflow detects stage tabs hint.
    /// </summary>
    private static ScraperRecipe CreateStageTabsRecipe()
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

        // JavaScript to find Results tab URL
        const string findResultsTabUrlScript = @"
            (() => {
                const links = Array.from(document.querySelectorAll('a'));
                const resultsLink = links.find(a => {
                    const href = a.getAttribute('href') || '';
                    return href.includes('/results') && !href.includes('/football/results/') && a.offsetParent !== null;
                });
                return resultsLink ? resultsLink.href : null;
            })()";

        // JavaScript to detect whitelisted stage tabs and store URLs in sessionStorage
        const string detectAndStoreStageTabsScript = @"
            (() => {
                const knownStageNames = " + KnownStageNamesJs + @";
                const tabs = Array.from(document.querySelectorAll('.list-tabs__item__in, .list-tabs a'));
                const seen = new Set();
                const stageTabs = tabs.filter(tab => {
                    if (!tab.offsetParent || !tab.href) return false;
                    if (seen.has(tab.href)) return false;
                    const text = tab.textContent.trim().toLowerCase();
                    if (!knownStageNames.some(name => text.includes(name))) return false;
                    seen.add(tab.href);
                    return true;
                });

                const count = Math.min(stageTabs.length, 4);
                for (let i = 0; i < count; i++) {
                    sessionStorage.setItem('__stageUrl' + (i + 1), stageTabs[i].href);
                    sessionStorage.setItem('__stageName' + (i + 1), stageTabs[i].textContent.trim());
                }
                sessionStorage.setItem('__stageCount', String(count));
                return 'found ' + count + ' stage tabs: ' + stageTabs.slice(0, count).map(t => t.textContent.trim()).join(', ');
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

        // Template: read stage URL from sessionStorage for slot N
        // Returns null when slot is unused (N > stageCount), causing navigateToVariable to skip
        const string getStageUrlTemplate = @"
            (() => {
                const count = parseInt(sessionStorage.getItem('__stageCount') || '0');
                if (SLOT_NUM > count) return null;
                return sessionStorage.getItem('__stageUrlSLOT_NUM');
            })()";

        // Template: store stage HTML into sessionStorage (cumulative) for slot N
        // Bails out early when slot is unused (N > stageCount)
        const string storeStageHtmlTemplate = @"
            (() => {
                const count = parseInt(sessionStorage.getItem('__stageCount') || '0');
                if (SLOT_NUM > count) return 'slot SLOT_NUM unused';
                const stageName = sessionStorage.getItem('__stageNameSLOT_NUM') || 'Stage SLOT_NUM';
                const table = document.querySelector('.table-main');
                if (!table) return 'no table found for ' + stageName;
                const prev = sessionStorage.getItem('__combinedHtml') || '';
                const stageHtml = '<!-- STAGE: ' + stageName + ' -->\n' + table.outerHTML;
                sessionStorage.setItem('__combinedHtml', prev + '\n' + stageHtml);
                return 'stored ' + stageName + ': ' + stageHtml.length + ' chars';
            })()";

        // Final: inject combined HTML from all stages into page body
        const string injectCombinedHtmlScript = @"
            (() => {
                const combined = sessionStorage.getItem('__combinedHtml') || '';
                const count = parseInt(sessionStorage.getItem('__stageCount') || '0');
                for (let i = 1; i <= count; i++) {
                    sessionStorage.removeItem('__stageUrl' + i);
                    sessionStorage.removeItem('__stageName' + i);
                }
                sessionStorage.removeItem('__stageCount');
                sessionStorage.removeItem('__combinedHtml');

                const container = document.createElement('div');
                container.id = 'combined-stages';
                container.innerHTML = combined;
                document.body.innerHTML = '';
                document.body.appendChild(container);
                return 'injected ' + combined.length + ' chars';
            })()";

        // Build actions list with slot-based approach for up to 4 stage tabs
        var actions = new List<object>
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

            // 4. Navigate to Results tab
            new { type = "evaluate", script = findResultsTabUrlScript, storeAs = "resultsUrl" },
            new { type = "navigateToVariable", variable = "resultsUrl" },
            new { type = "waitForLoadState", state = "load", timeout = 30000 },
            new { type = "wait", milliseconds = 1000 },

            // 5. Detect whitelisted stage tabs and store URLs in sessionStorage
            new { type = "evaluate", script = detectAndStoreStageTabsScript },
        };

        // 6. Slot-based actions for up to 4 stage tabs
        // Each slot: read URL -> navigate -> sort -> show more -> store HTML
        // Unused slots (N > stageCount) skip gracefully: navigate gets null, storeHtml bails out
        for (int n = 1; n <= 4; n++)
        {
            var getUrl = getStageUrlTemplate.Replace("SLOT_NUM", n.ToString());
            var storeHtml = storeStageHtmlTemplate.Replace("SLOT_NUM", n.ToString());
            var varName = $"stage{n}Url";

            actions.Add(new { type = "evaluate", script = getUrl, storeAs = varName });
            actions.Add(new { type = "navigateToVariable", variable = varName });
            actions.Add(new { type = "waitForLoadState", state = "load", timeout = 30000 });
            actions.Add(new { type = "wait", milliseconds = 1000 });
            actions.Add(new { type = "evaluate", script = sortByRoundScript });
            actions.Add(new { type = "wait", milliseconds = 2000 });
            actions.Add(new { type = "waitForLoadState", state = "networkidle", timeout = 15000 });
            actions.Add(new { type = "evaluate", script = showMoreScript });
            actions.Add(new { type = "wait", milliseconds = 1000 });
            actions.Add(new { type = "evaluate", script = storeHtml });
        }

        // 7. Inject combined HTML from all stages into page body and extract
        actions.Add(new { type = "evaluate", script = injectCombinedHtmlScript });
        actions.Add(new { type = "extractHtml", maxLength = 4000000 });

        return new ScraperRecipe
        {
            Name = "BetExplorer Stage Tabs",
            Description = "Processes known stage tabs that contain rounds (First/Second Stage, Apertura/Clausura, Taca Guanabara, etc.)",
            Provider = "betexplorer",
            PageType = "results",
            Priority = 5,
            IsActive = true,
            RequiresHint = "hasStageTabsHint",
            ActionsJson = JsonSerializer.Serialize(actions),
            RoundHeaderSelector = ".//th[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'round')]",
            GroupPatternRegex = @"^(.+?)\s*-\s*(\d+)\.\s*Round$",
            MatchRowSelector = ".//tr[td[contains(@class, 'h-text-left')]]"
        };
    }
}
