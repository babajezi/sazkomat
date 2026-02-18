using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Core.Common;
using Sazkomat.Data.Debug;
using Sazkomat.Data.DTOs;
using Sazkomat.Data.Entities;
using Sazkomat.Data.Repositories;
using Sazkomat.Data.Scrapers;
using System.Text.Json;

namespace Sazkomat.Data.Services;

public class SeasonSyncService : ISeasonSyncService
{
    private readonly IDataProviderRepository _providerRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ILeagueSeasonRepository _leagueSeasonRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IRoundRepository _roundRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IScraperRecipeRepository _recipeRepository;
    private readonly RecipeExecutorService _recipeExecutor;
    private readonly ScraperFactory _scraperFactory;
    private readonly ILogger<SeasonSyncService> _logger;

    public SeasonSyncService(
        IDataProviderRepository providerRepository,
        ILeagueRepository leagueRepository,
        ILeagueSeasonRepository leagueSeasonRepository,
        ISeasonRepository seasonRepository,
        IRoundRepository roundRepository,
        IMatchRepository matchRepository,
        IScraperRecipeRepository recipeRepository,
        RecipeExecutorService recipeExecutor,
        ScraperFactory scraperFactory,
        ILogger<SeasonSyncService> logger)
    {
        _providerRepository = providerRepository;
        _leagueRepository = leagueRepository;
        _leagueSeasonRepository = leagueSeasonRepository;
        _seasonRepository = seasonRepository;
        _roundRepository = roundRepository;
        _matchRepository = matchRepository;
        _recipeRepository = recipeRepository;
        _recipeExecutor = recipeExecutor;
        _scraperFactory = scraperFactory;
        _logger = logger;
    }

    public async Task<Result> DetectAndMarkCurrentSeasonsAsync(Guid providerId)
    {
        try
        {
            _logger.LogInformation("Detecting current seasons for provider {ProviderId}", providerId);

            // Get provider
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result.Failure("Provider not found");
            }

            // Parse current season patterns (used as override)
            var patterns = JsonSerializer.Deserialize<string[]>(provider.CurrentSeasonPatterns) ?? Array.Empty<string>();
            var currentYear = DateTime.UtcNow.Year;

            _logger.LogInformation(
                "Current season detection: Year={Year}, Override patterns: [{Patterns}]",
                currentYear, string.Join(", ", patterns));

            // Get all active leagues
            var leagues = await _leagueRepository.GetAllAsync();
            var activeLeagues = leagues.Where(l => l.IsActive).ToList();

            _logger.LogInformation("Found {Count} active leagues", activeLeagues.Count);

            var updatedCount = 0;

            // For each active league, check its seasons
            foreach (var league in activeLeagues)
            {
                var leagueSeasons = (await _leagueSeasonRepository.GetByLeagueIdAsync(league.Id, includeRelations: true))
                    .OrderByDescending(ls => ls.Season?.StartYear ?? 0)
                    .ToList();

                foreach (var leagueSeason in leagueSeasons)
                {
                    var season = leagueSeason.Season;
                    if (season == null) continue;

                    var newSyncMode = DetermineSyncMode(season, patterns, currentYear, leagueSeasons);
                    var isCurrent = newSyncMode == SyncMode.Current;

                    // Only update if values changed
                    if (leagueSeason.IsCurrent != isCurrent || leagueSeason.SyncMode != newSyncMode)
                    {
                        await _leagueSeasonRepository.UpdateIsCurrentAsync(
                            leagueSeason.Id,
                            isCurrent,
                            newSyncMode);

                        updatedCount++;

                        _logger.LogInformation(
                            "Marked {League} - {Season} as {Status}",
                            league.DisplayName,
                            season.Name,
                            newSyncMode.ToString().ToUpper());
                    }
                }
            }

            _logger.LogInformation("Updated {Count} league seasons", updatedCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting current seasons");
            return Result.Failure($"Error detecting current seasons: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines the SyncMode for a season using smart detection logic:
    /// 1. Pattern override - if season matches CurrentSeasonPatterns, it's Current
    /// 2. Future check - if startYear > currentYear, it's Future
    /// 3. Single year seasons (e.g., 2026) - Current if currentYear == startYear
    /// 4. Split seasons (e.g., 2025-2026):
    ///    - Current if currentYear == endYear (we're in spring of the season)
    ///    - Future if currentYear == startYear (season hasn't started yet - fall)
    ///    - Historical if next season has data
    /// </summary>
    private SyncMode DetermineSyncMode(
        Season season,
        string[] patterns,
        int currentYear,
        List<LeagueSeason> allLeagueSeasons)
    {
        // 1. Pattern override - admin can force a season to be Current
        if (patterns.Contains(season.Name, StringComparer.OrdinalIgnoreCase))
            return SyncMode.Current;

        // 2. Future check - startYear is strictly after currentYear
        if (season.StartYear > currentYear)
            return SyncMode.Future;

        var isSplitSeason = season.EndYear.HasValue && season.StartYear != season.EndYear.Value;

        if (isSplitSeason)
        {
            // Split season (e.g., 2025-2026): plays from fall of startYear to spring of endYear

            // If currentYear == startYear → season hasn't started yet (Future)
            // Example: In Feb 2026, season 2026-2027 is Future (starts in fall 2026)
            if (currentYear == season.StartYear)
                return SyncMode.Future;

            // If currentYear == endYear → we're in the middle of the season (Current)
            // Example: In Feb 2026, season 2025-2026 is Current (ends in spring 2026)
            if (currentYear == season.EndYear.Value)
            {
                // But check if NEXT season already has data → this one is done
                var nextSeasonStartYear = season.StartYear + 1;
                var nextSeason = allLeagueSeasons
                    .FirstOrDefault(ls => ls.Season?.StartYear == nextSeasonStartYear);

                if (nextSeason?.HasData == true)
                    return SyncMode.Historical;

                return SyncMode.Current;
            }
        }
        else
        {
            // Single year season (e.g., 2026): plays within one calendar year (spring to fall)
            if (currentYear == season.StartYear)
            {
                // Check if next season has data → this one is done
                var nextSeasonStartYear = season.StartYear + 1;
                var nextSeason = allLeagueSeasons
                    .FirstOrDefault(ls => ls.Season?.StartYear == nextSeasonStartYear);

                if (nextSeason?.HasData == true)
                    return SyncMode.Historical;

                return SyncMode.Current;
            }
        }

        return SyncMode.Historical;
    }

    public async Task<Result<SyncResponse>> SyncSeasonDataAsync(
        Guid providerId,
        Guid leagueId,
        Guid seasonId,
        bool forceUpdate = false)
    {
        try
        {
            _logger.LogInformation(
                "Syncing season data for provider {ProviderId}, league {LeagueId}, season {SeasonId}",
                providerId, leagueId, seasonId);

            // Get league season
            var leagueSeason = await _leagueSeasonRepository.GetByLeagueAndSeasonAsync(leagueId, seasonId);
            if (leagueSeason == null)
            {
                return Result<SyncResponse>.Failure("League season not found");
            }

            // Check if season is locked
            if (leagueSeason.IsLocked)
            {
                _logger.LogWarning(
                    "Cannot sync locked season: {League} - {Season}",
                    leagueId, seasonId);

                return Result<SyncResponse>.Failure("Season is locked and cannot be synced. Unlock the season first if you need to update data.");
            }

            // Check if season is ignored
            if (leagueSeason.IsIgnored)
            {
                _logger.LogWarning(
                    "Cannot sync ignored season: {League} - {Season}",
                    leagueId, seasonId);

                return Result<SyncResponse>.Failure("Season is ignored and cannot be synced. Remove ignore flag first if you need to update data.");
            }

            // Check if we should skip this season
            if (!forceUpdate &&
                leagueSeason.SyncMode == SyncMode.Historical &&
                leagueSeason.HasData)
            {
                _logger.LogInformation(
                    "Skipping historical season that already has data: {League} - {Season}",
                    leagueId, seasonId);

                return Result<SyncResponse>.Success(new SyncResponse
                {
                    Success = true,
                    Message = "Season already synced (Historical mode)",
                    Statistics = new SyncStatistics
                    {
                        Skipped = 1
                    }
                });
            }

            // Get provider and league
            var provider = await _providerRepository.GetByIdAsync(providerId);
            if (provider == null)
            {
                return Result<SyncResponse>.Failure("Provider not found");
            }

            var league = await _leagueRepository.GetByIdAsync(leagueId);
            if (league == null)
            {
                return Result<SyncResponse>.Failure("League not found");
            }

            var season = await _seasonRepository.GetByIdAsync(seasonId);
            if (season == null)
            {
                return Result<SyncResponse>.Failure("Season not found");
            }

            _logger.LogInformation(
                "Scraping {LeagueName} season {SeasonName}...",
                league.DisplayName, season.Name);

            // Try recipe-based scraping first
            var recipeResult = await TryScrapeWithRecipesAsync(
                provider, league, season, leagueSeason);

            ScrapeResult scrapeResult;
            if (recipeResult.Success && recipeResult.ScrapeResult != null)
            {
                scrapeResult = recipeResult.ScrapeResult;
                _logger.LogInformation(
                    "Recipe '{RecipeName}' succeeded for {LeagueName} {SeasonName}",
                    recipeResult.SuccessfulRecipeName, league.DisplayName, season.Name);
            }
            else if (recipeResult.ScrapeResult != null)
            {
                // Recipe extracted HTML but found no rounds/results - use the ScrapeResult with correct FailureReason
                scrapeResult = recipeResult.ScrapeResult;
                var reasonText = scrapeResult.FailureReason == NoDataReason.NoRoundsFound
                    ? "má výsledky ale bez struktury kol"
                    : "nemá žádné výsledky";
                _logger.LogInformation(
                    "Recipe for {LeagueName} {SeasonName}: stránka existuje, {Reason}",
                    league.DisplayName, season.Name, reasonText);
            }
            else
            {
                // No recipe worked - mark as NoRecipeFound and return
                _logger.LogWarning(
                    "No suitable recipe found for {LeagueName} {SeasonName}. Marking as NoRecipeFound.",
                    league.DisplayName, season.Name);

                var triedRecipeNames = string.Join(", ", recipeResult.TriedRecipes.Select(r => r.RecipeName));

                // Only mark as NoRecipeFound if there's no existing data in DB
                // If rounds exist from previous sync, preserve the state
                if (leagueSeason.RoundsCount == 0)
                {
                    leagueSeason.HasData = false;
                    leagueSeason.NoDataReason = NoDataReason.NoRecipeFound;
                    leagueSeason.NoDataNote = recipeResult.NoRecipesAvailable
                        ? "Žádný aktivní recept pro betexplorer/results"
                        : $"Vyzkoušeno {recipeResult.TriedRecipes.Count} receptů: {triedRecipeNames}";
                }
                else
                {
                    // Data exists from previous sync, just log the failure
                    _logger.LogWarning(
                        "Recipe failed for {League} {Season} but existing data preserved ({Rounds} rounds)",
                        league.DisplayName, season.Name, leagueSeason.RoundsCount);
                }
                leagueSeason.LastRecipeTestedAt = DateTime.UtcNow;
                await _leagueSeasonRepository.UpdateAsync(leagueSeason);

                return Result<SyncResponse>.Success(new SyncResponse
                {
                    Success = false,
                    Message = $"No suitable recipe found for {league.DisplayName} - {season.Name}",
                    Statistics = new SyncStatistics
                    {
                        Skipped = 1
                    }
                });
            }

            var rounds = scrapeResult.Rounds;

            _logger.LogInformation(
                "Scraped {RoundCount} rounds for {LeagueName} {SeasonName} (Success: {IsSuccess}, Reason: {Reason})",
                rounds.Count, league.DisplayName, season.Name, scrapeResult.IsSuccess, scrapeResult.FailureReason);

            // Save rounds to database
            var savedCount = 0;
            var updatedCount = 0;
            var totalMatches = 0;
            var hasOdds = false;

            foreach (var round in rounds)
            {
                // Set metadata
                round.SeasonId = seasonId;
                round.ProviderId = providerId;

                // Check if round exists (including group name for leagues with groups)
                var existingRound = await _roundRepository.GetByLeagueSeasonRoundAsync(
                    leagueId, seasonId, round.RoundNumber, round.GroupName);

                if (existingRound == null)
                {
                    await _roundRepository.CreateAsync(round);
                    savedCount++;
                }
                else
                {
                    // Update existing round (for current seasons)
                    existingRound.MatchesCount = round.MatchesCount;
                    existingRound.HomeWins = round.HomeWins;
                    existingRound.Draws = round.Draws;
                    existingRound.AwayWins = round.AwayWins;
                    existingRound.CumulativeOddsHome = round.CumulativeOddsHome;
                    existingRound.CumulativeOddsDraw = round.CumulativeOddsDraw;
                    existingRound.CumulativeOddsAway = round.CumulativeOddsAway;
                    existingRound.SummaryResult = round.SummaryResult;
                    existingRound.OddsComplete = round.OddsComplete;
                    existingRound.ScrapedAt = DateTime.UtcNow;
                    existingRound.StartDate = round.StartDate;
                    existingRound.EndDate = round.EndDate;

                    await _roundRepository.UpdateAsync(existingRound);
                    updatedCount++;
                }

                totalMatches += round.MatchesCount;
                if (round.OddsComplete == "Yes")
                {
                    hasOdds = true;
                }
            }

            // Update league season metadata
            leagueSeason.HasData = scrapeResult.IsSuccess && rounds.Count > 0;
            leagueSeason.RoundsCount = rounds.Count;
            leagueSeason.MatchesCount = totalMatches;
            leagueSeason.HasOdds = hasOdds;
            leagueSeason.LastDataSyncAt = DateTime.UtcNow;
            leagueSeason.LastScrapedAt = DateTime.UtcNow;

            // Determine NoDataReason and Note
            if (!scrapeResult.IsSuccess)
            {
                leagueSeason.NoDataReason = scrapeResult.FailureReason;
                leagueSeason.NoDataNote = scrapeResult.ErrorMessage;
            }
            else if (scrapeResult.IsPartialData)
            {
                leagueSeason.NoDataReason = NoDataReason.PartialData;
                var missingRounds = scrapeResult.TotalRoundHeadersFound - rounds.Count;
                leagueSeason.NoDataNote = $"Načteno {rounds.Count} z {scrapeResult.TotalRoundHeadersFound} kol ({missingRounds} kol bez výsledků - pravděpodobně zrušeno)";
                _logger.LogWarning(
                    "Partial data for {League} season: {LoadedRounds} of {TotalRounds} rounds have data",
                    leagueId, rounds.Count, scrapeResult.TotalRoundHeadersFound);
            }
            else
            {
                leagueSeason.NoDataReason = null;
                leagueSeason.NoDataNote = null;
            }

            await _leagueSeasonRepository.UpdateAsync(leagueSeason);

            _logger.LogInformation(
                "Season sync completed: {Saved} new, {Updated} updated rounds, {TotalMatches} matches",
                savedCount, updatedCount, totalMatches);

            return Result<SyncResponse>.Success(new SyncResponse
            {
                Success = true,
                Message = $"Synced {rounds.Count} rounds ({totalMatches} matches)",
                Statistics = new SyncStatistics
                {
                    TotalProcessed = rounds.Count,
                    Created = savedCount,
                    Updated = updatedCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing season data");
            return Result<SyncResponse>.Failure($"Error syncing season data: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to scrape using recipes with adaptive fallback.
    /// Returns success if any recipe works, otherwise returns failure with tried recipes info.
    /// </summary>
    private async Task<RecipeScrapeAttempt> TryScrapeWithRecipesAsync(
        DataProvider provider,
        League league,
        Season season,
        LeagueSeason leagueSeason)
    {
        // Get all active recipes for BetExplorer results pages
        var recipes = await _recipeRepository.GetOrderedByPriorityAsync("betexplorer", "results");

        if (!recipes.Any())
        {
            _logger.LogWarning("No active recipes found for betexplorer/results");
            return RecipeScrapeAttempt.NoRecipes();
        }

        // If we have a last successful recipe, try it first
        if (leagueSeason.LastSuccessfulRecipeId.HasValue)
        {
            var lastSuccessful = recipes.FirstOrDefault(r => r.Id == leagueSeason.LastSuccessfulRecipeId);
            if (lastSuccessful != null)
            {
                recipes.Remove(lastSuccessful);
                recipes.Insert(0, lastSuccessful);
                _logger.LogInformation(
                    "Prioritizing last successful recipe '{RecipeName}' for {League} {Season}",
                    lastSuccessful.Name, league.DisplayName, season.Name);
            }
        }

        var triedRecipes = new List<TriedRecipeInfo>();
        var accumulatedHints = new Dictionary<string, string>();
        ScraperRecipe? firstNoRoundsFoundRecipe = null;
        ScrapeResult? firstNoRoundsFoundResult = null;
        ScraperRecipe? firstNoResultsRecipe = null;
        ScrapeResult? firstNoResultsResult = null;
        var countrySlug = league.Country?.Code?.ToLowerInvariant() ?? "unknown";
        var baseUrl = $"https://www.betexplorer.com/football/{countrySlug}/{league.BetExplorerSlug}/";

        var variables = new Dictionary<string, string>
        {
            ["baseUrl"] = baseUrl,
            ["season"] = season.Name
        };

        // Create a single ScraperDebugService instance for all recipe attempts
        await using var debugService = new ScraperDebugService(
            _logger as ILogger<ScraperDebugService> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ScraperDebugService>.Instance);

        foreach (var recipe in recipes)
        {
            // Skip recipe if RequiresHint not satisfied (bypass for LastSuccessfulRecipeId)
            var isLastSuccessful = leagueSeason.LastSuccessfulRecipeId == recipe.Id;
            if (!string.IsNullOrEmpty(recipe.RequiresHint) && !isLastSuccessful)
            {
                if (!accumulatedHints.TryGetValue(recipe.RequiresHint, out var hintValue) || hintValue != "true")
                {
                    _logger.LogInformation("Skipping recipe '{RecipeName}': hint '{Hint}' = '{Value}'",
                        recipe.Name, recipe.RequiresHint, accumulatedHints.GetValueOrDefault(recipe.RequiresHint, "(not set)"));
                    triedRecipes.Add(new TriedRecipeInfo
                    {
                        RecipeId = recipe.Id,
                        RecipeName = recipe.Name,
                        Error = $"Skipped: hint '{recipe.RequiresHint}' not satisfied",
                        DurationMs = 0
                    });
                    continue;
                }
            }

            _logger.LogInformation("Trying recipe '{RecipeName}' for {League} {Season}",
                recipe.Name, league.DisplayName, season.Name);

            // Retry logic for Playwright transient errors
            const int maxRetries = 2;
            RecipeExecutionResult? execResult = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                execResult = await _recipeExecutor.ExecuteRecipeAsync(debugService, recipe, variables);

                if (execResult.Success)
                    break;

                // Check if it's a transient Playwright error worth retrying
                var isTransientError = execResult.ErrorReason?.Contains("Execution context was destroyed") == true ||
                                       execResult.ErrorReason?.Contains("Target page, context or browser has been closed") == true ||
                                       execResult.ErrorReason?.Contains("Navigation") == true;

                if (isTransientError && attempt < maxRetries)
                {
                    _logger.LogWarning(
                        "Recipe '{RecipeName}' failed with transient error for {League} {Season}, retrying ({Attempt}/{MaxRetries}): {Error}",
                        recipe.Name, league.DisplayName, season.Name, attempt, maxRetries, execResult.ErrorReason);

                    // Small delay before retry
                    await Task.Delay(1000);

                    // Reinitialize browser for retry
                    await debugService.DisposeAsync();
                    await using var newDebugService = new ScraperDebugService(
                        _logger as ILogger<ScraperDebugService> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ScraperDebugService>.Instance);
                    execResult = await _recipeExecutor.ExecuteRecipeAsync(newDebugService, recipe, variables);

                    if (execResult.Success)
                        break;
                }
            }

            // Accumulate hints from this recipe's execution (regardless of success)
            foreach (var kv in execResult!.StoredVariables)
                accumulatedHints[kv.Key] = kv.Value;

            if (!execResult.Success)
            {
                _logger.LogWarning("Recipe '{RecipeName}' failed for {League} {Season}: {Error}",
                    recipe.Name, league.DisplayName, season.Name, execResult.ErrorReason);

                triedRecipes.Add(new TriedRecipeInfo
                {
                    RecipeId = recipe.Id,
                    RecipeName = recipe.Name,
                    Error = execResult.ErrorReason,
                    DurationMs = execResult.DurationMs
                });
                await _recipeRepository.IncrementStatsAsync(recipe.Id, success: false);
                continue;
            }

            // Parse the HTML using recipe rules
            var scrapeResult = _recipeExecutor.ParseHtmlWithRules(
                execResult.Html!, recipe, league.Id, season.Name);

            _logger.LogInformation(
                "Recipe '{RecipeName}' parsing result: Rounds={Rounds}, TotalHeaders={Headers}, TotalMatchRows={MatchRows}, FailureReason={Reason}, IsSuccess={IsSuccess}",
                recipe.Name, scrapeResult.Rounds.Count, scrapeResult.TotalRoundHeadersFound,
                scrapeResult.TotalMatchRowsFound, scrapeResult.FailureReason, scrapeResult.IsSuccess);

            if (scrapeResult.IsSuccess && scrapeResult.Rounds.Count > 0)
            {
                // Success! Update statistics and league season
                await _recipeRepository.IncrementStatsAsync(recipe.Id, success: true);

                leagueSeason.LastSuccessfulRecipeId = recipe.Id;
                leagueSeason.LastRecipeTestedAt = DateTime.UtcNow;
                await _leagueSeasonRepository.UpdateAsync(leagueSeason);

                return RecipeScrapeAttempt.Succeeded(recipe.Name, scrapeResult, triedRecipes);
            }

            // NoRoundsFound = page has match results but no round headers → TRY fallback (might be wrong tab, e.g. Main vs Apertura)
            // NoResults = empty page with no match results → TRY fallback (might be wrong tab selection)
            var isNoRoundsFound = scrapeResult.FailureReason == NoDataReason.NoRoundsFound;
            var isNoResults = scrapeResult.FailureReason == NoDataReason.NoResults;

            _logger.LogInformation(
                "Recipe '{RecipeName}' decision: FailureReason={Reason}, NoRoundsFound={NoRoundsFound}, NoResults={NoResults}",
                recipe.Name, scrapeResult.FailureReason, isNoRoundsFound, isNoResults);

            if (isNoRoundsFound)
            {
                // Page has match data but no round structure - could be wrong tab selection (e.g., Main vs Apertura/Clausura)
                // Try fallback recipes to see if different tab structure works.
                // Remember the first recipe that found matches - if no fallback produces rounds,
                // this is a valid "no rounds" result (e.g., old seasons without round data on BetExplorer).
                _logger.LogInformation(
                    "Recipe '{RecipeName}' for {League} {Season}: Page has {MatchCount} match results but no round structure. Trying fallback recipes.",
                    recipe.Name, league.DisplayName, season.Name, scrapeResult.TotalMatchRowsFound);

                if (firstNoRoundsFoundRecipe == null)
                {
                    firstNoRoundsFoundRecipe = recipe;
                    firstNoRoundsFoundResult = scrapeResult;
                }

                await _recipeRepository.IncrementStatsAsync(recipe.Id, success: false);
                triedRecipes.Add(new TriedRecipeInfo
                {
                    RecipeId = recipe.Id,
                    RecipeName = recipe.Name,
                    Error = $"No round structure found ({scrapeResult.TotalMatchRowsFound} matches), trying fallback",
                    DurationMs = execResult.DurationMs
                });
                continue;
            }

            if (isNoResults)
            {
                // Page has no match results - try fallback recipes (could be wrong tab selection)
                _logger.LogInformation(
                    "Recipe '{RecipeName}' for {League} {Season}: No results found. Trying fallback recipes.",
                    recipe.Name, league.DisplayName, season.Name);

                if (firstNoResultsRecipe == null)
                {
                    firstNoResultsRecipe = recipe;
                    firstNoResultsResult = scrapeResult;
                }

                await _recipeRepository.IncrementStatsAsync(recipe.Id, success: false);

                triedRecipes.Add(new TriedRecipeInfo
                {
                    RecipeId = recipe.Id,
                    RecipeName = recipe.Name,
                    Error = "No results found, trying fallback",
                    DurationMs = execResult.DurationMs
                });

                // Continue to next recipe
                continue;
            }

            // Parsing failed - try next recipe
            triedRecipes.Add(new TriedRecipeInfo
            {
                RecipeId = recipe.Id,
                RecipeName = recipe.Name,
                Error = $"Parsing failed: {scrapeResult.ErrorMessage}",
                DurationMs = execResult.DurationMs
            });
            await _recipeRepository.IncrementStatsAsync(recipe.Id, success: false);
        }

        // No recipe produced rounds. If a recipe found matches but no round structure,
        // treat it as a valid result - the page exists but has no rounds (e.g., old seasons on BetExplorer).
        if (firstNoRoundsFoundRecipe != null && firstNoRoundsFoundResult != null)
        {
            _logger.LogInformation(
                "No recipe found rounds for {League} {Season}, but '{RecipeName}' found {MatchCount} matches without round structure. Treating as valid NoRoundsFound result.",
                league.DisplayName, season.Name, firstNoRoundsFoundRecipe.Name, firstNoRoundsFoundResult.TotalMatchRowsFound);

            await _recipeRepository.IncrementStatsAsync(firstNoRoundsFoundRecipe.Id, success: true);
            leagueSeason.LastSuccessfulRecipeId = firstNoRoundsFoundRecipe.Id;
            leagueSeason.LastRecipeTestedAt = DateTime.UtcNow;
            await _leagueSeasonRepository.UpdateAsync(leagueSeason);

            return RecipeScrapeAttempt.FailedWithResult(firstNoRoundsFoundResult, triedRecipes);
        }

        // No recipe found any results. If a recipe got NoResults (page exists but empty),
        // treat it as a valid result - e.g., cancelled season, no data on BetExplorer.
        if (firstNoResultsRecipe != null && firstNoResultsResult != null)
        {
            _logger.LogInformation(
                "No recipe found results for {League} {Season}. Page exists but has no match data (e.g., cancelled season). First recipe: '{RecipeName}'.",
                league.DisplayName, season.Name, firstNoResultsRecipe.Name);

            leagueSeason.LastRecipeTestedAt = DateTime.UtcNow;
            leagueSeason.LastSuccessfulRecipeId = firstNoResultsRecipe.Id;
            await _leagueSeasonRepository.UpdateAsync(leagueSeason);

            return RecipeScrapeAttempt.FailedWithResult(firstNoResultsResult, triedRecipes);
        }

        // No recipe worked at all
        leagueSeason.LastRecipeTestedAt = DateTime.UtcNow;
        leagueSeason.LastSuccessfulRecipeId = null;
        await _leagueSeasonRepository.UpdateAsync(leagueSeason);

        _logger.LogWarning(
            "No suitable recipe found for {League} {Season}. Tried {Count} recipes.",
            league.DisplayName, season.Name, recipes.Count);

        return RecipeScrapeAttempt.Failed(triedRecipes);
    }

    public async Task<Result<SyncResponse>> SyncAllMarkedSeasonsDataAsync(Guid providerId)
    {
        try
        {
            _logger.LogInformation("Syncing all marked seasons for provider {ProviderId}", providerId);

            // Get all sync-enabled league seasons (excluding locked and ignored ones)
            var allSyncEnabledSeasons = await _leagueSeasonRepository.GetSyncEnabledAsync();
            var lockedCount = allSyncEnabledSeasons.Count(s => s.IsLocked);
            var ignoredCount = allSyncEnabledSeasons.Count(s => s.IsIgnored);
            var syncEnabledSeasons = allSyncEnabledSeasons.Where(s => !s.IsLocked && !s.IsIgnored).ToList();

            _logger.LogInformation(
                "Found {Count} seasons marked for sync ({LockedCount} locked, {IgnoredCount} ignored, skipped)",
                syncEnabledSeasons.Count, lockedCount, ignoredCount);

            if (!syncEnabledSeasons.Any())
            {
                return Result<SyncResponse>.Success(new SyncResponse
                {
                    Success = true,
                    Message = "No seasons marked for synchronization",
                    Statistics = new SyncStatistics()
                });
            }

            // Aggregate statistics
            var totalCreated = 0;
            var totalUpdated = 0;
            var totalSkipped = 0;
            var totalFailed = 0;
            var errors = new List<string>();

            // Process each season
            foreach (var leagueSeason in syncEnabledSeasons)
            {
                try
                {
                    var result = await SyncSeasonDataAsync(
                        providerId,
                        leagueSeason.LeagueId,
                        leagueSeason.SeasonId,
                        forceUpdate: false);

                    if (result.IsSuccess && result.Value != null)
                    {
                        totalCreated += result.Value.Statistics.Created;
                        totalUpdated += result.Value.Statistics.Updated;
                        totalSkipped += result.Value.Statistics.Skipped;
                    }
                    else
                    {
                        totalFailed++;
                        var errorMsg = $"{leagueSeason.League?.DisplayName} - {leagueSeason.Season?.Name}: {result.Error}";
                        errors.Add(errorMsg);
                        _logger.LogError("Failed to sync season: {Error}", errorMsg);
                    }
                }
                catch (Exception ex)
                {
                    totalFailed++;
                    var errorMsg = $"{leagueSeason.League?.DisplayName} - {leagueSeason.Season?.Name}: {ex.Message}";
                    errors.Add(errorMsg);
                    _logger.LogError(ex, "Error syncing season");
                }
            }

            var message = $"Synced {syncEnabledSeasons.Count} seasons: {totalCreated} created, {totalUpdated} updated, {totalSkipped} skipped, {totalFailed} failed";

            _logger.LogInformation(message);

            return Result<SyncResponse>.Success(new SyncResponse
            {
                Success = totalFailed == 0,
                Message = message,
                Statistics = new SyncStatistics
                {
                    TotalProcessed = syncEnabledSeasons.Count,
                    Created = totalCreated,
                    Updated = totalUpdated,
                    Skipped = totalSkipped,
                    Errors = totalFailed,
                    ErrorMessages = errors
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all marked seasons");
            return Result<SyncResponse>.Failure($"Error syncing all marked seasons: {ex.Message}");
        }
    }

    /// <summary>
    /// Internal result type for recipe scraping attempts
    /// </summary>
    private class RecipeScrapeAttempt
    {
        public bool Success { get; set; }
        public string? SuccessfulRecipeName { get; set; }
        public ScrapeResult? ScrapeResult { get; set; }
        public List<TriedRecipeInfo> TriedRecipes { get; set; } = new();
        public bool NoRecipesAvailable { get; set; }

        public static RecipeScrapeAttempt NoRecipes()
        {
            return new RecipeScrapeAttempt { NoRecipesAvailable = true };
        }

        public static RecipeScrapeAttempt Succeeded(string recipeName, ScrapeResult result, List<TriedRecipeInfo> tried)
        {
            return new RecipeScrapeAttempt
            {
                Success = true,
                SuccessfulRecipeName = recipeName,
                ScrapeResult = result,
                TriedRecipes = tried
            };
        }

        public static RecipeScrapeAttempt Failed(List<TriedRecipeInfo> tried)
        {
            return new RecipeScrapeAttempt
            {
                Success = false,
                TriedRecipes = tried
            };
        }

        public static RecipeScrapeAttempt FailedWithResult(ScrapeResult result, List<TriedRecipeInfo> tried)
        {
            return new RecipeScrapeAttempt
            {
                Success = false,
                ScrapeResult = result,
                TriedRecipes = tried
            };
        }
    }
}
