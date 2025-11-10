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

        // Check if already seeded (sports only - countries and leagues are created through scan/import workflow)
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

        await context.SaveChangesAsync();

        // NOTE: Countries, Leagues, CountryProviders, and LeagueProviders are NOT seeded.
        // They are created automatically through:
        // 1. Scan Countries - loads countries from providers into cache
        // 2. Import Countries - imports countries from cache (IsActive = false initially)
        // 3. Scan Leagues - loads leagues from providers, auto-activates countries with leagues
        // 4. Import Leagues - imports leagues from cache
    }
}
