using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data;

public static class ConfigurationSeeder
{
    public static async Task SeedAsync(ConfigurationDbContext context)
    {
        // Always check and update Data Providers (even if already seeded)
        var betExplorer = await context.DataProviders.FindAsync(Guid.Parse("a0000000-0000-0000-0000-000000000001"));
        if (betExplorer != null && (string.IsNullOrEmpty(betExplorer.CurrentSeasonPatterns) || betExplorer.CurrentSeasonPatterns == "[]"))
        {
            // Update existing provider with current season patterns
            betExplorer.CurrentSeasonPatterns = "[\"2025\",\"2025-2026\"]";
            context.DataProviders.Update(betExplorer);
            await context.SaveChangesAsync();
        }

        // Create Data Providers (check existence for each one)
        if (betExplorer == null)
        {
            betExplorer = new DataProvider
        {
            Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
            Name = "BetExplorer",
            Code = "betexplorer",
            BaseUrl = "https://www.betexplorer.com",
            IsActive = true,
            Priority = 10,
            Type = ProviderType.Scraper,
            CurrentSeasonPatterns = "[\"2025\",\"2025-2026\"]"
            };
            context.DataProviders.Add(betExplorer);
        }

        var oddsportal = await context.DataProviders.FindAsync(Guid.Parse("a0000000-0000-0000-0000-000000000002"));
        if (oddsportal == null)
        {
            oddsportal = new DataProvider
            {
                Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"),
                Name = "Oddsportal",
                Code = "oddsportal",
                BaseUrl = "https://www.oddsportal.com",
                IsActive = false,
                Priority = 5,
                Type = ProviderType.Scraper,
                Notes = "Prepared for future use"
            };
            context.DataProviders.Add(oddsportal);
        }

        // Create Betting Providers (Czech betting companies)
        var betano = await context.DataProviders.FindAsync(Guid.Parse("b0000000-0000-0000-0000-000000000001"));
        if (betano == null)
        {
            betano = new DataProvider
            {
                Id = Guid.Parse("b0000000-0000-0000-0000-000000000001"),
                Name = "Betano",
                Code = "betano",
                BaseUrl = "https://www.betano.cz",
                IsActive = true,
                Priority = 10,
                Type = ProviderType.BettingProvider,
                Notes = "Czech betting provider - Kaizen Gaming"
            };
            context.DataProviders.Add(betano);
        }

        var chance = await context.DataProviders.FindAsync(Guid.Parse("b0000000-0000-0000-0000-000000000002"));
        if (chance == null)
        {
            chance = new DataProvider
            {
                Id = Guid.Parse("b0000000-0000-0000-0000-000000000002"),
                Name = "Chance",
                Code = "chance",
                BaseUrl = "https://www.chance.cz",
                IsActive = false,
                Priority = 8,
                Type = ProviderType.BettingProvider,
                Notes = "Czech betting provider - Aggressive Cloudflare protection"
            };
            context.DataProviders.Add(chance);
        }

        var fortuna = await context.DataProviders.FindAsync(Guid.Parse("b0000000-0000-0000-0000-000000000003"));
        if (fortuna == null)
        {
            fortuna = new DataProvider
            {
                Id = Guid.Parse("b0000000-0000-0000-0000-000000000003"),
                Name = "Fortuna",
                Code = "fortuna",
                BaseUrl = "https://www.ifortuna.cz",
                IsActive = false,
                Priority = 9,
                Type = ProviderType.BettingProvider,
                Notes = "Czech betting provider"
            };
            context.DataProviders.Add(fortuna);
        }

        var tipsport = await context.DataProviders.FindAsync(Guid.Parse("b0000000-0000-0000-0000-000000000004"));
        if (tipsport == null)
        {
            tipsport = new DataProvider
            {
                Id = Guid.Parse("b0000000-0000-0000-0000-000000000004"),
                Name = "Tipsport",
                Code = "tipsport",
                BaseUrl = "https://www.tipsport.cz",
                IsActive = false,
                Priority = 9,
                Type = ProviderType.BettingProvider,
                Notes = "Largest Czech bookmaker"
            };
            context.DataProviders.Add(tipsport);
        }

        var kingsbet = await context.DataProviders.FindAsync(Guid.Parse("b0000000-0000-0000-0000-000000000005"));
        if (kingsbet == null)
        {
            kingsbet = new DataProvider
            {
                Id = Guid.Parse("b0000000-0000-0000-0000-000000000005"),
                Name = "Kingsbet",
                Code = "kingsbet",
                BaseUrl = "https://www.kingsbet.cz",
                IsActive = false,
                Priority = 7,
                Type = ProviderType.BettingProvider,
                Notes = "Czech betting provider"
            };
            context.DataProviders.Add(kingsbet);
        }

        // Save betting providers if any were added
        await context.SaveChangesAsync();

        // Check if already seeded (sports, countries, leagues)
        if (await context.Sports.AnyAsync())
        {
            return;
        }

        // Create Football sport
        var football = new Sport
        {
            Name = "Football",
            Code = "football",
            IsActive = true
        };
        context.Sports.Add(football);

        // Create countries
        var england = new Country
        {
            Name = "England",
            Code = "england",
            FlagEmoji = "🏴󠁧󠁢󠁥󠁮󠁧󠁿"
        };

        var spain = new Country
        {
            Name = "Spain",
            Code = "spain",
            FlagEmoji = "🇪🇸"
        };

        var germany = new Country
        {
            Name = "Germany",
            Code = "germany",
            FlagEmoji = "🇩🇪"
        };

        var italy = new Country
        {
            Name = "Italy",
            Code = "italy",
            FlagEmoji = "🇮🇹"
        };

        var france = new Country
        {
            Name = "France",
            Code = "france",
            FlagEmoji = "🇫🇷"
        };

        context.Countries.AddRange(england, spain, germany, italy, france);

        // Save to get IDs
        await context.SaveChangesAsync();

        // Create leagues
        var premierLeague = new League
        {
            SportId = football.Id,
            CountryId = england.Id,
            Name = "Premier League",
            DisplayName = "Premier League (England)",
            BetExplorerSlug = "premier-league",
            IsSyncEnabled = false,
            IsBettable = true,
            Priority = 10
        };

        var laLiga = new League
        {
            SportId = football.Id,
            CountryId = spain.Id,
            Name = "La Liga",
            DisplayName = "La Liga (Spain)",
            BetExplorerSlug = "laliga",
            IsSyncEnabled = false,
            IsBettable = true,
            Priority = 9
        };

        var bundesliga = new League
        {
            SportId = football.Id,
            CountryId = germany.Id,
            Name = "Bundesliga",
            DisplayName = "Bundesliga (Germany)",
            BetExplorerSlug = "bundesliga",
            IsSyncEnabled = false,
            IsBettable = true,
            Priority = 8
        };

        var serieA = new League
        {
            SportId = football.Id,
            CountryId = italy.Id,
            Name = "Serie A",
            DisplayName = "Serie A (Italy)",
            BetExplorerSlug = "serie-a",
            IsSyncEnabled = false,
            IsBettable = true,
            Priority = 7
        };

        var ligue1 = new League
        {
            SportId = football.Id,
            CountryId = france.Id,
            Name = "Ligue 1",
            DisplayName = "Ligue 1 (France)",
            BetExplorerSlug = "ligue-1",
            IsSyncEnabled = false,
            IsBettable = true,
            Priority = 6
        };

        context.Leagues.AddRange(premierLeague, laLiga, bundesliga, serieA, ligue1);

        // Save to get IDs
        await context.SaveChangesAsync();

        // Create CountryProvider mappings for BetExplorer
        var countryProviders = new[]
        {
            new CountryProvider { CountryId = england.Id, ProviderId = betExplorer.Id, ProviderCode = "england", ProviderName = "England", IsActive = true },
            new CountryProvider { CountryId = spain.Id, ProviderId = betExplorer.Id, ProviderCode = "spain", ProviderName = "Spain", IsActive = true },
            new CountryProvider { CountryId = germany.Id, ProviderId = betExplorer.Id, ProviderCode = "germany", ProviderName = "Germany", IsActive = true },
            new CountryProvider { CountryId = italy.Id, ProviderId = betExplorer.Id, ProviderCode = "italy", ProviderName = "Italy", IsActive = true },
            new CountryProvider { CountryId = france.Id, ProviderId = betExplorer.Id, ProviderCode = "france", ProviderName = "France", IsActive = true }
        };
        context.CountryProviders.AddRange(countryProviders);

        // Create LeagueProvider mappings for BetExplorer
        var leagueProviders = new[]
        {
            new LeagueProvider { LeagueId = premierLeague.Id, ProviderId = betExplorer.Id, ProviderSlug = "premier-league", ProviderName = "Premier League", IsActive = false },
            new LeagueProvider { LeagueId = laLiga.Id, ProviderId = betExplorer.Id, ProviderSlug = "laliga", ProviderName = "La Liga", IsActive = false },
            new LeagueProvider { LeagueId = bundesliga.Id, ProviderId = betExplorer.Id, ProviderSlug = "bundesliga", ProviderName = "Bundesliga", IsActive = false },
            new LeagueProvider { LeagueId = serieA.Id, ProviderId = betExplorer.Id, ProviderSlug = "serie-a", ProviderName = "Serie A", IsActive = false },
            new LeagueProvider { LeagueId = ligue1.Id, ProviderId = betExplorer.Id, ProviderSlug = "ligue-1", ProviderName = "Ligue 1", IsActive = false }
        };
        context.LeagueProviders.AddRange(leagueProviders);

        await context.SaveChangesAsync();
    }
}
