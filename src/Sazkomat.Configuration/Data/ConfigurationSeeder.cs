using Microsoft.EntityFrameworkCore;
using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.Data;

public static class ConfigurationSeeder
{
    public static async Task SeedAsync(ConfigurationDbContext context)
    {
        // ScanCapabilities constants for different provider types
        // BetExplorer = reference provider, seasons come from here
        const string BetExplorerCapabilities = "{\"canScanCountries\":true,\"canScanLeagues\":false,\"canScanSeasons\":true}";
        // Betting providers - only countries + leagues mapping, seasons from BetExplorer
        const string BettingProviderCapabilities = "{\"canScanCountries\":true,\"canScanLeagues\":true,\"canScanSeasons\":false}";
        // Tipsport - derives countries from league names, so only leagues
        const string TipsportCapabilities = "{\"canScanCountries\":false,\"canScanLeagues\":true,\"canScanSeasons\":false}";
        const string DefaultCapabilities = "{\"canScanCountries\":true,\"canScanLeagues\":true,\"canScanSeasons\":true}";

        // Always check and update Data Providers (even if already seeded)
        var betExplorer = await context.DataProviders.FindAsync(Guid.Parse("a0000000-0000-0000-0000-000000000001"));
        if (betExplorer != null)
        {
            var needsUpdate = false;
            if (string.IsNullOrEmpty(betExplorer.CurrentSeasonPatterns) || betExplorer.CurrentSeasonPatterns == "[]")
            {
                betExplorer.CurrentSeasonPatterns = "[\"2025\",\"2025-2026\"]";
                needsUpdate = true;
            }
            if (betExplorer.ScanCapabilities != BetExplorerCapabilities)
            {
                betExplorer.ScanCapabilities = BetExplorerCapabilities;
                needsUpdate = true;
            }
            if (needsUpdate)
            {
                context.DataProviders.Update(betExplorer);
                await context.SaveChangesAsync();
            }
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
                CurrentSeasonPatterns = "[\"2025\",\"2025-2026\"]",
                ScanCapabilities = BetExplorerCapabilities
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
                Notes = "Prepared for future use",
                ScanCapabilities = DefaultCapabilities
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
                Notes = "Czech betting provider - Kaizen Gaming",
                ScanCapabilities = BettingProviderCapabilities
            };
            context.DataProviders.Add(betano);
        }
        else if (betano.ScanCapabilities != BettingProviderCapabilities)
        {
            betano.ScanCapabilities = BettingProviderCapabilities;
            context.DataProviders.Update(betano);
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
                Notes = "Czech betting provider - Aggressive Cloudflare protection",
                ScanCapabilities = BettingProviderCapabilities
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
                IsActive = true,
                Priority = 9,
                Type = ProviderType.BettingProvider,
                Notes = "Czech betting provider - uses Playwright for JavaScript rendering",
                ScanCapabilities = BettingProviderCapabilities
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
                IsActive = true,  // Enabled - uses FlareSolverr to bypass Cloudflare
                Priority = 9,
                Type = ProviderType.BettingProvider,
                Notes = "Largest Czech bookmaker - uses FlareSolverr for Cloudflare bypass",
                ScanCapabilities = TipsportCapabilities
            };
            context.DataProviders.Add(tipsport);
        }
        else
        {
            var needsUpdate = false;
            const string correctTipsportNotes = "Largest Czech bookmaker - uses FlareSolverr for Cloudflare bypass";

            // Activate Tipsport - now uses FlareSolverr to bypass Cloudflare
            if (!tipsport.IsActive)
            {
                tipsport.IsActive = true;
                needsUpdate = true;
            }
            // Fix outdated notes
            if (tipsport.Notes != correctTipsportNotes)
            {
                tipsport.Notes = correctTipsportNotes;
                needsUpdate = true;
            }
            if (tipsport.ScanCapabilities != TipsportCapabilities)
            {
                tipsport.ScanCapabilities = TipsportCapabilities;
                needsUpdate = true;
            }
            if (needsUpdate)
            {
                context.DataProviders.Update(tipsport);
            }
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
                Notes = "Czech betting provider",
                ScanCapabilities = BettingProviderCapabilities
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
