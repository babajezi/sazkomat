using Microsoft.Extensions.Logging;
using Moq;
using Sazkomat.Configuration.Entities;
using Sazkomat.Data.Scrapers;
using Xunit;
using Xunit.Abstractions;

namespace Sazkomat.Tests.Scrapers;

/// <summary>
/// Manual test pro scraper - vyžaduje internet připojení a volá skutečnou webovou stránku.
/// Tento test je označen jako [Fact(Skip = ...)] protože volá reálné API.
/// Pro spuštění odstraňte Skip parametr.
/// </summary>
public class FootballBetExplorerScraperManualTest
{
    private readonly ITestOutputHelper _output;

    public FootballBetExplorerScraperManualTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual test - requires internet and calls real BetExplorer.com")]
    public async Task ScrapeSeasonAsync_RealData_PremierLeague2023_2024()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<FootballBetExplorerScraper>>();
        var httpClient = new HttpClient();
        var resilientClient = new ResilientHttpClient(httpClient, Mock.Of<ILogger<ResilientHttpClient>>());
        var scraper = new FootballBetExplorerScraper(resilientClient, mockLogger.Object);

        var league = new League
        {
            Id = Guid.NewGuid(),
            Name = "Premier League",
            BetExplorerSlug = "england/premier-league",
            SportId = Guid.NewGuid(),
            CountryId = Guid.NewGuid(),
            Country = new Country
            {
                Id = Guid.NewGuid(),
                Name = "England",
                Code = "ENG",
                FlagEmoji = "🏴"
            }
        };

        // Act
        var rounds = await scraper.ScrapeSeasonAsync(league, "2023/2024");

        // Assert
        Assert.NotEmpty(rounds);
        _output.WriteLine($"Scraped {rounds.Count} rounds");

        foreach (var round in rounds.Take(5)) // Show first 5 rounds
        {
            _output.WriteLine($"Round {round.RoundNumber}: {round.SummaryResult} " +
                            $"(Matches: {round.MatchesCount}, Odds Complete: {round.OddsComplete})");
            _output.WriteLine($"  Cumulative Odds - H: {round.CumulativeOddsHome:F2}, " +
                            $"D: {round.CumulativeOddsDraw:F2}, A: {round.CumulativeOddsAway:F2}");
        }

        // Verify basic data structure
        var firstRound = rounds.First();
        Assert.True(firstRound.RoundNumber > 0);
        Assert.True(firstRound.MatchesCount > 0);
        // Note: Season validation removed as Round.Season is now a navigation property, not a string
        Assert.Contains(firstRound.OddsComplete, new[] { "Yes", "Partial", "No" });

        // Verify cumulative odds are calculated
        Assert.True(firstRound.CumulativeOddsHome > 0);
        Assert.True(firstRound.CumulativeOddsDraw > 0);
        Assert.True(firstRound.CumulativeOddsAway > 0);

        // Verify match counts add up
        Assert.Equal(firstRound.MatchesCount,
                    firstRound.HomeWins + firstRound.Draws + firstRound.AwayWins);
    }
}
