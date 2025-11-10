using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Sazkomat.DataImport.Helpers;
using Sazkomat.DataImport.Scrapers;
using System.Text.RegularExpressions;

namespace Sazkomat.DataImport.Validators;

public class BetExplorerRoundValidator : ILeagueRoundValidator
{
    private readonly IHttpClient _httpClient;
    private readonly ILogger<BetExplorerRoundValidator> _logger;

    // Cup competition patterns to detect
    private static readonly string[] CupPatterns = new[]
    {
        "FINAL",
        "SEMI-FINAL", "SEMIFINALS", "SEMI FINAL",
        "QUARTER-FINAL", "QUARTERFINALS", "QUARTER FINAL",
        "1/8-FINALS", "1/8 FINALS",
        "1/4-FINALS", "1/4 FINALS",
        "1/2-FINALS", "1/2 FINALS",
        "ROUND OF 16", "ROUND OF 32", "ROUND OF 64",
        "ELIMINATION ROUND",
        "PLAY-OFF", "PLAYOFF"
    };

    // Round-based patterns (multi-language support)
    private const string RoundPattern = @"(\d+)\.\s*(ROUND|KOLO|SPIELTAG|JORNADA|GIORNATA|RONDE|RUNDA|TURA)|\b(ROUND|KOLO|SPIELTAG|JORNADA|GIORNATA|RONDE|RUNDA|TURA)\s+(\d+)";

    public BetExplorerRoundValidator(
        IHttpClient httpClient,
        ILogger<BetExplorerRoundValidator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsRoundBasedLeagueAsync(
        string leagueSlug,
        string countrySlug,
        string season,
        Guid providerId)
    {
        try
        {
            // Construct BetExplorer results URL
            // Format: https://www.betexplorer.com/football/{country}/{league-slug}-{season}/results/
            var url = $"https://www.betexplorer.com/football/{countrySlug}/{leagueSlug}-{season}/results/";

            _logger.LogDebug("Validating league structure: {Url}", url);

            var html = await _httpClient.GetHtmlAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find all round/phase headers from table headers (same approach as FootballBetExplorerScraper)
            // BetExplorer uses <th> tags inside <table class="table-main"> for round/phase names
            var headerNodes = doc.DocumentNode.SelectNodes("//table[contains(@class, 'table-main')]//tr//th");

            if (headerNodes == null || !headerNodes.Any())
            {
                _logger.LogWarning(
                    "No table headers found for {Country}/{League}-{Season}, assuming round-based",
                    countrySlug, leagueSlug, season
                );
                return true; // Assume round-based if can't determine
            }

            // Extract header texts (no filtering needed - <th> tags don't contain ads/privacy text)
            var headerTexts = headerNodes
                .Select(h => h.InnerText.Trim())
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .ToList();

            _logger.LogDebug(
                "Found {Count} table headers for {League}-{Season}: {Headers}",
                headerTexts.Count, leagueSlug, season,
                string.Join(", ", headerTexts.Take(5))
            );

            // Check for cup patterns
            var cupHeadersFound = headerTexts
                .Where(h => CupPatterns.Any(p => h.ToUpperInvariant().Contains(p)))
                .ToList();

            if (cupHeadersFound.Any())
            {
                _logger.LogInformation(
                    "Cup competition detected: {Country}/{League}-{Season}. Cup headers: {CupHeaders}",
                    countrySlug, leagueSlug, season,
                    string.Join(", ", cupHeadersFound.Take(3))
                );
                return false; // This is a cup competition - ignore it
            }

            // Check for round-based patterns
            var roundHeadersFound = headerTexts
                .Where(h => Regex.IsMatch(h, RoundPattern, RegexOptions.IgnoreCase))
                .ToList();

            if (roundHeadersFound.Any())
            {
                _logger.LogDebug(
                    "Round-based league confirmed: {Country}/{League}-{Season}. Found {Count} round headers",
                    countrySlug, leagueSlug, season, roundHeadersFound.Count
                );
                return true; // This is a round-based league
            }

            // No clear indicators found - default to round-based to avoid false negatives
            _logger.LogWarning(
                "Could not determine league type for {Country}/{League}-{Season}, defaulting to round-based. Headers: {Headers}",
                countrySlug, leagueSlug, season,
                string.Join(", ", headerTexts.Take(5))
            );
            return true;
        }
        catch (HttpRequestException ex)
        {
            // HTTP error (404, 500, etc.) - don't fail the entire sync
            _logger.LogWarning(ex,
                "HTTP error validating {Country}/{League}-{Season}, including league anyway",
                countrySlug, leagueSlug, season
            );
            return true; // Include league on HTTP errors (don't skip valid leagues)
        }
        catch (Exception ex)
        {
            // Unexpected error - don't fail the entire sync
            _logger.LogError(ex,
                "Unexpected error validating {Country}/{League}-{Season}, including league anyway",
                countrySlug, leagueSlug, season
            );
            return true; // Include league on errors (don't skip valid leagues)
        }
    }
}
