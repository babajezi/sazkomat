using Microsoft.Playwright;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Sazkomat.Tests.Scrapers;

/// <summary>
/// Exploratory tests for analyzing Tipsport.cz page structure
/// These are not regular unit tests - they are for manual analysis
/// </summary>
[Trait("Category", "Manual")]
[Trait("Type", "Exploration")]
public class TipsportAnalysisTests
{
    private readonly ITestOutputHelper _output;

    public TipsportAnalysisTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual exploration test - requires Tipsport access")]
    public async Task AnalyzeTipsportPageStructure()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await context.NewPageAsync();

        // Capture API requests - save immediately to files
        var apiCounter = 0;

        page.Response += async (_, response) =>
        {
            var url = response.Url;
            var contentType = response.Headers.TryGetValue("content-type", out var ct) ? ct : "";

            if (url.Contains("api") || url.Contains("rest") || url.Contains("json") ||
                contentType.Contains("application/json"))
            {
                try
                {
                    var body = await response.TextAsync();
                    _output.WriteLine($"[API] {url} - {body.Length} bytes");

                    // Save immediately to file
                    var safeFileName = $"/tmp/tipsport_api_{apiCounter++}_{Uri.EscapeDataString(new Uri(url).AbsolutePath.Replace("/", "_"))}.json";
                    await File.WriteAllTextAsync(safeFileName, $"// URL: {url}\n// Content-Type: {contentType}\n// Length: {body.Length}\n\n{body}");
                    _output.WriteLine($"  Saved to: {safeFileName}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"[API] {url} - Error: {ex.Message}");
                }
            }
        };

        _output.WriteLine("Navigating to Tipsport...");

        // Use DOMContentLoaded instead of NetworkIdle to avoid timeout
        await page.GotoAsync("https://www.tipsport.cz/vysledky", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        // Wait a bit for API calls to complete
        _output.WriteLine("Waiting for API calls...");
        await Task.Delay(15000);

        _output.WriteLine("Page loaded, waiting for content...");
        await Task.Delay(5000);

        // Try to click on Fotbal
        try
        {
            var fotbalLink = await page.WaitForSelectorAsync("text=Fotbal", new PageWaitForSelectorOptions { Timeout = 10000 });
            if (fotbalLink != null)
            {
                await fotbalLink.ClickAsync();
                _output.WriteLine("Clicked on Fotbal");
                await Task.Delay(3000);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Could not click Fotbal: {ex.Message}");
        }

        // Get HTML
        var html = await page.ContentAsync();
        _output.WriteLine($"\nHTML size: {html.Length} bytes");

        // Search for embedded JSON
        var jsonPatterns = new[]
        {
            (@"window\.__INITIAL_STATE__\s*=\s*(\{.+?\});", "INITIAL_STATE"),
            (@"window\[""initial_state""\]\s*=\s*(\{.+?\})\s*</script>", "initial_state"),
            (@"<script[^>]*type=""application/json""[^>]*>(\{.+?\})</script>", "script json"),
            (@"window\.APP_STATE\s*=\s*(\{.+?\});", "APP_STATE"),
        };

        _output.WriteLine("\n=== Searching for embedded JSON ===");
        foreach (var (pattern, name) in jsonPatterns)
        {
            var match = Regex.Match(html, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                _output.WriteLine($"Found JSON with pattern: {name}");
                var json = match.Groups[1].Value;
                _output.WriteLine($"JSON preview (first 1000 chars): {json.Substring(0, Math.Min(1000, json.Length))}...");

                // Save to file
                await File.WriteAllTextAsync($"/tmp/tipsport_{name}.json", json);
            }
        }

        // Note: API requests are captured and saved in the Response handler above
        _output.WriteLine($"\n=== API requests captured to files in /tmp/ ===");

        // Look for league elements in DOM
        _output.WriteLine("\n=== Looking for league elements in DOM ===");

        var selectors = new[]
        {
            "[class*='league']",
            "[class*='competition']",
            "[class*='Liga']",
            "[data-sport]",
            "[data-competition]",
            ".sport-menu",
            ".left-menu",
            ".sidebar",
        };

        foreach (var selector in selectors)
        {
            try
            {
                var elements = await page.QuerySelectorAllAsync(selector);
                if (elements.Count > 0)
                {
                    _output.WriteLine($"Selector '{selector}': {elements.Count} elements");

                    // Get first few element contents
                    for (int i = 0; i < Math.Min(3, elements.Count); i++)
                    {
                        var text = await elements[i].TextContentAsync();
                        var outerHtml = await elements[i].EvaluateAsync<string>("el => el.outerHTML.substring(0, 300)");
                        _output.WriteLine($"  [{i}] Text: {text?.Substring(0, Math.Min(100, text?.Length ?? 0))}");
                        _output.WriteLine($"  [{i}] HTML: {outerHtml}");
                    }
                }
            }
            catch { }
        }

        // Save full HTML
        await File.WriteAllTextAsync("/tmp/tipsport_vysledky.html", html);
        _output.WriteLine("\nSaved HTML to /tmp/tipsport_vysledky.html");

        // API requests are saved directly by Response handler to /tmp/tipsport_api_*.json
        _output.WriteLine("API requests saved to /tmp/tipsport_api_*.json files");

        await context.CloseAsync();
    }

    [Fact(Skip = "Manual exploration test - run manually when needed")]
    public async Task AnalyzeTipsportLeftMenu()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false, // Show browser for debugging
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync("https://www.tipsport.cz/vysledky", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60000
        });

        await Task.Delay(3000);

        // Click on Fotbal and expand the menu
        var fotbalSelector = await page.QuerySelectorAsync("text=Fotbal");
        if (fotbalSelector != null)
        {
            await fotbalSelector.ClickAsync();
            await Task.Delay(2000);
        }

        // Get all league items from the left menu
        var menuItems = await page.QuerySelectorAllAsync(".left-menu a, .sidebar a, [class*='menu'] a");
        _output.WriteLine($"Found {menuItems.Count} menu items");

        var leagues = new List<string>();
        foreach (var item in menuItems)
        {
            var text = await item.TextContentAsync();
            var href = await item.GetAttributeAsync("href");
            if (!string.IsNullOrWhiteSpace(text))
            {
                leagues.Add($"{text?.Trim()} -> {href}");
                _output.WriteLine($"Menu item: {text?.Trim()} -> {href}");
            }
        }

        await File.WriteAllLinesAsync("/tmp/tipsport_menu_items.txt", leagues);

        // Keep browser open for manual inspection
        _output.WriteLine("\nBrowser is open for inspection. Close it manually when done.");
        await Task.Delay(60000); // Keep open for 1 minute

        await context.CloseAsync();
    }
}
