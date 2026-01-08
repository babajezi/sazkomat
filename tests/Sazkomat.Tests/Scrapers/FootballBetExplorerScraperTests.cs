using Moq;
using Microsoft.Extensions.Logging;
using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Scrapers;

namespace Sazkomat.Tests.Scrapers;

public class FootballBetExplorerScraperTests
{
    private readonly Mock<IHttpClient> _mockHttpClient;
    private readonly Mock<ILogger<FootballBetExplorerScraper>> _mockLogger;
    private readonly FootballBetExplorerScraper _scraper;
    private readonly Sport _footballSport;
    private readonly League _league;

    public FootballBetExplorerScraperTests()
    {
        _mockHttpClient = new Mock<IHttpClient>();
        _mockLogger = new Mock<ILogger<FootballBetExplorerScraper>>();
        _scraper = new FootballBetExplorerScraper(_mockHttpClient.Object, _mockLogger.Object);

        _footballSport = new Sport
        {
            Id = Guid.NewGuid(),
            Name = "Football",
            Code = "football",
            IsActive = true
        };

        _league = new League
        {
            Id = Guid.NewGuid(),
            Name = "Premier League",
            BetExplorerSlug = "premier-league",
            SportId = _footballSport.Id,
            CountryId = Guid.NewGuid(),
            IsActive = true,
            Country = new Country
            {
                Id = Guid.NewGuid(),
                Name = "England",
                Code = "england",
                IsoCode = "GB-ENG"
            }
        };
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public void CanHandle_FootballSport_ReturnsTrue()
    {
        // Act
        var result = _scraper.CanHandle(_footballSport);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public void CanHandle_BasketballSport_ReturnsFalse()
    {
        // Arrange
        var basketballSport = new Sport
        {
            Id = Guid.NewGuid(),
            Name = "Basketball",
            Code = "basketball",
            IsActive = true
        };

        // Act
        var result = _scraper.CanHandle(basketballSport);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task ScrapeSeasonAsync_NoResultsContainer_ReturnsEmptyList()
    {
        // Arrange
        var html = @"<html><body><div>No results found</div></body></html>";

        _mockHttpClient.Setup(c => c.GetHtmlAsync(It.IsAny<string>()))
            .ReturnsAsync(html);

        // Act
        var rounds = await _scraper.ScrapeSeasonAsync(_league, "2023-2024");

        // Assert
        Assert.Empty(rounds);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task ScrapeSeasonAsync_ValidHtml_ParsesRounds()
    {
        // Arrange - using HTML format that matches actual BetExplorer structure
        var html = @"
<html>
<body>
    <div id='js-leagueresults-all'>
        <table class='table-main'>
            <tr>
                <th>Round 1</th>
            </tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/abc123/'>Manchester United - Wolves</a></td>
                <td class='table-main__result'>1:0</td>
                <td class='table-main__odds'><a href='/match/abc123/'>1.57</a></td>
                <td class='table-main__odds'><a href='/match/abc123/'>4.00</a></td>
                <td class='table-main__odds'><a href='/match/abc123/'>5.50</a></td>
            </tr>
            <tr>
                <td class='table-main__datetime'>11.08.2023</td>
                <td><a class='in-match' href='/match/def456/'>Arsenal - Liverpool</a></td>
                <td class='table-main__result'>2:2</td>
                <td class='table-main__odds'><a href='/match/def456/'>2.10</a></td>
                <td class='table-main__odds'><a href='/match/def456/'>3.50</a></td>
                <td class='table-main__odds'><a href='/match/def456/'>3.20</a></td>
            </tr>
        </table>
    </div>
</body>
</html>";

        _mockHttpClient.Setup(c => c.GetHtmlAsync(It.IsAny<string>()))
            .ReturnsAsync(html);

        // Act
        var rounds = await _scraper.ScrapeSeasonAsync(_league, "2023-2024");

        // Assert
        Assert.Single(rounds);
        var round = rounds[0];
        Assert.Equal(1, round.RoundNumber);
        Assert.Equal(2, round.MatchesCount);
        Assert.Equal(1, round.HomeWins);  // Man Utd won
        Assert.Equal(1, round.Draws);     // Arsenal drew
        Assert.Equal(0, round.AwayWins);
        Assert.Equal("1-1-0", round.SummaryResult);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task ScrapeSeasonAsync_MultipleRounds_ParsesCorrectly()
    {
        // Arrange
        var html = @"
<html>
<body>
    <div id='js-leagueresults-all'>
        <table class='table-main'>
            <tr><th>Round 1</th></tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/1/'>Team A - Team B</a></td>
                <td class='table-main__result'>1:0</td>
                <td class='table-main__odds'><a href='/match/1/'>1.50</a></td>
                <td class='table-main__odds'><a href='/match/1/'>4.00</a></td>
                <td class='table-main__odds'><a href='/match/1/'>6.00</a></td>
            </tr>
        </table>
        <table class='table-main'>
            <tr><th>Round 2</th></tr>
            <tr>
                <td class='table-main__datetime'>17.08.2023</td>
                <td><a class='in-match' href='/match/2/'>Team C - Team D</a></td>
                <td class='table-main__result'>0:1</td>
                <td class='table-main__odds'><a href='/match/2/'>2.00</a></td>
                <td class='table-main__odds'><a href='/match/2/'>3.00</a></td>
                <td class='table-main__odds'><a href='/match/2/'>3.50</a></td>
            </tr>
        </table>
    </div>
</body>
</html>";

        _mockHttpClient.Setup(c => c.GetHtmlAsync(It.IsAny<string>()))
            .ReturnsAsync(html);

        // Act
        var rounds = await _scraper.ScrapeSeasonAsync(_league, "2023-2024");

        // Assert
        Assert.Equal(2, rounds.Count);

        var round1 = rounds.First(r => r.RoundNumber == 1);
        Assert.Equal(1, round1.MatchesCount);
        Assert.Equal(1, round1.HomeWins);

        var round2 = rounds.First(r => r.RoundNumber == 2);
        Assert.Equal(1, round2.MatchesCount);
        Assert.Equal(1, round2.AwayWins);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task ScrapeSeasonAsync_OddsCalculation_CorrectlySumsCumulativeOdds()
    {
        // Arrange
        var html = @"
<html>
<body>
    <div id='js-leagueresults-all'>
        <table class='table-main'>
            <tr><th>Round 1</th></tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/1/'>Team A - Team B</a></td>
                <td class='table-main__result'>1:0</td>
                <td class='table-main__odds'><a href='/match/1/'>2.00</a></td>
                <td class='table-main__odds'><a href='/match/1/'>3.00</a></td>
                <td class='table-main__odds'><a href='/match/1/'>4.00</a></td>
            </tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/2/'>Team C - Team D</a></td>
                <td class='table-main__result'>2:2</td>
                <td class='table-main__odds'><a href='/match/2/'>1.50</a></td>
                <td class='table-main__odds'><a href='/match/2/'>3.50</a></td>
                <td class='table-main__odds'><a href='/match/2/'>5.00</a></td>
            </tr>
        </table>
    </div>
</body>
</html>";

        _mockHttpClient.Setup(c => c.GetHtmlAsync(It.IsAny<string>()))
            .ReturnsAsync(html);

        // Act
        var rounds = await _scraper.ScrapeSeasonAsync(_league, "2023-2024");

        // Assert
        Assert.Single(rounds);
        var round = rounds[0];
        // Cumulative odds are MULTIPLIED, not summed
        Assert.Equal(3.00m, round.CumulativeOddsHome);  // 2.00 * 1.50 = 3.00
        Assert.Equal(10.50m, round.CumulativeOddsDraw); // 3.00 * 3.50 = 10.50
        Assert.Equal(20.00m, round.CumulativeOddsAway); // 4.00 * 5.00 = 20.00
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task ScrapeSeasonAsync_MissingOdds_HandlesGracefully()
    {
        // Arrange
        var html = @"
<html>
<body>
    <div id='js-leagueresults-all'>
        <table class='table-main'>
            <tr><th>Round 1</th></tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/1/'>Team A - Team B</a></td>
                <td class='table-main__result'>1:0</td>
                <td class='table-main__odds'>-</td>
                <td class='table-main__odds'>-</td>
                <td class='table-main__odds'>-</td>
            </tr>
        </table>
    </div>
</body>
</html>";

        _mockHttpClient.Setup(c => c.GetHtmlAsync(It.IsAny<string>()))
            .ReturnsAsync(html);

        // Act
        var rounds = await _scraper.ScrapeSeasonAsync(_league, "2023-2024");

        // Assert
        Assert.Single(rounds);
        var round = rounds[0];
        Assert.Equal(1, round.MatchesCount);
        // When any match is missing odds, the round shows "Partial" (not all matches have complete odds)
        Assert.Equal("Partial", round.OddsComplete); // Should indicate incomplete odds
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task ScrapeSeasonAsync_PostponedMatch_ParsesCorrectly()
    {
        // Arrange - postponed matches don't have valid score, so only completed match should count
        var html = @"
<html>
<body>
    <div id='js-leagueresults-all'>
        <table class='table-main'>
            <tr><th>Round 1</th></tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/1/'>Team A - Team B</a></td>
                <td class='table-main__result'>postp.</td>
                <td class='table-main__odds'>-</td>
                <td class='table-main__odds'>-</td>
                <td class='table-main__odds'>-</td>
            </tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/2/'>Team C - Team D</a></td>
                <td class='table-main__result'>1:0</td>
                <td class='table-main__odds'><a href='/match/2/'>2.00</a></td>
                <td class='table-main__odds'><a href='/match/2/'>3.00</a></td>
                <td class='table-main__odds'><a href='/match/2/'>4.00</a></td>
            </tr>
        </table>
    </div>
</body>
</html>";

        _mockHttpClient.Setup(c => c.GetHtmlAsync(It.IsAny<string>()))
            .ReturnsAsync(html);

        // Act
        var rounds = await _scraper.ScrapeSeasonAsync(_league, "2023-2024");

        // Assert
        Assert.Single(rounds);
        var round = rounds[0];
        // Postponed match is skipped by the parser (no valid score)
        Assert.Equal(1, round.MatchesCount);
        Assert.Equal(1, round.HomeWins);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task ScrapeSeasonAsync_SeasonFormatConversion_UsesCorrectUrl()
    {
        // Arrange
        var capturedUrl = "";
        var html = @"<html><body><div id='js-leagueresults-all'></div></body></html>";

        _mockHttpClient.Setup(c => c.GetHtmlAsync(It.IsAny<string>()))
            .Callback<string>(url => capturedUrl = url)
            .ReturnsAsync(html);

        // Act
        await _scraper.ScrapeSeasonAsync(_league, "2023/2024");

        // Assert
        Assert.Contains("2023-2024", capturedUrl); // Should convert / to -
        Assert.Contains("/football/", capturedUrl);
        Assert.Contains("/results/", capturedUrl);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task ScrapeSeasonAsync_ResultParsing_IdentifiesWinDrawLoss()
    {
        // Arrange
        var html = @"
<html>
<body>
    <div id='js-leagueresults-all'>
        <table class='table-main'>
            <tr><th>Round 1</th></tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/1/'>Team A - Team B</a></td>
                <td class='table-main__result'>2:1</td>
                <td class='table-main__odds'><a href='/match/1/'>1.80</a></td>
                <td class='table-main__odds'><a href='/match/1/'>3.20</a></td>
                <td class='table-main__odds'><a href='/match/1/'>4.50</a></td>
            </tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/2/'>Team C - Team D</a></td>
                <td class='table-main__result'>1:1</td>
                <td class='table-main__odds'><a href='/match/2/'>2.00</a></td>
                <td class='table-main__odds'><a href='/match/2/'>3.00</a></td>
                <td class='table-main__odds'><a href='/match/2/'>3.50</a></td>
            </tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/3/'>Team E - Team F</a></td>
                <td class='table-main__result'>0:2</td>
                <td class='table-main__odds'><a href='/match/3/'>2.50</a></td>
                <td class='table-main__odds'><a href='/match/3/'>3.10</a></td>
                <td class='table-main__odds'><a href='/match/3/'>2.80</a></td>
            </tr>
        </table>
    </div>
</body>
</html>";

        _mockHttpClient.Setup(c => c.GetHtmlAsync(It.IsAny<string>()))
            .ReturnsAsync(html);

        // Act
        var rounds = await _scraper.ScrapeSeasonAsync(_league, "2023-2024");

        // Assert
        Assert.Single(rounds);
        var round = rounds[0];
        Assert.Equal(3, round.MatchesCount);
        Assert.Equal(1, round.HomeWins);  // Team A won 2:1
        Assert.Equal(1, round.Draws);     // Team C drew 1:1
        Assert.Equal(1, round.AwayWins);  // Team F won 0:2
        Assert.Equal("1-1-1", round.SummaryResult);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task ScrapeSeasonAsync_MatchDetails_PopulatedCorrectly()
    {
        // Arrange
        var html = @"
<html>
<body>
    <div id='js-leagueresults-all'>
        <table class='table-main'>
            <tr><th>Round 1</th></tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td><a class='in-match' href='/match/abc123/'>Manchester United - Wolves</a></td>
                <td class='table-main__result'>3:1</td>
                <td class='table-main__odds'><a href='/match/abc123/'>1.65</a></td>
                <td class='table-main__odds'><a href='/match/abc123/'>3.80</a></td>
                <td class='table-main__odds'><a href='/match/abc123/'>5.25</a></td>
            </tr>
        </table>
    </div>
</body>
</html>";

        _mockHttpClient.Setup(c => c.GetHtmlAsync(It.IsAny<string>()))
            .ReturnsAsync(html);

        // Act
        var rounds = await _scraper.ScrapeSeasonAsync(_league, "2023-2024");

        // Assert
        Assert.Single(rounds);
        var round = rounds[0];
        Assert.Single(round.Matches);

        var match = round.Matches.First();
        Assert.Equal("Manchester United", match.HomeTeam);
        Assert.Equal("Wolves", match.AwayTeam);
        Assert.Equal(3, match.HomeScore);
        Assert.Equal(1, match.AwayScore);
        Assert.Equal("H", match.Result); // Home win
        Assert.Equal(1.65m, match.HomeOdds);
        Assert.Equal(3.80m, match.DrawOdds);
        Assert.Equal(5.25m, match.AwayOdds);
    }
}
