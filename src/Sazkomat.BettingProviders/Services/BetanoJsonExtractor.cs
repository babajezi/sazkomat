using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Sazkomat.BettingProviders.Models;
using Sazkomat.Core.Common;
using Sazkomat.Data.Scrapers;

namespace Sazkomat.BettingProviders.Services;

/// <summary>
/// Extracts and parses JSON data from Betano.cz league pages
/// </summary>
public partial class BetanoJsonExtractor
{
    private readonly PlaywrightHttpClient _playwrightClient;
    private readonly ILogger<BetanoJsonExtractor> _logger;

    // Regex to extract JSON from script tag: window["initial_state"] = {...}
    [GeneratedRegex(@"window\[""initial_state""\]\s*=\s*(\{.+?\})\s*</script>", RegexOptions.Singleline)]
    private static partial Regex InitialStatePattern();

    public BetanoJsonExtractor(PlaywrightHttpClient playwrightClient, ILogger<BetanoJsonExtractor> logger)
    {
        _playwrightClient = playwrightClient;
        _logger = logger;
    }

    /// <summary>
    /// Downloads and extracts league data from Betano sport page
    /// </summary>
    /// <param name="sportUrl">Full URL to Betano sport page (e.g., https://www.betano.cz/sport/fotbal/liga/)</param>
    /// <returns>Betano data containing leagues and regions</returns>
    public async Task<Result<BetanoData>> ExtractLeagueDataAsync(string sportUrl)
    {
        try
        {
            _logger.LogInformation("Fetching Betano data from {Url}", sportUrl);

            // 1. Download HTML
            var html = await DownloadHtmlAsync(sportUrl);
            if (string.IsNullOrEmpty(html))
            {
                return Result<BetanoData>.Failure("Failed to download HTML from Betano");
            }

            _logger.LogDebug("Downloaded HTML, size: {Size} characters", html.Length);

            // 2. Extract JSON
            var extractResult = ExtractJsonFromHtml(html);
            if (!extractResult.IsSuccess)
            {
                return Result<BetanoData>.Failure(extractResult.Error);
            }

            var json = extractResult.Value;
            _logger.LogDebug("Extracted JSON, size: {Size} characters", json.Length);

            // 3. Parse JSON
            var parseResult = ParseJson(json);
            if (!parseResult.IsSuccess)
            {
                return Result<BetanoData>.Failure(parseResult.Error);
            }

            var data = parseResult.Value;
            _logger.LogInformation(
                "Successfully parsed Betano data: {TopLeagues} top leagues, {RegionGroups} region groups",
                data.TopLeagues.Count,
                data.RegionGroups.Count);

            return Result<BetanoData>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting Betano data from {Url}", sportUrl);
            return Result<BetanoData>.Failure($"Extraction failed: {ex.Message}");
        }
    }

    private async Task<string> DownloadHtmlAsync(string url)
    {
        try
        {
            // Use Playwright to download HTML (handles JavaScript rendering and anti-bot protection)
            return await _playwrightClient.GetHtmlAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playwright request failed for {Url}", url);
            throw;
        }
    }

    private Result<string> ExtractJsonFromHtml(string html)
    {
        var match = InitialStatePattern().Match(html);

        if (!match.Success)
        {
            _logger.LogError("Failed to find window[\"initial_state\"] JSON in HTML");
            return Result<string>.Failure("Could not extract initial_state JSON from page");
        }

        var json = match.Groups[1].Value;

        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogError("Extracted JSON is empty");
            return Result<string>.Failure("Extracted JSON is empty");
        }

        return Result<string>.Success(json);
    }

    private Result<BetanoData> ParseJson(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            var response = JsonSerializer.Deserialize<BetanoResponse>(json, options);

            if (response?.Data == null)
            {
                _logger.LogError("Deserialized response has null Data");
                return Result<BetanoData>.Failure("Invalid JSON structure: Data is null");
            }

            return Result<BetanoData>.Success(response.Data);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON");
            return Result<BetanoData>.Failure($"JSON parsing failed: {ex.Message}");
        }
    }
}
