using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

namespace Sazkomat.TipsportScraper;

/// <summary>
/// Standalone Tipsport scraper that runs outside Docker to bypass Cloudflare.
/// Run this locally, then it will push the data to the API.
///
/// Usage:
///   dotnet run -- [--headless] [--api-url http://localhost:3001]
///
/// Options:
///   --headless    Run in headless mode (may not work due to Cloudflare)
///   --api-url     API URL to push data to (default: http://localhost:3001)
/// </summary>
class Program
{
    private const string TipsportBaseUrl = "https://www.tipsport.cz";
    private const string TipsportApiEndpoint = "/rest/offer/v6/sports";
    private const string TipsportProviderId = "b0000000-0000-0000-0000-000000000004";

    static async Task<int> Main(string[] args)
    {
        var headless = args.Contains("--headless");
        var apiUrl = GetArgValue(args, "--api-url") ?? "http://localhost:3001";

        Console.WriteLine("===========================================");
        Console.WriteLine("  Sazkomat Tipsport Scraper (standalone)");
        Console.WriteLine("===========================================");
        Console.WriteLine($"Mode: {(headless ? "Headless" : "Visible browser")}");
        Console.WriteLine($"API URL: {apiUrl}");
        Console.WriteLine();

        try
        {
            // Step 1: Scrape Tipsport
            Console.WriteLine("[1/3] Launching browser and navigating to Tipsport...");
            var jsonData = await ScrapeWithPlaywright(headless);

            if (string.IsNullOrEmpty(jsonData))
            {
                Console.WriteLine("ERROR: Failed to capture JSON data from Tipsport");
                return 1;
            }

            Console.WriteLine($"      Captured {jsonData.Length:N0} bytes of JSON data");

            // Save raw JSON for debugging
            var jsonPath = Path.Combine(Environment.CurrentDirectory, "captured-tipsport.json");
            await File.WriteAllTextAsync(jsonPath, jsonData);
            Console.WriteLine($"      Saved raw JSON to: {jsonPath}");

            // Step 2: Parse and extract competitions
            Console.WriteLine("[2/3] Parsing competitions from JSON...");
            var competitions = ParseCompetitions(jsonData);
            Console.WriteLine($"      Found {competitions.Count} football competitions");

            if (competitions.Count == 0)
            {
                Console.WriteLine("WARNING: No competitions found in the data");
                return 1;
            }

            // Show sample
            Console.WriteLine("\n      Sample competitions:");
            foreach (var comp in competitions.Take(10))
            {
                Console.WriteLine($"        - {comp.Title} (Country: {comp.DerivedCountryCode ?? "Unknown"})");
            }
            if (competitions.Count > 10)
            {
                Console.WriteLine($"        ... and {competitions.Count - 10} more");
            }

            // Step 3: Push to API
            Console.WriteLine($"\n[3/3] Pushing {competitions.Count} leagues to API...");
            var pushResult = await PushToApi(apiUrl, competitions);

            if (pushResult)
            {
                Console.WriteLine("\nSUCCESS: Data pushed to API successfully!");
                Console.WriteLine($"         Now run a league scan from the UI to process the data.");
                return 0;
            }
            else
            {
                Console.WriteLine("\nERROR: Failed to push data to API");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static string? GetArgValue(string[] args, string key)
    {
        var index = Array.IndexOf(args, key);
        if (index >= 0 && index < args.Length - 1)
        {
            return args[index + 1];
        }
        return null;
    }

    static async Task<string?> ScrapeWithPlaywright(bool headless)
    {
        using var playwright = await Playwright.CreateAsync();

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo = headless ? 0 : 50  // Slow down if visible for debugging
        };

        await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "cs-CZ",
            TimezoneId = "Europe/Prague",
            JavaScriptEnabled = true,
            HasTouch = false,
            IsMobile = false,
            DeviceScaleFactor = 1
        });

        // Add stealth scripts to hide automation
        await context.AddInitScriptAsync(@"
            // Hide webdriver
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });

            // Hide automation
            window.chrome = { runtime: {} };

            // Fix permissions
            const originalQuery = window.navigator.permissions.query;
            window.navigator.permissions.query = (parameters) => (
                parameters.name === 'notifications' ?
                    Promise.resolve({ state: Notification.permission }) :
                    originalQuery(parameters)
            );

            // Hide plugins length
            Object.defineProperty(navigator, 'plugins', {
                get: () => [1, 2, 3, 4, 5]
            });

            // Hide languages
            Object.defineProperty(navigator, 'languages', {
                get: () => ['cs-CZ', 'cs', 'en-US', 'en']
            });
        ");

        var page = await context.NewPageAsync();

        // Capture API response
        string? capturedJson = null;

        page.Response += async (_, response) =>
        {
            var url = response.Url;
            // Only capture Tipsport API responses
            if (response.Status == 200 &&
                url.Contains("tipsport.cz/rest") &&
                response.Headers.TryGetValue("content-type", out var contentType) &&
                contentType.Contains("json"))
            {
                try
                {
                    var text = await response.TextAsync();
                    Console.WriteLine($"      [Tipsport API] {url.Substring(url.IndexOf("/rest"))} ({text.Length:N0} bytes)");

                    // Capture init-web or offer endpoints
                    if (url.Contains("/rest/offer/") || url.Contains("/rest/common/v1/init-web"))
                    {
                        // Keep the largest/most relevant response
                        if (capturedJson == null || text.Length > capturedJson.Length)
                        {
                            capturedJson = text;
                            Console.WriteLine($"      *** CAPTURED: {url.Substring(url.IndexOf("/rest"))} ***");
                        }
                    }
                }
                catch { }
            }
        };

        // Navigate to main page first to get cookies, then football
        Console.WriteLine("      Navigating to Tipsport main page...");
        await page.GotoAsync(TipsportBaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60000
        });

        Console.WriteLine("      Waiting 15 seconds for page to fully initialize...");
        await Task.Delay(15000);  // Let cookies, JS and anti-bot checks settle

        // Instead of direct navigation, click on football menu
        Console.WriteLine("      Looking for football menu on main page...");
        try
        {
            // Try clicking on "Fotbal" or sports menu
            var fotbalLink = await page.WaitForSelectorAsync("text=Fotbal", new PageWaitForSelectorOptions { Timeout = 10000 });
            if (fotbalLink != null)
            {
                Console.WriteLine("      Found 'Fotbal' link, clicking...");
                await fotbalLink.ClickAsync();
                Console.WriteLine("      Waiting 10 seconds after click...");
                await Task.Delay(10000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      Could not find Fotbal link: {ex.Message}");
            // Fallback to direct navigation
            Console.WriteLine("      Trying direct navigation...");
            await page.GotoAsync($"{TipsportBaseUrl}/kurzy/fotbal", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60000
            });
            await Task.Delay(10000);
        }

        // Save screenshot for debugging
        var screenshotPath = Path.Combine(Environment.CurrentDirectory, "tipsport-screenshot.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
        Console.WriteLine($"      Screenshot saved to: {screenshotPath}");

        // Try clicking on football menu to trigger API call
        Console.WriteLine("      Looking for football leagues menu...");
        try
        {
            // Try to find and click on a league to trigger offer API
            var leagueLinks = await page.QuerySelectorAllAsync("a[href*='/kurzy/fotbal/']");
            Console.WriteLine($"      Found {leagueLinks.Count} football league links");

            if (leagueLinks.Count > 0)
            {
                Console.WriteLine("      Clicking on first league link...");
                await leagueLinks[0].ClickAsync();
                await Task.Delay(5000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      Could not click league: {ex.Message}");
        }

        // Check page state
        Console.WriteLine("      Checking page state...");

        for (int i = 0; i < 6; i++)  // Max 1 minute
        {
            await Task.Delay(10000);

            // Check if we already captured JSON
            if (!string.IsNullOrEmpty(capturedJson))
            {
                Console.WriteLine("      API response captured!");
                break;
            }

            var content = await page.ContentAsync();
            if (content.Contains("Fotbal") || content.Contains("fotbal") || content.Contains("kurzy"))
            {
                Console.WriteLine("      Page loaded successfully!");
                // Wait a bit more for XHR calls to complete
                await Task.Delay(5000);
                break;
            }

            if (content.Contains("challenge") || content.Contains("Checking"))
            {
                Console.WriteLine($"      Cloudflare challenge in progress... ({i + 1}/12)");
                if (!headless)
                {
                    Console.WriteLine("      (If you see a captcha, please solve it manually)");
                }
            }
        }

        // Try to trigger API call by navigating
        if (string.IsNullOrEmpty(capturedJson))
        {
            Console.WriteLine("      Attempting to trigger API call...");
            try
            {
                // Try clicking on football category
                var fotbalLink = await page.QuerySelectorAsync("a[href*='fotbal'], text=Fotbal");
                if (fotbalLink != null)
                {
                    await fotbalLink.ClickAsync();
                    await Task.Delay(5000);
                }
            }
            catch { }
        }

        // If still no JSON, try fetch via page context (uses session cookies)
        if (string.IsNullOrEmpty(capturedJson))
        {
            Console.WriteLine("      Attempting fetch via page context (uses session cookies)...");

            // Build API URL with date params
            var now = DateTimeOffset.UtcNow;
            var fromMs = new DateTimeOffset(now.Date, TimeSpan.Zero).ToUnixTimeMilliseconds();
            var toMs = new DateTimeOffset(now.Date.AddDays(7), TimeSpan.Zero).ToUnixTimeMilliseconds();

            var apiEndpoints = new[]
            {
                $"/rest/offer/v6/sports?dateFrom={fromMs}&dateTo={toMs}&fromResults=true&withLive=true",
                "/rest/offer/v6/sports?superSport=1",
                "/rest/offer/v6/sports"
            };

            foreach (var endpoint in apiEndpoints)
            {
                try
                {
                    var apiUrl = $"{TipsportBaseUrl}{endpoint}";
                    Console.WriteLine($"      Trying via page context: {apiUrl}");

                    // Use page.EvaluateAsync to fetch within page context (shares cookies/session)
                    var fetchResult = await page.EvaluateAsync<string>($@"
                        async () => {{
                            try {{
                                const response = await fetch('{apiUrl}', {{
                                    method: 'GET',
                                    credentials: 'include',
                                    headers: {{
                                        'Accept': 'application/json',
                                        'Content-Type': 'application/json'
                                    }}
                                }});
                                if (!response.ok) {{
                                    return JSON.stringify({{ error: true, status: response.status }});
                                }}
                                const text = await response.text();
                                return text;
                            }} catch (e) {{
                                return JSON.stringify({{ error: true, message: e.message }});
                            }}
                        }}
                    ");

                    if (fetchResult != null && !fetchResult.Contains("\"error\":true"))
                    {
                        capturedJson = fetchResult;
                        Console.WriteLine($"      Page context fetch successful: {capturedJson.Length:N0} bytes");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"      Fetch result: {fetchResult?.Substring(0, Math.Min(100, fetchResult?.Length ?? 0))}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      Error: {ex.Message}");
                }
            }
        }

        // If still nothing, save page content for debugging
        if (string.IsNullOrEmpty(capturedJson))
        {
            Console.WriteLine("      Saving page content for debugging...");
            var content = await page.ContentAsync();
            var debugPath = Path.Combine(Environment.CurrentDirectory, "debug-tipsport.html");
            await File.WriteAllTextAsync(debugPath, content);
            Console.WriteLine($"      Page saved to: {debugPath}");

            // Try to find JSON data in the page itself
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"window\.__INITIAL_STATE__\s*=\s*(\{.+?\});", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                Console.WriteLine("      Found __INITIAL_STATE__ in page!");
                capturedJson = jsonMatch.Groups[1].Value;
            }
        }

        return capturedJson;
    }

    static List<TipsportCompetition> ParseCompetitions(string json)
    {
        var competitions = new List<TipsportCompetition>();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            var response = JsonSerializer.Deserialize<TipsportResponse>(json, options);
            if (response?.Data?.Children == null)
            {
                return competitions;
            }

            // Extract competitions recursively
            foreach (var child in response.Data.Children)
            {
                ExtractCompetitionsRecursive(child, competitions, null, null);
            }

            // Deduplicate
            return competitions
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .Where(c => c.SuperSportId == 1) // Football only (SuperSportId = 1)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Parse error: {ex.Message}");
            return competitions;
        }
    }

    static void ExtractCompetitionsRecursive(
        TipsportNode node,
        List<TipsportCompetition> competitions,
        string? sportTitle,
        string? superGroupTitle)
    {
        var currentSport = node.Type == "SPORT" ? node.Title : sportTitle;
        var currentGroup = node.Type == "SUPERGROUP" ? node.Title : superGroupTitle;

        if (node.Type == "COMPETITION")
        {
            var comp = new TipsportCompetition
            {
                Id = node.Id,
                Title = node.Title ?? "",
                Url = node.Url ?? "",
                CompetitionAnnualId = node.CompetitionAnnualId,
                SuperSportId = node.SuperSportId ?? 0,
                Count = node.Count ?? 0,
                ParentSportTitle = currentSport,
                ParentSuperGroupTitle = currentGroup
            };

            // Derive country from title (Czech naming conventions)
            comp.DerivedCountryCode = DeriveCountryFromTitle(comp.Title);
            competitions.Add(comp);
        }

        foreach (var child in node.Children ?? new List<TipsportNode>())
        {
            ExtractCompetitionsRecursive(child, competitions, currentSport, currentGroup);
        }
    }

    static string? DeriveCountryFromTitle(string title)
    {
        // Czech league naming: "1. anglická liga" -> England
        var countryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "anglick", "england" },
            { "německ", "germany" },
            { "španělsk", "spain" },
            { "italsk", "italy" },
            { "francouzsk", "france" },
            { "česk", "czech-republic" },
            { "polsk", "poland" },
            { "portug", "portugal" },
            { "holandsk", "netherlands" },
            { "nizozemsk", "netherlands" },
            { "belgick", "belgium" },
            { "rakousk", "austria" },
            { "švýcarsk", "switzerland" },
            { "skotsk", "scotland" },
            { "irsk", "ireland" },
            { "řeck", "greece" },
            { "tureck", "turkey" },
            { "rusk", "russia" },
            { "ukrajinsk", "ukraine" },
            { "amerik", "usa" },
            { "brazil", "brazil" },
            { "argentin", "argentina" },
            { "mexick", "mexico" },
            { "japonsk", "japan" },
            { "korejsk", "south-korea" },
            { "čínsk", "china" },
            { "australsk", "australia" },
            { "dánsk", "denmark" },
            { "norsk", "norway" },
            { "švédsk", "sweden" },
            { "finsk", "finland" },
            { "slovensk", "slovakia" },
            { "maďarsk", "hungary" },
            { "rumunsk", "romania" },
            { "bulharsk", "bulgaria" },
            { "srbsk", "serbia" },
            { "chorvatsk", "croatia" },
            { "slovinsk", "slovenia" }
        };

        foreach (var kvp in countryMap)
        {
            if (title.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    static async Task<bool> PushToApi(string apiUrl, List<TipsportCompetition> competitions)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(2);

            var payload = new TipsportLeaguesPayload
            {
                ProviderId = TipsportProviderId,
                Leagues = competitions.Select(c => new TipsportLeagueDto
                {
                    ProviderLeagueId = c.Id.ToString(),
                    ProviderLeagueName = c.Title,
                    CountryCode = c.DerivedCountryCode,
                    Url = c.Url,
                    MatchCount = c.Count
                }).ToList()
            };

            var response = await client.PostAsJsonAsync($"{apiUrl}/api/tipsport/leagues", payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"      API Response: {result}");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"      API Error ({response.StatusCode}): {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      Push error: {ex.Message}");
            return false;
        }
    }
}

#region Models

public class TipsportResponse
{
    public TipsportData? Data { get; set; }
}

public class TipsportData
{
    public List<TipsportNode> Children { get; set; } = new();
}

public class TipsportNode
{
    public long Id { get; set; }
    public string? Title { get; set; }
    public string? Type { get; set; }
    public string? Url { get; set; }
    public long? CompetitionAnnualId { get; set; }
    public int? SuperSportId { get; set; }
    public int? Count { get; set; }
    public List<TipsportNode> Children { get; set; } = new();
}

public class TipsportCompetition
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public long? CompetitionAnnualId { get; set; }
    public int SuperSportId { get; set; }
    public int Count { get; set; }
    public string? ParentSportTitle { get; set; }
    public string? ParentSuperGroupTitle { get; set; }
    public string? DerivedCountryCode { get; set; }
}

public class TipsportLeaguesPayload
{
    public string ProviderId { get; set; } = "";
    public List<TipsportLeagueDto> Leagues { get; set; } = new();
}

public class TipsportLeagueDto
{
    public string ProviderLeagueId { get; set; } = "";
    public string ProviderLeagueName { get; set; } = "";
    public string? CountryCode { get; set; }
    public string? Url { get; set; }
    public int MatchCount { get; set; }
}

#endregion
