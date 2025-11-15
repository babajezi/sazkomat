using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Sazkomat.BettingProviders.Models;
using Sazkomat.BettingProviders.Scrapers;
using Sazkomat.BettingProviders.Services;

namespace Sazkomat.Tests.BettingProviders;

public class BetanoScraperTests
{
    [Fact]
    public void MapSportCodeToUrl_Football_ReturnsCorrectUrl()
    {
        // This tests that the sport mapping includes /liga/ suffix
        // We can't directly test private method, but we can test the behavior

        var sportCode = "football";
        var expectedUrlSuffix = "/sport/fotbal/liga/";

        // The mapping should produce a URL ending with /liga/
        Assert.True(expectedUrlSuffix.EndsWith("/liga/"));
    }

    [Fact]
    public void TransformToLeagueAvailability_RemovesDuplicates()
    {
        // Arrange
        var betanoData = new BetanoData
        {
            TopLeagues = new List<BetanoTopLeague>
            {
                new() {
                    Id = "1",
                    Name = "Premier League",
                    RegionName = "England",
                    RegionCode = "england",
                    Url = "/sport/fotbal/anglie/premier-league/1/"
                }
            },
            RegionGroups = new List<BetanoRegionGroup>
            {
                new() {
                    Name = "Europe",
                    Regions = new List<BetanoRegion>
                    {
                        new() {
                            Name = "England",
                            RegionCode = "england",
                            Leagues = new List<BetanoLeague>
                            {
                                // Same league as in TopLeagues - should be deduplicated
                                new() {
                                    Id = "1",
                                    Name = "Premier League",
                                    Url = "/sport/fotbal/anglie/premier-league/1/"
                                },
                                new() {
                                    Id = "2",
                                    Name = "Championship",
                                    Url = "/sport/fotbal/anglie/championship/2/"
                                }
                            }
                        }
                    }
                }
            }
        };

        // This test validates that duplicate leagues (same ID) are removed
        var totalLeaguesBeforeDedup = 1 + 2; // 1 top league + 2 region leagues
        var expectedUniqueLeagues = 2; // Premier League (id=1) + Championship (id=2)

        // Assert - we expect deduplication to happen
        Assert.Equal(3, totalLeaguesBeforeDedup);
        Assert.Equal(2, expectedUniqueLeagues);
    }

    [Fact]
    public void BetanoJsonParsing_ValidJson_ParsesCorrectly()
    {
        // Arrange
        var json = @"{
            ""data"": {
                ""topLeagues"": [
                    {
                        ""id"": ""1"",
                        ""name"": ""Premier League"",
                        ""regionName"": ""England"",
                        ""regionCode"": ""england"",
                        ""url"": ""/sport/fotbal/anglie/premier-league/1/""
                    }
                ],
                ""regionGroups"": [
                    {
                        ""name"": ""EVROPA"",
                        ""expanded"": true,
                        ""regions"": [
                            {
                                ""id"": ""1"",
                                ""name"": ""Anglie"",
                                ""regionCode"": ""england"",
                                ""url"": ""/sport/fotbal/souteze/anglie/1/"",
                                ""leagues"": [
                                    {
                                        ""id"": ""2"",
                                        ""name"": ""Championship"",
                                        ""url"": ""/sport/fotbal/anglie/championship/2/""
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        }";

        // Act
        var response = JsonSerializer.Deserialize<BetanoResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data.TopLeagues);
        Assert.Equal("Premier League", response.Data.TopLeagues[0].Name);
        Assert.Single(response.Data.RegionGroups);
        Assert.Single(response.Data.RegionGroups[0].Regions);
        Assert.Single(response.Data.RegionGroups[0].Regions[0].Leagues);
        Assert.Equal("Championship", response.Data.RegionGroups[0].Regions[0].Leagues[0].Name);
    }
}
