using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Sazkomat.DataImport.Scrapers;

public class PlaywrightHttpClient : IHttpClient, IAsyncDisposable
{
    private readonly ILogger<PlaywrightHttpClient> _logger;
    private readonly Random _random = new();
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _initialized = false;

    private static readonly string[] UserAgents = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };

    public PlaywrightHttpClient(ILogger<PlaywrightHttpClient> logger)
    {
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        _logger.LogInformation("Initializing Playwright browser...");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });

        _initialized = true;
        _logger.LogInformation("Playwright browser initialized successfully");
    }

    public async Task<string> GetHtmlAsync(string url)
    {
        await EnsureInitializedAsync();

        if (_browser == null)
        {
            throw new InvalidOperationException("Browser is not initialized");
        }

        // Random delay between requests (2-5 seconds) to avoid rate limiting
        var delay = _random.Next(2000, 5001);
        await Task.Delay(delay);

        // Random user agent
        var userAgent = UserAgents[_random.Next(UserAgents.Length)];

        _logger.LogInformation("Fetching with Playwright: {Url}", url);

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = userAgent,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            var page = await context.NewPageAsync();

            // Navigate to URL and wait for Load (all scripts loaded, JS can render content)
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });

            // Extra wait for AJAX/lazy-loaded content (especially for Betano)
            if (url.Contains("betano.cz"))
            {
                _logger.LogInformation("Betano page detected, waiting extra time for AJAX data...");
                await Task.Delay(5000);  // Wait 5 seconds for background AJAX requests
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = 10000
                });
                _logger.LogInformation("Extra wait completed, proceeding to extract HTML");
            }

            // Additional wait for dynamic content to load (BetExplorer specific)
            // Try multiple selectors - BetExplorer changed HTML structure around 2011
            var selectors = new[]
            {
                "#js-leagueresults-all",  // Old format (pre-2011)
                "table.table-main",        // Table directly on page
                ".table-main"              // Any element with this class
            };

            var selectorFound = false;
            foreach (var selector in selectors)
            {
                try
                {
                    await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                    {
                        Timeout = 5000
                    });
                    _logger.LogDebug("Results container loaded using selector: {Selector}", selector);
                    selectorFound = true;
                    break;
                }
                catch (TimeoutException)
                {
                    _logger.LogDebug("Selector {Selector} not found, trying next...", selector);
                }
            }

            if (!selectorFound)
            {
                _logger.LogWarning("No results container found with any selector, continuing anyway...");
            }

            // Get the full HTML content
            var html = await page.ContentAsync();

            _logger.LogInformation("Successfully fetched {Length} bytes from {Url}", html.Length, url);

            return html;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary>
    /// Fetches HTML from Betano sport page after clicking on "Soutěže" (Competitions) tab
    /// This is needed because Betano loads league data dynamically after tab interaction
    /// </summary>
    public async Task<string> GetBetanoLeaguesHtmlAsync(string sportUrl)
    {
        await EnsureInitializedAsync();

        if (_browser == null)
        {
            throw new InvalidOperationException("Browser is not initialized");
        }

        // Random delay between requests (2-5 seconds) to avoid rate limiting
        var delay = _random.Next(2000, 5001);
        await Task.Delay(delay);

        // Random user agent
        var userAgent = UserAgents[_random.Next(UserAgents.Length)];

        _logger.LogInformation("Fetching Betano leagues with tab interaction: {Url}", sportUrl);

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = userAgent,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            var page = await context.NewPageAsync();

            // Navigate to sport page (e.g., /sport/fotbal/)
            await page.GotoAsync(sportUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            _logger.LogDebug("Page loaded, looking for 'Soutěže' tab...");

            // Try multiple selectors for the "Soutěže" (Competitions) tab
            var tabSelectors = new[]
            {
                "a[href*='/liga/']",  // Link containing /liga/
                "a:has-text('Soutěže')",  // Link with text "Soutěže"
                "a:has-text('soutěže')",  // Lowercase variant
                "[data-tab='leagues']",  // Potential data attribute
                ".tab-leagues",  // Potential class name
                "button:has-text('Soutěže')",  // Button variant
            };

            bool tabClicked = false;
            foreach (var selector in tabSelectors)
            {
                try
                {
                    var element = await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                    {
                        Timeout = 5000,
                        State = WaitForSelectorState.Visible
                    });

                    if (element != null)
                    {
                        _logger.LogInformation("Found 'Soutěže' tab with selector: {Selector}", selector);
                        await element.ClickAsync();
                        _logger.LogInformation("Clicked on 'Soutěže' tab");
                        tabClicked = true;
                        break;
                    }
                }
                catch (TimeoutException)
                {
                    // Try next selector
                    continue;
                }
            }

            if (!tabClicked)
            {
                _logger.LogWarning("Could not find 'Soutěže' tab, will try to extract data anyway");
            }
            else
            {
                // Wait for new content to load after clicking tab
                await Task.Delay(2000);

                // Wait for network to be idle after tab click
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = 10000
                });

                _logger.LogDebug("Tab content loaded successfully");
            }

            // Get the full HTML content
            var html = await page.ContentAsync();

            _logger.LogInformation("Successfully fetched {Length} bytes from Betano", html.Length);

            return html;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary>
    /// Fetches HTML from BetExplorer results page.
    /// Navigates to the URL and clicks "Sort by round" button if available.
    /// URL should be in format: /football/hungary/otp-bank-liga-2020-2021/results/
    /// </summary>
    /// <param name="url">Full URL to the results page</param>
    /// <param name="season">Unused - kept for interface compatibility</param>
    /// <param name="debugSavePath">Optional path to save HTML for debugging</param>
    public async Task<string> GetBetExplorerResultsHtmlAsync(string url, string? season = null, string? debugSavePath = null)
    {
        await EnsureInitializedAsync();

        if (_browser == null)
        {
            throw new InvalidOperationException("Browser is not initialized");
        }

        // Random delay between requests (2-5 seconds) to avoid rate limiting
        var delay = _random.Next(2000, 5001);
        await Task.Delay(delay);

        // Random user agent
        var userAgent = UserAgents[_random.Next(UserAgents.Length)];

        _logger.LogInformation("Fetching BetExplorer results: {Url}", url);

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = userAgent,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            var page = await context.NewPageAsync();

            // Navigate directly to the results URL
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            _logger.LogDebug("Page loaded, checking for 'Sort by round' control...");

            // Click on "Sort by: round" if available
            var sortSelectors = new[]
            {
                "a:has-text('round')",
                "button:has-text('round')",
                "[data-sort='round']",
                ".sort-round",
                "span:has-text('round')"
            };

            bool sortClicked = false;
            foreach (var selector in sortSelectors)
            {
                try
                {
                    var element = await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                    {
                        Timeout = 3000,
                        State = WaitForSelectorState.Visible
                    });

                    if (element != null)
                    {
                        _logger.LogInformation("Found 'Sort by round' with selector: {Selector}", selector);
                        await element.ClickAsync();
                        sortClicked = true;

                        // Wait for content to reload after sorting
                        await Task.Delay(2000);
                        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                        {
                            Timeout = 10000
                        });
                        break;
                    }
                }
                catch (TimeoutException)
                {
                    continue;
                }
            }

            if (!sortClicked)
            {
                _logger.LogDebug("'Sort by round' not found, using default page content");
            }

            // Wait for results content
            var contentSelectors = new[]
            {
                "#js-leagueresults-all",
                "table.table-main",
                ".table-main"
            };

            foreach (var selector in contentSelectors)
            {
                try
                {
                    await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                    {
                        Timeout = 5000
                    });
                    _logger.LogDebug("Found results with selector: {Selector}", selector);
                    break;
                }
                catch (TimeoutException)
                {
                    continue;
                }
            }

            // Get the full HTML content
            var html = await page.ContentAsync();

            // Debug: save HTML to file if path provided
            if (!string.IsNullOrEmpty(debugSavePath))
            {
                try
                {
                    await File.WriteAllTextAsync(debugSavePath, html);
                    _logger.LogInformation("Debug HTML saved to: {Path}", debugSavePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save debug HTML to: {Path}", debugSavePath);
                }
            }

            _logger.LogInformation("Successfully fetched {Length} bytes from BetExplorer", html.Length);

            return html;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary>
    /// Scrapes multiple seasons from BetExplorer in a single browser session.
    /// More efficient - loads page once, then iterates through seasons via dropdown.
    /// </summary>
    /// <param name="baseLeagueUrl">Base league URL like /football/hungary/nb-i/</param>
    /// <param name="seasons">Seasons to scrape, e.g. ["2020-2021", "2019-2020"]</param>
    /// <param name="debugPathPattern">Optional pattern for debug HTML files, use {season} placeholder</param>
    public async IAsyncEnumerable<(string season, string html)> GetBetExplorerMultiSeasonResultsAsync(
        string baseLeagueUrl,
        IEnumerable<string> seasons,
        string? debugPathPattern = null)
    {
        await EnsureInitializedAsync();

        if (_browser == null)
        {
            throw new InvalidOperationException("Browser is not initialized");
        }

        // Random user agent
        var userAgent = UserAgents[_random.Next(UserAgents.Length)];

        _logger.LogInformation("Starting multi-season scrape from: {BaseUrl}", baseLeagueUrl);

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = userAgent,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        try
        {
            var page = await context.NewPageAsync();

            // 1. Load base URL ONCE
            var fullBaseUrl = baseLeagueUrl.StartsWith("http")
                ? baseLeagueUrl
                : $"https://www.betexplorer.com{baseLeagueUrl}";

            await page.GotoAsync(fullBaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            _logger.LogInformation("Base page loaded: {Url}", fullBaseUrl);

            // 2. Iterate through seasons
            foreach (var season in seasons)
            {
                // Random delay between seasons
                var delay = _random.Next(2000, 4001);
                await Task.Delay(delay);

                var seasonText = season.Replace("-", "/"); // "2020-2021" → "2020/2021"
                var seasonSlug = season.Replace("/", "-"); // "2020/2021" → "2020-2021"
                _logger.LogInformation("Selecting season: {SeasonText} (slug: {SeasonSlug})", seasonText, seasonSlug);

                // BetExplorer uses a custom dropdown with hidden <select> and visible <div class="select">
                // The <select> has options with href values, we can use JavaScript to navigate directly

                var seasonSelected = false;

                try
                {
                    // Strategy 1: Use JavaScript to find the select option and navigate
                    // The select element has options with value="/football/country/league-YYYY-YYYY/"
                    var optionValue = await page.EvaluateAsync<string?>($@"
                        (() => {{
                            const select = document.querySelector('.wrap-section__header__select select');
                            if (!select) return null;
                            for (const option of select.options) {{
                                if (option.text.includes('{seasonText}')) {{
                                    return option.value;
                                }}
                            }}
                            return null;
                        }})()
                    ");

                    if (!string.IsNullOrEmpty(optionValue))
                    {
                        _logger.LogInformation("Found season URL via JavaScript: {Url}", optionValue);

                        // Navigate directly to the season URL
                        var seasonUrl = optionValue.StartsWith("http")
                            ? optionValue
                            : $"https://www.betexplorer.com{optionValue}";

                        await page.GotoAsync(seasonUrl, new PageGotoOptions
                        {
                            WaitUntil = WaitUntilState.NetworkIdle,
                            Timeout = 30000
                        });

                        _logger.LogInformation("Navigated to season page: {Url}", page.Url);
                        seasonSelected = true;
                    }
                    else
                    {
                        _logger.LogWarning("Season {Season} not found in dropdown options", season);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "JavaScript navigation failed for season {Season}", season);
                }

                // Fallback: Try clicking on custom dropdown
                if (!seasonSelected)
                {
                    try
                    {
                        // Click on dropdown trigger to open it
                        var dropdownTrigger = await page.WaitForSelectorAsync(
                            ".wrap-section__header__select .select li, .wrap-section__header__select .select",
                            new PageWaitForSelectorOptions { Timeout = 3000 });

                        if (dropdownTrigger != null)
                        {
                            await dropdownTrigger.ClickAsync();
                            await Task.Delay(500); // Wait for dropdown animation

                            // Find and click the season option
                            var seasonOption = await page.WaitForSelectorAsync(
                                $".wrap-section__header__select li:has-text('{seasonText}')",
                                new PageWaitForSelectorOptions { Timeout = 3000, State = WaitForSelectorState.Visible });

                            if (seasonOption != null)
                            {
                                await seasonOption.ClickAsync();
                                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                                {
                                    Timeout = 15000
                                });
                                seasonSelected = true;
                                _logger.LogInformation("Selected season via dropdown click: {Url}", page.Url);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Dropdown click fallback failed");
                    }
                }

                if (!seasonSelected)
                {
                    _logger.LogWarning("Could not select season {Season} with any strategy, skipping...", season);
                    continue;
                }

                // 2b. Click on "Results" tab
                // Important: We need to find the league-specific Results link, not the global /football/results/
                _logger.LogInformation("Looking for Results tab on: {Url}", page.Url);
                var resultsClicked = false;

                // Extract current path to build expected results URL pattern
                var currentUri = new Uri(page.Url);
                var currentPath = currentUri.AbsolutePath.TrimEnd('/');
                var expectedResultsPath = currentPath + "/results/";
                _logger.LogDebug("Expected Results path: {Path}", expectedResultsPath);

                try
                {
                    // Wait a bit for page to fully load
                    await Task.Delay(1000);

                    // Strategy 1: Find link with exact season results path
                    var resultsSelectors = new[]
                    {
                        $"a[href='{expectedResultsPath}']",
                        $"a[href$='{expectedResultsPath}']",  // ends with
                        $".list-tabs a[href*='/results/']",    // in tab list
                        $".wrap-section a:has-text('Results')", // with text Results in section
                        $"a:has-text('Results'):not([href='/football/results/'])" // text Results but not global
                    };

                    foreach (var selector in resultsSelectors)
                    {
                        try
                        {
                            var resultsTab = await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                            {
                                Timeout = 2000,
                                State = WaitForSelectorState.Visible
                            });

                            if (resultsTab != null)
                            {
                                var href = await resultsTab.GetAttributeAsync("href");

                                // Verify it's the league-specific results, not global
                                if (href != null && !href.Equals("/football/results/") && href.Contains("/results"))
                                {
                                    _logger.LogInformation("Found league Results tab with selector '{Selector}', href: {Href}", selector, href);
                                    await resultsTab.ClickAsync();
                                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                                    {
                                        Timeout = 15000
                                    });
                                    _logger.LogInformation("After Results click, URL is: {Url}", page.Url);
                                    resultsClicked = true;
                                    break;
                                }
                            }
                        }
                        catch (TimeoutException)
                        {
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error finding Results tab for season {Season}", season);
                }

                if (!resultsClicked)
                {
                    _logger.LogWarning("Skipping season {Season} - league Results tab not found", season);
                    continue;
                }

                // 2c. Click on "Main" tab if stage tabs exist (e.g., "Main", "Winners stage", "Relegation")
                // This is common in older seasons like 2001-2002
                try
                {
                    // Look for stage/phase tabs within the results table
                    // These are typically in a sub-navigation or tab row
                    var mainTabSelectors = new[]
                    {
                        "a:has-text('Main'):not([href*='/results/'])",  // Text "Main" but not Results link
                        ".table-tabs a:has-text('Main')",               // Tab in table navigation
                        ".list-tabs--secondary a:has-text('Main')",     // Secondary tab list
                        "[class*='stage'] a:has-text('Main')",          // Stage navigation
                        "a[href*='stage=main']",                        // URL with stage parameter
                    };

                    foreach (var selector in mainTabSelectors)
                    {
                        var mainTab = await page.QuerySelectorAsync(selector);
                        if (mainTab != null)
                        {
                            _logger.LogInformation("Found 'Main' stage tab, clicking...");
                            await mainTab.ClickAsync();
                            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("No 'Main' stage tab found or click failed: {Message}", ex.Message);
                    // Continue - not all seasons have stage tabs
                }

                // 2d. Click "Sort by round" if available
                var sortSelectors = new[]
                {
                    "a:has-text('round')",
                    "span:has-text('round')"
                };

                foreach (var selector in sortSelectors)
                {
                    try
                    {
                        var sortElement = await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                        {
                            Timeout = 3000,
                            State = WaitForSelectorState.Visible
                        });

                        if (sortElement != null)
                        {
                            _logger.LogDebug("Clicking Sort by round...");
                            await sortElement.ClickAsync();
                            await Task.Delay(2000);
                            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                            {
                                Timeout = 10000
                            });
                            break;
                        }
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                }

                // 2d. Get HTML content
                _logger.LogInformation("Final URL before fetching HTML: {Url}", page.Url);
                var html = await page.ContentAsync();
                _logger.LogInformation("Fetched {Length} bytes for season {Season}", html.Length, season);

                // Save debug HTML if pattern provided
                if (!string.IsNullOrEmpty(debugPathPattern))
                {
                    var debugPath = debugPathPattern.Replace("{season}", season);
                    try
                    {
                        await File.WriteAllTextAsync(debugPath, html);
                        _logger.LogDebug("Debug HTML saved to: {Path}", debugPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save debug HTML");
                    }
                }

                yield return (season, html);

                // 2e. Go back to base page for next season
                _logger.LogDebug("Navigating back to base page...");
                await page.GotoAsync(fullBaseUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });
            }
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;

        _initialized = false;

        GC.SuppressFinalize(this);
    }
}
