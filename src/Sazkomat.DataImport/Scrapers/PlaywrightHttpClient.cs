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

            // Additional wait for dynamic content to load
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
