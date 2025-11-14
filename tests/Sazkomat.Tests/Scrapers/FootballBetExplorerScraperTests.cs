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
            IsSyncEnabled = true,
            Country = new Country
            {
                Id = Guid.NewGuid(),
                Name = "England",
                Code = "england",
                IsoCode = "GB-ENG"
            }
        };
    }

    [Fact]
    public void CanHandle_FootballSport_ReturnsTrue()
    {
        // Act
        var result = _scraper.CanHandle(_footballSport);

        // Assert
        Assert.True(result);
    }

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

    [Fact]
    public async Task ScrapeSeasonAsync_ValidHtml_ParsesRounds()
    {
        // Arrange
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
                <td class='table-main__team table-main__team--home'>Manchester United</td>
                <td class='table-main__result'>
                    <a href='/match/abc123/'>1:0</a>
                </td>
                <td class='table-main__team table-main__team--away'>Wolves</td>
                <td class='table-main__odds'>
                    <a href='/match/abc123/'>1.57</a>
                </td>
                <td class='table-main__odds'>
                    <a href='/match/abc123/'>4.00</a>
                </td>
                <td class='table-main__odds'>
                    <a href='/match/abc123/'>5.50</a>
                </td>
            </tr>
            <tr>
                <td class='table-main__datetime'>11.08.2023</td>
                <td class='table-main__team table-main__team--home'>Arsenal</td>
                <td class='table-main__result'>
                    <a href='/match/def456/'>2:2</a>
                </td>
                <td class='table-main__team table-main__team--away'>Liverpool</td>
                <td class='table-main__odds'>
                    <a href='/match/def456/'>2.10</a>
                </td>
                <td class='table-main__odds'>
                    <a href='/match/def456/'>3.50</a>
                </td>
                <td class='table-main__odds'>
                    <a href='/match/def456/'>3.20</a>
                </td>
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
                <td class='table-main__team table-main__team--home'>Team A</td>
                <td class='table-main__result'><a href='/match/1/'>1:0</a></td>
                <td class='table-main__team table-main__team--away'>Team B</td>
                <td class='table-main__odds'><a href='/match/1/'>1.50</a></td>
                <td class='table-main__odds'><a href='/match/1/'>4.00</a></td>
                <td class='table-main__odds'><a href='/match/1/'>6.00</a></td>
            </tr>
        </table>
        <table class='table-main'>
            <tr><th>Round 2</th></tr>
            <tr>
                <td class='table-main__datetime'>17.08.2023</td>
                <td class='table-main__team table-main__team--home'>Team C</td>
                <td class='table-main__result'><a href='/match/2/'>0:1</a></td>
                <td class='table-main__team table-main__team--away'>Team D</td>
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
                <td class='table-main__team table-main__team--home'>Team A</td>
                <td class='table-main__result'><a href='/match/1/'>1:0</a></td>
                <td class='table-main__team table-main__team--away'>Team B</td>
                <td class='table-main__odds'><a href='/match/1/'>2.00</a></td>
                <td class='table-main__odds'><a href='/match/1/'>3.00</a></td>
                <td class='table-main__odds'><a href='/match/1/'>4.00</a></td>
            </tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td class='table-main__team table-main__team--home'>Team C</td>
                <td class='table-main__result'><a href='/match/2/'>2:2</a></td>
                <td class='table-main__team table-main__team--away'>Team D</td>
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
        Assert.Equal(3.50m, round.CumulativeOddsHome);  // 2.00 + 1.50
        Assert.Equal(6.50m, round.CumulativeOddsDraw);  // 3.00 + 3.50
        Assert.Equal(9.00m, round.CumulativeOddsAway);  // 4.00 + 5.00
    }

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
                <td class='table-main__team table-main__team--home'>Team A</td>
                <td class='table-main__result'><a href='/match/1/'>1:0</a></td>
                <td class='table-main__team table-main__team--away'>Team B</td>
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
        Assert.Equal("No", round.OddsComplete); // Should indicate incomplete odds
    }

    [Fact]
    public async Task ScrapeSeasonAsync_PostponedMatch_ParsesCorrectly()
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
                <td class='table-main__team table-main__team--home'>Team A</td>
                <td class='table-main__result'><a href='/match/1/'>postp.</a></td>
                <td class='table-main__team table-main__team--away'>Team B</td>
                <td class='table-main__odds'>-</td>
                <td class='table-main__odds'>-</td>
                <td class='table-main__odds'>-</td>
            </tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td class='table-main__team table-main__team--home'>Team C</td>
                <td class='table-main__result'><a href='/match/2/'>1:0</a></td>
                <td class='table-main__team table-main__team--away'>Team D</td>
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
        Assert.Equal(2, round.MatchesCount); // Both matches counted
        Assert.Equal(1, round.HomeWins); // Only completed match
    }

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
                <td class='table-main__team table-main__team--home'>Team A</td>
                <td class='table-main__result'><a href='/match/1/'>2:1</a></td>
                <td class='table-main__team table-main__team--away'>Team B</td>
                <td class='table-main__odds'><a href='/match/1/'>1.80</a></td>
                <td class='table-main__odds'><a href='/match/1/'>3.20</a></td>
                <td class='table-main__odds'><a href='/match/1/'>4.50</a></td>
            </tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td class='table-main__team table-main__team--home'>Team C</td>
                <td class='table-main__result'><a href='/match/2/'>1:1</a></td>
                <td class='table-main__team table-main__team--away'>Team D</td>
                <td class='table-main__odds'><a href='/match/2/'>2.00</a></td>
                <td class='table-main__odds'><a href='/match/2/'>3.00</a></td>
                <td class='table-main__odds'><a href='/match/2/'>3.50</a></td>
            </tr>
            <tr>
                <td class='table-main__datetime'>10.08.2023</td>
                <td class='table-main__team table-main__team--home'>Team E</td>
                <td class='table-main__result'><a href='/match/3/'>0:2</a></td>
                <td class='table-main__team table-main__team--away'>Team F</td>
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
                <td class='table-main__team table-main__team--home'>Manchester United</td>
                <td class='table-main__result'><a href='/match/abc123/'>3:1</a></td>
                <td class='table-main__team table-main__team--away'>Wolves</td>
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

        var match = round.Matches[0];
        Assert.Equal("Manchester United", match.HomeTeam);
        Assert.Equal("Wolves", match.AwayTeam);
        Assert.Equal("3:1", match.Score);
        Assert.Equal("H", match.Result); // Home win
        Assert.Equal(1.65m, match.Odds1);
        Assert.Equal(3.80m, match.OddsX);
        Assert.Equal(5.25m, match.Odds2);
    }
}
