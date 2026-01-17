using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Sazkomat.BettingProviders.Models;
using Sazkomat.Core.Common;

namespace Sazkomat.BettingProviders.Services;

/// <summary>
/// Extracts and parses JSON data from Chance.cz REST API
/// Uses FlareSolverr (primary) or Playwright (fallback) to bypass Cloudflare protection
///
/// Note: Chance and Tipsport are both owned by SAZKA Group and share similar API structure.
/// This extractor reuses TipsportModels as the response format is expected to be identical.
/// </summary>
public class ChanceJsonExtractor
{
    private readonly ILogger<ChanceJsonExtractor> _logger;
    private readonly FlareSolverrClient? _flareSolverrClient;
    private const string BaseUrl = "https://www.chance.cz";
    private const string ApiEndpoint = "/rest/offer/v6/sports";
    private const string FootballPageUrl = "/kurzy/fotbal-16";

    public ChanceJsonExtractor(
        ILogger<ChanceJsonExtractor> logger,
        FlareSolverrClient? flareSolverrClient = null)
    {
        _logger = logger;
        _flareSolverrClient = flareSolverrClient;
    }

    /// <summary>
    /// Extracts competition data from Chance for a given date range
    /// </summary>
    public async Task<Result<List<TipsportCompetition>>> ExtractCompetitionsAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null)
    {
        try
        {
            var from = dateFrom ?? DateTimeOffset.Now.Date;
            var to = dateTo ?? DateTimeOffset.Now.Date.AddDays(1).AddSeconds(-1);

            _logger.LogInformation("Fetching Chance data from {From} to {To}", from, to);

            // Try FlareSolverr first (preferred), then fallback to Playwright
            Result<string> jsonResult;

            if (_flareSolverrClient != null)
            {
                jsonResult = await FetchJsonViaFlareSolverrAsync();
                if (!jsonResult.IsSuccess)
                {
                    _logger.LogWarning("FlareSolverr failed: {Error}. Trying Playwright fallback...", jsonResult.Error);
                    jsonResult = await FetchJsonViaPlaywrightAsync(from, to);
                }
            }
            else
            {
                _logger.LogDebug("FlareSolverrClient not available, using Playwright");
                jsonResult = await FetchJsonViaPlaywrightAsync(from, to);
            }

            if (!jsonResult.IsSuccess)
            {
                return Result<List<TipsportCompetition>>.Failure(jsonResult.Error ?? "Unknown error");
            }

            var parseResult = ParseJson(jsonResult.Value!);
            if (!parseResult.IsSuccess)
            {
                return Result<List<TipsportCompetition>>.Failure(parseResult.Error ?? "Parse error");
            }

            var competitions = ExtractCompetitionsFromTree(parseResult.Value!);

            _logger.LogInformation("Successfully extracted {Count} competitions from Chance", competitions.Count);

            return Result<List<TipsportCompetition>>.Success(competitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting Chance data");
            return Result<List<TipsportCompetition>>.Failure($"Extraction failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts competitions for a specific sport (by SuperSportId)
    /// </summary>
    public async Task<Result<List<TipsportCompetition>>> ExtractCompetitionsForSportAsync(
        int superSportId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null)
    {
        var result = await ExtractCompetitionsAsync(dateFrom, dateTo);
        if (!result.IsSuccess)
        {
            return result;
        }

        var filtered = result.Value!
            .Where(c => c.SuperSportId == superSportId)
            .ToList();

        _logger.LogInformation("Filtered to {Count} competitions for SuperSportId {Id}",
            filtered.Count, superSportId);

        return Result<List<TipsportCompetition>>.Success(filtered);
    }

    /// <summary>
    /// Fetches JSON from Chance API using FlareSolverr with persistent session (Cloudflare bypass)
    /// </summary>
    private async Task<Result<string>> FetchJsonViaFlareSolverrAsync()
    {
        if (_flareSolverrClient == null)
        {
            return Result<string>.Failure("FlareSolverr client not configured");
        }

        var sessionId = $"chance_{Guid.NewGuid():N}";

        try
        {
            _logger.LogInformation("Fetching Chance via FlareSolverr with session...");

            // Step 1: Create a persistent session
            var sessionResult = await _flareSolverrClient.CreateSessionAsync(sessionId);
            if (!sessionResult.IsSuccess)
            {
                return Result<string>.Failure($"Failed to create session: {sessionResult.Error}");
            }

            // Step 2: Visit football page to establish session cookies
            _logger.LogDebug("Visiting football page to establish session...");
            var pageResult = await _flareSolverrClient.GetWithSessionAsync($"{BaseUrl}{FootballPageUrl}", sessionId);
            if (!pageResult.IsSuccess)
            {
                _logger.LogWarning("Failed to visit football page: {Error}", pageResult.Error);
                // Continue anyway - might still work
            }

            // Small delay to let session stabilize
            await Task.Delay(1000);

            // Step 3: Fetch API with the same session
            var apiUrl = $"{BaseUrl}{ApiEndpoint}?fromResults=false&withLive=true";
            _logger.LogDebug("Fetching API with session...");
            var apiResult = await _flareSolverrClient.GetWithSessionAsync(apiUrl, sessionId);

            if (!apiResult.IsSuccess)
            {
                return Result<string>.Failure($"API fetch failed: {apiResult.Error}");
            }

            var json = apiResult.Value;

            // Validate JSON
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith('{'))
            {
                _logger.LogWarning("Invalid response from FlareSolverr: {Preview}",
                    json?.Substring(0, Math.Min(200, json.Length)));
                return Result<string>.Failure("Invalid JSON response from Chance");
            }

            _logger.LogInformation("FlareSolverr: Got {Length} bytes from Chance API", json.Length);
            return Result<string>.Success(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlareSolverr fetch failed");
            return Result<string>.Failure($"FlareSolverr error: {ex.Message}");
        }
        finally
        {
            // Always cleanup the session
            await _flareSolverrClient.DestroySessionAsync(sessionId);
        }
    }

    /// <summary>
    /// Fetches JSON from Chance API using Playwright to bypass Cloudflare (fallback)
    /// </summary>
    private async Task<Result<string>> FetchJsonViaPlaywrightAsync(DateTimeOffset from, DateTimeOffset to)
    {
        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            var fromMs = from.ToUnixTimeMilliseconds();
            var toMs = to.ToUnixTimeMilliseconds();

            var apiUrl = $"{BaseUrl}{ApiEndpoint}?dateFrom={fromMs}&dateTo={toMs}&fromResults=true&withLive=true";
            _logger.LogDebug("API URL: {Url}", apiUrl);

            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-infobars",
                    "--window-position=0,0",
                    "--ignore-certificate-errors",
                    "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
                }
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "cs-CZ",
                TimezoneId = "Europe/Prague",
                JavaScriptEnabled = true,
                HasTouch = false,
                IsMobile = false
            });

            await context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                Object.defineProperty(navigator, 'languages', { get: () => ['cs-CZ', 'cs', 'en-US', 'en'] });
                window.chrome = { runtime: {} };
                const originalQuery = window.navigator.permissions.query;
                window.navigator.permissions.query = (parameters) => (
                    parameters.name === 'notifications' ?
                        Promise.resolve({ state: Notification.permission }) :
                        originalQuery(parameters)
                );
            ");

            var page = await context.NewPageAsync();

            string? capturedJson = null;

            page.Response += async (_, response) =>
            {
                if (response.Url.Contains("/rest/offer/v6/sports") && response.Status == 200)
                {
                    try
                    {
                        capturedJson = await response.TextAsync();
                        _logger.LogDebug("Captured API response: {Length} bytes", capturedJson.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read API response body");
                    }
                }
            };

            _logger.LogInformation("Navigating to Chance football betting page...");
            await page.GotoAsync($"{BaseUrl}/kurzy/fotbal", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 90000
            });

            _logger.LogInformation("Waiting for Cloudflare challenge to complete...");

            var pageReady = false;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                await Task.Delay(10000);

                var pageContent = await page.ContentAsync();

                if (pageContent.Contains("challenge") || pageContent.Contains("Checking your browser"))
                {
                    _logger.LogWarning("Cloudflare challenge still active (attempt {Attempt}/6)...", attempt + 1);
                    continue;
                }

                if (pageContent.Contains("chance") || pageContent.Contains("Fotbal") ||
                    pageContent.Contains("kurzy") || pageContent.Contains("odds"))
                {
                    _logger.LogInformation("Page loaded successfully (detected Chance content)");
                    pageReady = true;
                    break;
                }

                _logger.LogDebug("Page content check {Attempt}: waiting for Chance content...", attempt + 1);
            }

            if (!pageReady)
            {
                _logger.LogWarning("Page did not load properly after 60 seconds, continuing anyway...");
            }

            if (string.IsNullOrEmpty(capturedJson))
            {
                _logger.LogInformation("Attempting to trigger API by clicking Fotbal...");
                try
                {
                    var fotbalElement = await page.WaitForSelectorAsync("text=Fotbal", new PageWaitForSelectorOptions
                    {
                        Timeout = 10000
                    });

                    if (fotbalElement != null)
                    {
                        await fotbalElement.ClickAsync();
                        await Task.Delay(3000);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not click on Fotbal element");
                }
            }

            if (string.IsNullOrEmpty(capturedJson))
            {
                _logger.LogInformation("Attempting fetch through page context (uses session cookies)...");
                try
                {
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
                        _logger.LogInformation("Page context fetch successful: {Length} bytes", capturedJson.Length);
                    }
                    else
                    {
                        _logger.LogWarning("Page context fetch failed: {Result}", fetchResult);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Page context fetch failed");
                }
            }

            if (string.IsNullOrEmpty(capturedJson))
            {
                return Result<string>.Failure("Failed to capture JSON response from Chance API");
            }

            return Result<string>.Success(capturedJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playwright request failed for Chance");
            return Result<string>.Failure($"Playwright failed: {ex.Message}");
        }
        finally
        {
            if (browser != null)
                await browser.CloseAsync();

            playwright?.Dispose();
        }
    }

    /// <summary>
    /// Parses JSON response into TipsportResponse (same format as Tipsport)
    /// </summary>
    private Result<TipsportResponse> ParseJson(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            var response = JsonSerializer.Deserialize<TipsportResponse>(json, options);

            if (response?.Data == null)
            {
                _logger.LogError("Deserialized response has null Data");
                return Result<TipsportResponse>.Failure("Invalid JSON structure: Data is null");
            }

            _logger.LogDebug("Parsed Chance response with {Count} children",
                response.Data.Children.Count);

            return Result<TipsportResponse>.Success(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Chance JSON");
            return Result<TipsportResponse>.Failure($"JSON parsing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Recursively extracts all COMPETITION nodes from tree structure
    /// </summary>
    private List<TipsportCompetition> ExtractCompetitionsFromTree(TipsportResponse response)
    {
        var competitions = new List<TipsportCompetition>();

        foreach (var child in response.Data.Children)
        {
            ExtractCompetitionsRecursive(child, competitions, null, null);
        }

        var unique = competitions
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .OrderBy(c => c.Title)
            .ToList();

        _logger.LogDebug("Extracted {Total} competitions, {Unique} unique",
            competitions.Count, unique.Count);

        return unique;
    }

    /// <summary>
    /// Recursively traverses tree and extracts COMPETITION nodes
    /// </summary>
    private void ExtractCompetitionsRecursive(
        TipsportNode node,
        List<TipsportCompetition> competitions,
        string? parentSportTitle,
        string? parentSuperGroupTitle)
    {
        var sportTitle = parentSportTitle;
        var superGroupTitle = parentSuperGroupTitle;

        if (node.Type == TipsportNodeType.Sport)
        {
            sportTitle = node.Title;
        }
        else if (node.Type == TipsportNodeType.SuperGroup)
        {
            superGroupTitle = node.Title;
        }

        if (node.Type == TipsportNodeType.Competition)
        {
            competitions.Add(new TipsportCompetition
            {
                Id = node.Id,
                Title = node.Title,
                Url = node.Url ?? string.Empty,
                CompetitionAnnualId = node.CompetitionAnnualId,
                SuperSportId = node.SuperSportId ?? 0,
                Count = node.Count ?? 0,
                ParentSportTitle = sportTitle,
                ParentSuperGroupTitle = superGroupTitle,
                CommunityStatsEnabled = node.CommunityStatsEnabled ?? false
            });
        }

        foreach (var child in node.Children)
        {
            ExtractCompetitionsRecursive(child, competitions, sportTitle, superGroupTitle);
        }
    }
}
