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

            // Navigate to URL and wait for network to be idle
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
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
            // Wait for the results container to appear
            try
            {
                await page.WaitForSelectorAsync("#js-leagueresults-all", new PageWaitForSelectorOptions
                {
                    Timeout = 10000
                });
                _logger.LogDebug("Results container loaded successfully");
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Timeout waiting for results container, continuing anyway...");
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
