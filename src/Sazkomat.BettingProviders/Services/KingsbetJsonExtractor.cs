using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Sazkomat.BettingProviders.Models;
using Sazkomat.Core.Common;

namespace Sazkomat.BettingProviders.Services;

/// <summary>
/// Extracts and parses JSON data from Kingsbet/Altenar sportsbook API.
/// Uses Playwright to intercept network requests and capture the Authorization token.
///
/// Kingsbet uses Altenar as their sportsbook provider. The API requires a dynamic
/// JWT token that is generated client-side and sent as Authorization header.
/// </summary>
public class KingsbetJsonExtractor
{
    private readonly ILogger<KingsbetJsonExtractor> _logger;
    private const string BaseUrl = "https://www.kingsbet.cz";
    private const string SportPageUrl = "/sport";
    private const string AltenarApiDomain = "sb2frontend-altenar2.biahosted.com";
    private const string ApiEndpoint = "/api/widget/GetSportMenu";

    // Token cache
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public KingsbetJsonExtractor(ILogger<KingsbetJsonExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts sport menu data from Kingsbet/Altenar API
    /// </summary>
    public async Task<Result<KingsbetSportMenuResponse>> ExtractSportMenuAsync()
    {
        try
        {
            _logger.LogInformation("Fetching Kingsbet sport menu data...");

            // Get authorization token (cached or fresh)
            var tokenResult = await GetAuthorizationTokenAsync();
            if (!tokenResult.IsSuccess)
            {
                return Result<KingsbetSportMenuResponse>.Failure(tokenResult.Error ?? "Failed to get authorization token");
            }

            var token = tokenResult.Value!;

            // Call Altenar API with the token
            var apiResult = await CallAltenarApiAsync(token);
            if (!apiResult.IsSuccess)
            {
                // Token might be expired, try to get a fresh one
                _logger.LogWarning("API call failed, trying with fresh token...");
                _cachedToken = null;

                tokenResult = await GetAuthorizationTokenAsync();
                if (!tokenResult.IsSuccess)
                {
                    return Result<KingsbetSportMenuResponse>.Failure(tokenResult.Error ?? "Failed to get fresh authorization token");
                }

                apiResult = await CallAltenarApiAsync(tokenResult.Value!);
                if (!apiResult.IsSuccess)
                {
                    return Result<KingsbetSportMenuResponse>.Failure(apiResult.Error ?? "API call failed");
                }
            }

            // Parse the response
            var parseResult = ParseSportMenuResponse(apiResult.Value!);
            if (!parseResult.IsSuccess)
            {
                return Result<KingsbetSportMenuResponse>.Failure(parseResult.Error ?? "Failed to parse response");
            }

            // Resolve category associations for championships
            ResolveChampionshipCategories(parseResult.Value!);

            _logger.LogInformation("Successfully extracted {SportCount} sports, {CategoryCount} categories, {ChampCount} championships from Kingsbet",
                parseResult.Value!.Sports.Count,
                parseResult.Value!.Categories.Count,
                parseResult.Value!.Championships.Count);

            return Result<KingsbetSportMenuResponse>.Success(parseResult.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting Kingsbet data");
            return Result<KingsbetSportMenuResponse>.Failure($"Extraction failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the authorization token, using cache if available and valid
    /// </summary>
    private async Task<Result<string>> GetAuthorizationTokenAsync()
    {
        // Check cache
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            _logger.LogDebug("Using cached authorization token");
            return Result<string>.Success(_cachedToken);
        }

        _logger.LogInformation("Fetching fresh authorization token from Kingsbet...");

        return await ExtractTokenViaPlaywrightAsync();
    }

    /// <summary>
    /// Uses Playwright to navigate to Kingsbet and intercept the Authorization header
    /// </summary>
    private async Task<Result<string>> ExtractTokenViaPlaywrightAsync()
    {
        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
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
                    "--disable-infobars"
                }
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "cs-CZ",
                TimezoneId = "Europe/Prague"
            });

            // Anti-detection scripts
            await context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                Object.defineProperty(navigator, 'languages', { get: () => ['cs-CZ', 'cs', 'en-US', 'en'] });
                window.chrome = { runtime: {} };
            ");

            var page = await context.NewPageAsync();

            string? capturedToken = null;

            // Set up request interception BEFORE navigation
            page.Request += (_, request) =>
            {
                if (request.Url.Contains(AltenarApiDomain))
                {
                    _logger.LogDebug("Intercepted Altenar request: {Url}", request.Url.Substring(0, Math.Min(100, request.Url.Length)));
                    var headers = request.Headers;
                    if (headers.TryGetValue("authorization", out var authHeader) && !string.IsNullOrEmpty(authHeader))
                    {
                        capturedToken = authHeader;
                        _logger.LogInformation("Captured Authorization token: {TokenPreview}...",
                            authHeader.Substring(0, Math.Min(50, authHeader.Length)));
                    }
                }
            };

            _logger.LogInformation("Navigating to Kingsbet sport page...");

            // Use NetworkIdle to ensure API calls are made
            await page.GotoAsync($"{BaseUrl}{SportPageUrl}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 90000
            });

            // If token was captured during navigation, return it
            if (!string.IsNullOrEmpty(capturedToken))
            {
                _cachedToken = capturedToken;
                _tokenExpiry = DateTime.UtcNow.AddMinutes(5);
                _logger.LogInformation("Successfully captured Authorization token during navigation");
                return Result<string>.Success(capturedToken);
            }

            // Try clicking on Football category to trigger API call
            _logger.LogInformation("Token not captured during navigation, trying to trigger API call...");

            try
            {
                // Wait for any sport menu element and click
                var footballLink = page.Locator("text=Fotbal").First;
                if (await footballLink.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 5000 }))
                {
                    await footballLink.ClickAsync();
                    _logger.LogDebug("Clicked on Fotbal link");
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 30000 });
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not click Fotbal link: {Error}", ex.Message);
            }

            // Wait additional time for any delayed API calls
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (!string.IsNullOrEmpty(capturedToken))
                {
                    break;
                }
                await Task.Delay(2000);
                _logger.LogDebug("Waiting for Altenar API request (attempt {Attempt}/5)...", attempt + 1);
            }

            if (string.IsNullOrEmpty(capturedToken))
            {
                // Try alternative approach - check if token is in page context
                _logger.LogWarning("Event handler didn't capture token, trying JavaScript extraction...");

                try
                {
                    // Try to get token from window or localStorage
                    var tokenFromJs = await page.EvaluateAsync<string?>(@"() => {
                        // Check localStorage
                        for (let i = 0; i < localStorage.length; i++) {
                            const key = localStorage.key(i);
                            const value = localStorage.getItem(key);
                            if (value && value.includes('Bearer') || (value && value.length > 100 && value.startsWith('ey'))) {
                                return value;
                            }
                        }
                        // Check sessionStorage
                        for (let i = 0; i < sessionStorage.length; i++) {
                            const key = sessionStorage.key(i);
                            const value = sessionStorage.getItem(key);
                            if (value && value.includes('Bearer') || (value && value.length > 100 && value.startsWith('ey'))) {
                                return value;
                            }
                        }
                        return null;
                    }");

                    if (!string.IsNullOrEmpty(tokenFromJs))
                    {
                        capturedToken = tokenFromJs.StartsWith("Bearer ") ? tokenFromJs : $"Bearer {tokenFromJs}";
                        _logger.LogInformation("Captured token from JavaScript storage");
                    }
                }
                catch (Exception jsEx)
                {
                    _logger.LogDebug("JavaScript token extraction failed: {Error}", jsEx.Message);
                }
            }

            if (string.IsNullOrEmpty(capturedToken))
            {
                // Log page state for debugging
                var pageUrl = page.Url;
                var pageTitle = await page.TitleAsync();
                _logger.LogWarning("Failed to capture token. Page URL: {Url}, Title: {Title}", pageUrl, pageTitle);

                return Result<string>.Failure("Failed to capture Authorization token from Kingsbet");
            }

            // Cache the token
            _cachedToken = capturedToken;
            _tokenExpiry = DateTime.UtcNow.AddMinutes(5);

            _logger.LogInformation("Successfully captured Authorization token");
            return Result<string>.Success(capturedToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playwright token extraction failed");
            return Result<string>.Failure($"Token extraction failed: {ex.Message}");
        }
        finally
        {
            if (browser != null)
                await browser.CloseAsync();
            playwright?.Dispose();
        }
    }

    /// <summary>
    /// Calls the Altenar API with the given authorization token
    /// </summary>
    private async Task<Result<string>> CallAltenarApiAsync(string authToken)
    {
        try
        {
            var apiUrl = $"https://{AltenarApiDomain}{ApiEndpoint}?culture=cs-CZ&timezoneOffset=-60&integration=kingsbet&deviceType=1&numFormat=en-GB&countryCode=CZ&period=0";

            using var httpClient = new HttpClient();

            // Use HttpRequestMessage to add Authorization header without validation
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.TryAddWithoutValidation("Accept", "*/*");
            request.Headers.TryAddWithoutValidation("Authorization", authToken);
            request.Headers.TryAddWithoutValidation("Origin", BaseUrl);
            request.Headers.TryAddWithoutValidation("Referer", $"{BaseUrl}/");
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");

            _logger.LogDebug("Calling Altenar API: {Url}", apiUrl);
            _logger.LogDebug("Authorization header length: {Length}", authToken?.Length ?? 0);

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Altenar API returned {StatusCode}: {Preview}",
                    response.StatusCode,
                    errorContent.Substring(0, Math.Min(500, errorContent.Length)));
                return Result<string>.Failure($"API returned {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Altenar API returned {Length} bytes", content.Length);

            return Result<string>.Success(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Altenar API call failed");
            return Result<string>.Failure($"API call failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses the sport menu JSON response
    /// </summary>
    private Result<KingsbetSportMenuResponse> ParseSportMenuResponse(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            var response = JsonSerializer.Deserialize<KingsbetSportMenuResponse>(json, options);

            if (response == null)
            {
                return Result<KingsbetSportMenuResponse>.Failure("Failed to deserialize response");
            }

            _logger.LogDebug("Parsed response: {Sports} sports, {Categories} categories, {Champs} championships",
                response.Sports.Count,
                response.Categories.Count,
                response.Championships.Count);

            return Result<KingsbetSportMenuResponse>.Success(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Kingsbet JSON");
            return Result<KingsbetSportMenuResponse>.Failure($"JSON parsing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolves championship-to-category associations using champIds
    /// </summary>
    private void ResolveChampionshipCategories(KingsbetSportMenuResponse response)
    {
        // Build a lookup: championship ID -> category
        var champToCategory = new Dictionary<int, KingsbetCategory>();

        foreach (var category in response.Categories)
        {
            foreach (var champId in category.ChampionshipIds)
            {
                champToCategory[champId] = category;
            }
        }

        // Assign category info to championships
        foreach (var champ in response.Championships)
        {
            if (champToCategory.TryGetValue(champ.Id, out var category))
            {
                champ.CategoryId = category.Id;
                champ.CategoryName = category.Name;
                champ.CategoryIso = category.Iso;
            }
        }

        var mappedCount = response.Championships.Count(c => c.CategoryId.HasValue);
        _logger.LogDebug("Resolved {Mapped}/{Total} championships to categories",
            mappedCount, response.Championships.Count);
    }

    /// <summary>
    /// Extracts championships for a specific sport
    /// </summary>
    public async Task<Result<List<KingsbetChampionship>>> ExtractChampionshipsForSportAsync(int sportId)
    {
        var menuResult = await ExtractSportMenuAsync();
        if (!menuResult.IsSuccess)
        {
            return Result<List<KingsbetChampionship>>.Failure(menuResult.Error ?? "Failed to get sport menu");
        }

        var menu = menuResult.Value!;

        // Find the sport
        var sport = menu.Sports.FirstOrDefault(s => s.Id == sportId);
        if (sport == null)
        {
            return Result<List<KingsbetChampionship>>.Failure($"Sport with ID {sportId} not found");
        }

        // Get categories for this sport
        var sportCategoryIds = new HashSet<int>(sport.CategoryIds);

        // Filter categories belonging to this sport
        var sportCategories = menu.Categories
            .Where(c => sportCategoryIds.Contains(c.Id))
            .ToList();

        // Get championship IDs from these categories
        var sportChampIds = new HashSet<int>(
            sportCategories.SelectMany(c => c.ChampionshipIds)
        );

        // Filter championships
        var championships = menu.Championships
            .Where(c => sportChampIds.Contains(c.Id))
            .ToList();

        _logger.LogInformation("Found {Count} championships for sport {SportName} (ID: {SportId})",
            championships.Count, sport.Name, sportId);

        return Result<List<KingsbetChampionship>>.Success(championships);
    }

    /// <summary>
    /// Extracts categories (countries) for a specific sport
    /// </summary>
    public async Task<Result<List<KingsbetCategory>>> ExtractCategoriesForSportAsync(int sportId)
    {
        var menuResult = await ExtractSportMenuAsync();
        if (!menuResult.IsSuccess)
        {
            return Result<List<KingsbetCategory>>.Failure(menuResult.Error ?? "Failed to get sport menu");
        }

        var menu = menuResult.Value!;

        // Find the sport
        var sport = menu.Sports.FirstOrDefault(s => s.Id == sportId);
        if (sport == null)
        {
            return Result<List<KingsbetCategory>>.Failure($"Sport with ID {sportId} not found");
        }

        // Get categories for this sport
        var sportCategoryIds = new HashSet<int>(sport.CategoryIds);

        var categories = menu.Categories
            .Where(c => sportCategoryIds.Contains(c.Id))
            .ToList();

        _logger.LogInformation("Found {Count} categories for sport {SportName} (ID: {SportId})",
            categories.Count, sport.Name, sportId);

        return Result<List<KingsbetCategory>>.Success(categories);
    }
}
