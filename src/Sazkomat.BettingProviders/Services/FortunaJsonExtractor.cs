using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Sazkomat.BettingProviders.Models;
using Sazkomat.Core.Common;

namespace Sazkomat.BettingProviders.Services;

/// <summary>
/// Extracts league data from Fortuna.cz football pages using Playwright.
/// Uses DOM parsing to extract country groups and leagues from the sidebar navigation.
/// </summary>
public partial class FortunaJsonExtractor
{
    private readonly ILogger<FortunaJsonExtractor> _logger;
    private const string BaseUrl = "https://www.ifortuna.cz";
    private const string FootballUrl = "/sazeni/fotbal?tab=matches&filter=all";

    /// <summary>
    /// Groups to exclude from scraping
    /// </summary>
    private static readonly string[] ExcludedGroupPatterns =
    {
        "mezinárodní",
        "mezinarodni",  // Without diacritics
        "international",
        "esport",
        "efotbal",
        "exhibice"
    };

    public FortunaJsonExtractor(ILogger<FortunaJsonExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts league data from Fortuna football page using multi-page scraping.
    /// First gets country URLs from main page, then navigates to each country page to get leagues.
    /// </summary>
    public async Task<Result<FortunaData>> ExtractLeagueDataAsync(string? customUrl = null)
    {
        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            var url = customUrl ?? $"{BaseUrl}{FootballUrl}";
            _logger.LogInformation("Extracting Fortuna data from: {Url}", url);

            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                    "--disable-setuid-sandbox"
                }
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                Locale = "cs-CZ",
                TimezoneId = "Europe/Prague"
            });

            var page = await context.NewPageAsync();

            // Step 1: Navigate to main football page and get country URLs
            _logger.LogInformation("Step 1: Getting country URLs from main page...");
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000
            });
            await Task.Delay(3000);

            var countryUrls = await ExtractCountryUrlsAsync(page);
            _logger.LogInformation("Found {Count} country URLs: {Countries}",
                countryUrls.Count,
                string.Join(", ", countryUrls.Select(c => c.Slug)));

            if (countryUrls.Count == 0)
            {
                return Result<FortunaData>.Failure("No country URLs found on main page");
            }

            // Step 2: Navigate to each country page and extract leagues
            var fortunaData = new FortunaData();
            var processedCount = 0;

            foreach (var (countrySlug, countryUrl) in countryUrls)
            {
                // Skip excluded groups
                if (IsExcludedGroup(countrySlug))
                {
                    _logger.LogDebug("Skipping excluded country: {Country}", countrySlug);
                    continue;
                }

                processedCount++;
                _logger.LogInformation("Step 2: Processing country {Index}/{Total}: {Country}",
                    processedCount, countryUrls.Count, countrySlug);

                var leagues = await ExtractLeaguesFromCountryPageAsync(page, countryUrl, countrySlug);

                // Always add country, even without leagues (for country scan)
                var countryGroup = new FortunaCountryGroup
                {
                    Name = countrySlug,
                    Code = countrySlug,
                    Url = countryUrl,
                    IsExcluded = false
                };
                countryGroup.Leagues.AddRange(leagues);
                fortunaData.CountryGroups.Add(countryGroup);

                if (leagues.Count > 0)
                {
                    _logger.LogInformation("  Found {Count} leagues for {Country}", leagues.Count, countrySlug);
                }
                else
                {
                    _logger.LogDebug("  No leagues found for {Country}", countrySlug);
                }

                // Small delay between requests to be polite
                await Task.Delay(500);
            }

            _logger.LogInformation("Successfully extracted {GroupCount} country groups with {LeagueCount} total leagues",
                fortunaData.CountryGroups.Count,
                fortunaData.CountryGroups.Sum(g => g.Leagues.Count));

            return Result<FortunaData>.Success(fortunaData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting Fortuna data");
            return Result<FortunaData>.Failure($"Extraction failed: {ex.Message}");
        }
        finally
        {
            if (browser != null)
                await browser.CloseAsync();
            playwright?.Dispose();
        }
    }

    /// <summary>
    /// Extracts country URLs from the main football page.
    /// Returns list of (countrySlug, fullUrl) tuples.
    /// </summary>
    private async Task<List<(string Slug, string Url)>> ExtractCountryUrlsAsync(IPage page)
    {
        var script = @"
            (() => {
                const results = [];
                const seen = new Set();
                const links = document.querySelectorAll('a[href*=""/sazeni/fotbal/""]');

                links.forEach(link => {
                    let href = link.getAttribute('href') || '';
                    // Remove query string
                    href = href.split('?')[0];

                    // Parse URL parts
                    const parts = href.split('/').filter(p => p);
                    // We want country links: /sazeni/fotbal/{country} (3 segments, no league)

                    if (parts.length === 3 && parts[0] === 'sazeni' && parts[1] === 'fotbal') {
                        const countrySlug = parts[2];
                        // Clean slug - remove trailing numbers (e.g., anglie-3 -> anglie)
                        const cleanSlug = countrySlug.replace(/-?\d+$/, '');

                        if (cleanSlug && !seen.has(cleanSlug)) {
                            seen.add(cleanSlug);
                            results.push({
                                slug: cleanSlug,
                                originalSlug: countrySlug,
                                url: href
                            });
                        }
                    }
                });

                return results;
            })();
        ";

        var result = await page.EvaluateAsync<JsonElement>(script);
        var countryUrls = new List<(string Slug, string Url)>();

        foreach (var item in result.EnumerateArray())
        {
            var slug = item.GetProperty("slug").GetString() ?? "";
            var originalSlug = item.GetProperty("originalSlug").GetString() ?? "";
            var relativeUrl = item.GetProperty("url").GetString() ?? "";

            if (!string.IsNullOrEmpty(slug))
            {
                // Build full URL
                var fullUrl = $"{BaseUrl}{relativeUrl}?tab=matches&filter=all";
                countryUrls.Add((slug, fullUrl));
                _logger.LogDebug("Country: {Slug} -> {Url}", slug, fullUrl);
            }
        }

        return countryUrls;
    }

    /// <summary>
    /// Navigates to a country page and extracts all league links.
    /// </summary>
    private async Task<List<FortunaLeague>> ExtractLeaguesFromCountryPageAsync(
        IPage page,
        string countryUrl,
        string countrySlug)
    {
        var leagues = new List<FortunaLeague>();

        try
        {
            await page.GotoAsync(countryUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });
            await Task.Delay(2000);

            // Pass the expected country slug to JavaScript for filtering
            var expectedCountrySlug = countrySlug.Replace("-", "").ToLowerInvariant();

            var script = @"
                ((expectedCountry) => {
                    const results = [];
                    const seen = new Set();
                    const links = document.querySelectorAll('a[href*=""/sazeni/fotbal/""]');

                    // Normalize country slug for comparison
                    const normalizeSlug = (s) => s.replace(/-/g, '').replace(/\d+$/, '').toLowerCase();

                    // Helper to strip trailing match count numbers
                    const stripMatchCount = (text) => text.replace(/\d+$/, '').trim();

                    links.forEach(link => {
                        let href = link.getAttribute('href') || '';
                        let rawText = link.textContent?.trim() || '';
                        let text = stripMatchCount(rawText);

                        // Remove query string
                        href = href.split('?')[0];

                        // Parse URL parts - we want league links: /sazeni/fotbal/{country}/{league}
                        const parts = href.split('/').filter(p => p);

                        if (parts.length >= 4 && parts[0] === 'sazeni' && parts[1] === 'fotbal') {
                            const urlCountrySlug = parts[2].replace(/-?\d+$/, ''); // Clean country slug from URL
                            const normalizedUrl = normalizeSlug(urlCountrySlug);

                            // Only include leagues from the CURRENT country page
                            if (normalizedUrl !== expectedCountry) {
                                return;
                            }

                            const leagueSlug = parts[3];
                            // Clean league slug - remove trailing numbers
                            const cleanLeagueSlug = leagueSlug.replace(/-?\d+$/, '');

                            // Skip if league slug is just the country name (these are country page variants, not leagues)
                            const leagueSlugBase = cleanLeagueSlug.replace(/^\d+-/, '');
                            if (leagueSlugBase === urlCountrySlug) {
                                // This is a country variant - skip it unless it has a division number
                                const divMatch = cleanLeagueSlug.match(/^(\d+)-/);
                                if (!divMatch) {
                                    return;
                                }
                            }

                            if (cleanLeagueSlug && !seen.has(cleanLeagueSlug)) {
                                seen.add(cleanLeagueSlug);

                                // Get league name from text and slug
                                let leagueName = text;

                                // Extract division number from SLUG (e.g., ""1-anglie"" -> ""1"")
                                const slugDivMatch = cleanLeagueSlug.match(/^(\d+)-/);
                                const divisionFromSlug = slugDivMatch ? slugDivMatch[1] : null;

                                // Also check text for division number like ""1. "", ""2. ""
                                const textDivMatch = text.match(/^(\d+)\.\s*/);
                                const divisionFromText = textDivMatch ? textDivMatch[1] : null;

                                // Use either source for division number
                                const divisionNumber = divisionFromSlug || divisionFromText;

                                // Remove ""1. "", ""2. "" prefixes from text
                                leagueName = leagueName.replace(/^\d+\.\s*/, '');

                                // If contains "" - "", take part after dash
                                const dashIdx = leagueName.indexOf(' - ');
                                if (dashIdx > 0) {
                                    leagueName = leagueName.substring(dashIdx + 3).trim();
                                }

                                // If league name is just the country name or empty, create better name
                                // Include both ASCII and Czech diacritics versions
                                const countryNames = [
                                    'anglie', 'německo', 'nemecko', 'itálie', 'italie', 'francie', 'španělsko', 'spanelsko',
                                    'česko', 'cesko', 'slovensko', 'polsko', 'rakousko', 'belgie', 'nizozemsko',
                                    'portugalsko', 'řecko', 'recko', 'turecko', 'chorvatsko', 'srbsko', 'rusko',
                                    'švýcarsko', 'svycarsko', 'dánsko', 'dansko', 'norsko', 'švédsko', 'svedsko',
                                    'finsko', 'irsko', 'skotsko', 'wales', 'maďarsko', 'madarsko', 'rumunsko',
                                    'bulharsko', 'slovinsko', 'bosna', 'makedonie', 'albánie', 'albanie',
                                    'kypr', 'izrael', 'gruzie', 'arménie', 'armenie', 'ázerbájdžán', 'azerbajdzan',
                                    'kazachstán', 'kazachstan', 'bělorusko', 'belorusko', 'litva', 'lotyšsko', 'lotyssko',
                                    'estonsko', 'usa', 'amerika', 'argentina', 'brazílie', 'brazilie', 'mexiko',
                                    'chile', 'kolumbie', 'japonsko', 'korea', 'čína', 'cina', 'austrálie', 'australie',
                                    'egypt', 'maroko', 'tunis', 'tunisko', 'alžírsko', 'alzirsko', 'saudská arábie',
                                    'saudska arabie', 'katar', 'bahrajn', 'thajsko', 'malajsie', 'indonésie', 'indonesie',
                                    'malta', 'etiopie', 'keňa', 'kena', 'rwanda', 'paraguay', 'severni irsko', 'sev-irsko'
                                ];
                                const isJustCountry = !leagueName || leagueName.length < 2 ||
                                    countryNames.some(c => leagueName.toLowerCase() === c);

                                if (isJustCountry && divisionNumber) {
                                    // Create readable name like ""1. Liga"", ""2. Liga""
                                    leagueName = divisionNumber + '. Liga';
                                } else if (isJustCountry) {
                                    // Fallback to formatted slug
                                    leagueName = cleanLeagueSlug
                                        .replace(/^(\d+)-/, '$1. ')
                                        .replace(/-/g, ' ')
                                        .split(' ')
                                        .map(w => w.charAt(0).toUpperCase() + w.slice(1))
                                        .join(' ');
                                }

                                results.push({
                                    leagueSlug: cleanLeagueSlug,
                                    leagueName: leagueName,
                                    url: href
                                });
                            }
                        }
                    });

                    return results;
                })('" + expectedCountrySlug + @"');
            ";

            var result = await page.EvaluateAsync<JsonElement>(script);

            foreach (var item in result.EnumerateArray())
            {
                try
                {
                    var leagueSlug = item.GetProperty("leagueSlug").GetString() ?? "";
                    var leagueName = item.GetProperty("leagueName").GetString() ?? "";
                    var relativeUrl = item.GetProperty("url").GetString() ?? "";

                    if (!string.IsNullOrEmpty(leagueSlug))
                    {
                        leagues.Add(new FortunaLeague
                        {
                            Name = leagueName,
                            Url = $"{BaseUrl}{relativeUrl}",
                            LeagueId = leagueSlug,
                            CountryCode = countrySlug,
                            CountryName = countrySlug
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse league item");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract leagues from country page: {Url}", countryUrl);
        }

        return leagues;
    }

    /// <summary>
    /// Checks if a group name matches any excluded patterns.
    /// </summary>
    private static bool IsExcludedGroup(string groupName)
    {
        return ExcludedGroupPatterns.Any(pattern =>
            groupName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Normalizes Czech country slug to English country code.
    /// </summary>
    public static string NormalizeCountryCode(string czechSlug)
    {
        return czechSlug.ToLowerInvariant() switch
        {
            "anglie" => "england",
            "nemecko" => "germany",
            "spanelsko" => "spain",
            "italie" => "italy",
            "francie" => "france",
            "portugalsko" => "portugal",
            "holandsko" or "nizozemsko" => "netherlands",
            "belgie" => "belgium",
            "rakousko" => "austria",
            "svycarsko" => "switzerland",
            "polsko" => "poland",
            "cesko" or "ceska-republika" => "czech-republic",
            "slovensko" => "slovakia",
            "recko" => "greece",
            "turecko" => "turkey",
            "rusko" => "russia",
            "ukrajina" => "ukraine",
            "skotsko" => "scotland",
            "irsko" => "ireland",
            "severni-irsko" or "sev-irsko" => "northern-ireland",
            "wales" => "wales",
            "dansko" => "denmark",
            "norsko" => "norway",
            "svedsko" => "sweden",
            "finsko" => "finland",
            "chorvatsko" => "croatia",
            "srbsko" => "serbia",
            "madarsko" => "hungary",
            "rumunsko" => "romania",
            "bulharsko" => "bulgaria",
            "slovinsko" => "slovenia",
            "bosna-a-hercegovina" or "bosna" => "bosnia-herzegovina",
            "cerna-hora" => "montenegro",
            "makedonie" or "severni-makedonie" => "north-macedonia",
            "albanie" => "albania",
            "kypr" => "cyprus",
            "izrael" => "israel",
            "gruzie" => "georgia",
            "azerbajdzan" => "azerbaijan",
            "armenie" => "armenia",
            "kazachstan" => "kazakhstan",
            "belorusko" => "belarus",
            "litva" => "lithuania",
            "lotyssko" => "latvia",
            "estonsko" => "estonia",
            "usa" or "amerika" => "usa",
            "argentina" => "argentina",
            "brazilie" => "brazil",
            "mexiko" => "mexico",
            "chile" => "chile",
            "kolumbie" => "colombia",
            "japonsko" => "japan",
            "jizni-korea" or "korea" => "south-korea",
            "cina" => "china",
            "australie" => "australia",
            "novy-zeland" => "new-zealand",
            "egypt" => "egypt",
            "maroko" => "morocco",
            "tunis" or "tunisko" => "tunisia",
            "alzirsko" => "algeria",
            "jizni-afrika" => "south-africa",
            "saudska-arabie" => "saudi-arabia",
            "katar" => "qatar",
            "spojene-arabske-emiraty" or "sae" => "uae",
            // Additional Fortuna-specific mappings
            "bahrajn" => "bahrain",
            "irak" => "iraq",
            "iran" => "iran",
            "jamajka" => "jamaica",
            "malajsie" => "malaysia",
            "myanmar" => "myanmar",
            "angola" => "angola",
            "oman" => "oman",
            "senegal" => "senegal",
            "uganda" => "uganda",
            "uruguay" => "uruguay",
            "paraguay" => "paraguay",
            "kostarika" => "costa-rica",
            "singapur" => "singapore",
            "indie" => "india",
            _ => czechSlug // Return as-is if no mapping
        };
    }
}
