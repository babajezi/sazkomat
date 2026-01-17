using System.Text.Json;
using Sazkomat.Configuration.Repositories;
using Sazkomat.DataImport.Entities;
using Sazkomat.DataImport.Repositories;

namespace Sazkomat.Api.Endpoints;

public static class UnmatchedLeagueEndpoints
{
    public static void MapUnmatchedLeagueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/unmatched-leagues")
            .WithTags("Unmatched Leagues")
            .WithOpenApi();

        // GET /api/unmatched-leagues
        group.MapGet("/", async (
            IUnmatchedLeagueRepository repo,
            Guid? providerId,
            bool? unresolvedOnly) =>
        {
            List<UnmatchedLeague> leagues;

            if (providerId.HasValue)
            {
                leagues = unresolvedOnly == true
                    ? await repo.GetUnresolvedByProviderAsync(providerId.Value)
                    : await repo.GetByProviderAsync(providerId.Value);
            }
            else
            {
                leagues = unresolvedOnly == true
                    ? await repo.GetUnresolvedAsync()
                    : await repo.GetAllAsync();
            }

            return Results.Ok(leagues.Select(l => new UnmatchedLeagueDto
            {
                Id = l.Id,
                ProviderId = l.ProviderId,
                ProviderName = l.Provider?.Name,
                ProviderLeagueId = l.ProviderLeagueId,
                ProviderLeagueName = l.ProviderLeagueName,
                ProviderSlug = l.ProviderSlug,
                CountryCode = l.CountryCode,
                CountryName = l.CountryName,
                ScrapedAt = l.ScrapedAt,
                IsResolved = l.IsResolved,
                ResolutionType = l.ResolutionType?.ToString(),
                ResolvedLeagueId = l.ResolvedLeagueId,
                ResolvedLeagueName = l.ResolvedLeague?.Name,
                ResolvedAt = l.ResolvedAt,
                ResolutionNotes = l.ResolutionNotes
            }));
        })
        .WithName("GetUnmatchedLeagues")
        .WithDescription("Get all unmatched leagues, optionally filtered by provider and resolution status");

        // GET /api/unmatched-leagues/{id}
        group.MapGet("/{id:guid}", async (Guid id, IUnmatchedLeagueRepository repo) =>
        {
            var league = await repo.GetByIdAsync(id);
            if (league == null)
                return Results.NotFound();

            return Results.Ok(new UnmatchedLeagueDto
            {
                Id = league.Id,
                ProviderId = league.ProviderId,
                ProviderName = league.Provider?.Name,
                ProviderLeagueId = league.ProviderLeagueId,
                ProviderLeagueName = league.ProviderLeagueName,
                ProviderSlug = league.ProviderSlug,
                CountryCode = league.CountryCode,
                CountryName = league.CountryName,
                ScrapedAt = league.ScrapedAt,
                IsResolved = league.IsResolved,
                ResolutionType = league.ResolutionType?.ToString(),
                ResolvedLeagueId = league.ResolvedLeagueId,
                ResolvedLeagueName = league.ResolvedLeague?.Name,
                ResolvedAt = league.ResolvedAt,
                ResolutionNotes = league.ResolutionNotes
            });
        })
        .WithName("GetUnmatchedLeague")
        .WithDescription("Get a specific unmatched league by ID");

        // POST /api/unmatched-leagues/{id}/resolve/map
        group.MapPost("/{id:guid}/resolve/map", async (
            Guid id,
            ResolveAsMappedRequest request,
            IUnmatchedLeagueRepository unmatchedRepo,
            ILeagueRepository leagueRepo,
            ILeagueProviderRepository leagueProviderRepo,
            IProviderLeagueRepository providerLeagueRepo,
            IDataProviderRepository providerRepo) =>
        {
            var unmatchedLeague = await unmatchedRepo.GetByIdAsync(id);
            if (unmatchedLeague == null)
                return Results.NotFound(new { error = "Unmatched league not found" });

            // Verify target league exists
            var targetLeague = await leagueRepo.GetByIdAsync(request.LeagueId);
            if (targetLeague == null)
                return Results.BadRequest(new { error = "Target league not found" });

            // Resolve as mapped
            var resolved = await unmatchedRepo.ResolveAsMappedAsync(id, request.LeagueId, request.Notes);

            var providerSlug = unmatchedLeague.ProviderSlug ?? unmatchedLeague.ProviderLeagueName.ToLowerInvariant().Replace(" ", "-");

            // Create LeagueProvider mapping if it doesn't exist
            var existingMapping = await leagueProviderRepo.GetByLeagueAndProviderAsync(
                request.LeagueId, unmatchedLeague.ProviderId);

            if (existingMapping == null)
            {
                var leagueProvider = new Configuration.Entities.LeagueProvider
                {
                    LeagueId = request.LeagueId,
                    ProviderId = unmatchedLeague.ProviderId,
                    ProviderSlug = providerSlug,
                    ProviderName = unmatchedLeague.ProviderLeagueName,
                    IsActive = true
                };
                await leagueProviderRepo.AddOrUpdateAsync(leagueProvider);
            }

            // Create/update ProviderLeague record with league_id
            var existingProviderLeague = await providerLeagueRepo.GetByProviderSlugAsync(
                unmatchedLeague.ProviderId, providerSlug);

            if (existingProviderLeague == null)
            {
                var providerLeague = new ProviderLeague
                {
                    ProviderId = unmatchedLeague.ProviderId,
                    ProviderName = unmatchedLeague.ProviderLeagueName,
                    ProviderSlug = providerSlug,
                    CountryCode = unmatchedLeague.CountryCode,
                    LeagueId = request.LeagueId,
                    IsImported = true,
                    ScrapedAt = DateTime.UtcNow
                };
                await providerLeagueRepo.CreateAsync(providerLeague);
            }
            else
            {
                existingProviderLeague.LeagueId = request.LeagueId;
                existingProviderLeague.IsImported = true;
                await providerLeagueRepo.UpdateAsync(existingProviderLeague);
            }

            // Optionally create LeagueNameMapping for future auto-matching
            if (request.CreateMapping == true)
            {
                // This would be handled by a separate service
            }

            return Results.Ok(new
            {
                success = true,
                message = $"League '{unmatchedLeague.ProviderLeagueName}' mapped to '{targetLeague.Name}'",
                unmatchedLeagueId = id,
                targetLeagueId = request.LeagueId
            });
        })
        .WithName("ResolveUnmatchedLeagueAsMap")
        .WithDescription("Resolve an unmatched league by mapping it to an existing league");

        // POST /api/unmatched-leagues/{id}/resolve/create-from-betexplorer
        // Creates a new league from BetExplorer data and maps the unmatched league to it
        group.MapPost("/{id:guid}/resolve/create-from-betexplorer", async (
            Guid id,
            CreateFromBetExplorerRequest request,
            IUnmatchedLeagueRepository unmatchedRepo,
            ILeagueRepository leagueRepo,
            ILeagueProviderRepository leagueProviderRepo,
            ICountryRepository countryRepo,
            ISportRepository sportRepo,
            IProviderLeagueRepository providerLeagueRepo,
            ILogger<Program> logger) =>
        {
            var unmatchedLeague = await unmatchedRepo.GetByIdAsync(id);
            if (unmatchedLeague == null)
                return Results.NotFound(new { error = "Unmatched league not found" });

            // Get country - prefer manually specified countryId, fall back to auto-detection
            Configuration.Entities.Country? country = null;

            if (request.CountryId.HasValue)
            {
                // Use manually specified country
                country = await countryRepo.GetByIdAsync(request.CountryId.Value);
                if (country == null)
                    return Results.BadRequest(new { error = $"Specified country ID '{request.CountryId}' not found" });

                logger.LogInformation("Using manually specified country '{CountryName}' (ID: {CountryId}) for league resolution",
                    country.Name, country.Id);
            }
            else
            {
                // Auto-detect country by code or name
                country = await countryRepo.GetByCodeAsync(unmatchedLeague.CountryCode);
                if (country == null)
                {
                    // Try by name
                    var allCountries = await countryRepo.GetAllAsync();
                    country = allCountries.FirstOrDefault(c =>
                        c.Name.Equals(unmatchedLeague.CountryName, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (country == null)
                return Results.BadRequest(new { error = $"Country '{unmatchedLeague.CountryCode}' not found. Please select country manually." });

            // Get football sport (default for BetExplorer)
            var sports = await sportRepo.GetAllAsync();
            var footballSport = sports.FirstOrDefault(s =>
                s.Code.Equals("football", StringComparison.OrdinalIgnoreCase));

            if (footballSport == null)
                return Results.BadRequest(new { error = "Football sport not found in database" });

            // Check if league with same slug already exists in this country
            var existingLeagues = await leagueRepo.GetByCountryIdAsync(country.Id);
            var existingLeague = existingLeagues.FirstOrDefault(l =>
                l.BetExplorerSlug?.Equals(request.BetExplorerSlug, StringComparison.OrdinalIgnoreCase) == true);

            if (existingLeague != null)
            {
                // League already exists, just map to it
                await unmatchedRepo.ResolveAsMappedAsync(id, existingLeague.Id, "Auto-mapped to existing league");

                // Also create provider_leagues record
                var existingProviderSlug = unmatchedLeague.ProviderSlug ?? unmatchedLeague.ProviderLeagueName.ToLowerInvariant().Replace(" ", "-");
                var existingProviderLeague = await providerLeagueRepo.GetByProviderSlugAsync(
                    unmatchedLeague.ProviderId, existingProviderSlug);

                if (existingProviderLeague == null)
                {
                    var providerLeague = new ProviderLeague
                    {
                        ProviderId = unmatchedLeague.ProviderId,
                        ProviderName = unmatchedLeague.ProviderLeagueName,
                        ProviderSlug = existingProviderSlug,
                        CountryCode = unmatchedLeague.CountryCode,
                        LeagueId = existingLeague.Id,
                        IsImported = true,
                        ScrapedAt = DateTime.UtcNow
                    };
                    await providerLeagueRepo.CreateAsync(providerLeague);
                }
                else
                {
                    existingProviderLeague.LeagueId = existingLeague.Id;
                    existingProviderLeague.IsImported = true;
                    await providerLeagueRepo.UpdateAsync(existingProviderLeague);
                }

                // Ensure BetExplorer LeagueProvider binding exists
                var betExplorerProviderId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
                var existingBetExplorerBinding = await leagueProviderRepo.GetByLeagueAndProviderAsync(
                    existingLeague.Id, betExplorerProviderId);

                if (existingBetExplorerBinding == null)
                {
                    var betExplorerProviderSlug = $"{country.Code}/{existingLeague.BetExplorerSlug}";
                    var missingBetExplorerMapping = new Configuration.Entities.LeagueProvider
                    {
                        LeagueId = existingLeague.Id,
                        ProviderId = betExplorerProviderId,
                        ProviderSlug = betExplorerProviderSlug,
                        ProviderName = existingLeague.Name,
                        IsActive = true
                    };
                    await leagueProviderRepo.AddOrUpdateAsync(missingBetExplorerMapping);
                    logger.LogInformation("Created missing BetExplorer binding for existing league '{LeagueName}' (slug: {Slug})",
                        existingLeague.Name, betExplorerProviderSlug);
                }

                return Results.Ok(new
                {
                    success = true,
                    message = $"League already exists. Mapped '{unmatchedLeague.ProviderLeagueName}' to existing '{existingLeague.Name}'",
                    unmatchedLeagueId = id,
                    leagueId = existingLeague.Id,
                    created = false
                });
            }

            // Create new league
            var leagueName = request.LeagueName ?? request.BetExplorerSlug.Replace("-", " ");
            leagueName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(leagueName.ToLower());

            var newLeague = new Configuration.Entities.League
            {
                Id = Guid.NewGuid(),
                SportId = footballSport.Id,
                CountryId = country.Id,
                Name = leagueName,
                DisplayName = $"{leagueName} ({country.Name})",
                BetExplorerSlug = request.BetExplorerSlug,
                IsActive = true,
                IsBettable = true,
                Priority = 5,
                Notes = $"Created from BetExplorer via unmatched league resolution"
            };

            await leagueRepo.CreateAsync(newLeague);
            logger.LogInformation("Created new league '{LeagueName}' (ID: {LeagueId}) for country {Country}",
                newLeague.Name, newLeague.Id, country.Name);

            // Create LeagueProvider mapping for BetExplorer (use country/slug format for uniqueness)
            var betExplorerId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
            var betExplorerSlug = $"{country.Code}/{request.BetExplorerSlug}";
            var betExplorerMapping = new Configuration.Entities.LeagueProvider
            {
                LeagueId = newLeague.Id,
                ProviderId = betExplorerId,
                ProviderSlug = betExplorerSlug,
                ProviderName = leagueName,
                IsActive = true
            };
            await leagueProviderRepo.AddOrUpdateAsync(betExplorerMapping);

            // Create LeagueProvider mapping for the original provider (if different from BetExplorer)
            var newProviderSlug = unmatchedLeague.ProviderSlug ?? unmatchedLeague.ProviderLeagueName.ToLowerInvariant().Replace(" ", "-");
            if (unmatchedLeague.ProviderId != betExplorerId)
            {
                var providerMapping = new Configuration.Entities.LeagueProvider
                {
                    LeagueId = newLeague.Id,
                    ProviderId = unmatchedLeague.ProviderId,
                    ProviderSlug = newProviderSlug,
                    ProviderName = unmatchedLeague.ProviderLeagueName,
                    IsActive = true
                };
                await leagueProviderRepo.AddOrUpdateAsync(providerMapping);
            }

            // Create provider_leagues record for the original provider (betting provider)
            if (unmatchedLeague.ProviderId != betExplorerId)
            {
                var existingProviderLeague = await providerLeagueRepo.GetByProviderSlugAsync(
                    unmatchedLeague.ProviderId, newProviderSlug);

                if (existingProviderLeague == null)
                {
                    var providerLeague = new ProviderLeague
                    {
                        ProviderId = unmatchedLeague.ProviderId,
                        ProviderName = unmatchedLeague.ProviderLeagueName,
                        ProviderSlug = newProviderSlug,
                        CountryCode = unmatchedLeague.CountryCode,
                        LeagueId = newLeague.Id,
                        IsImported = true,
                        ScrapedAt = DateTime.UtcNow
                    };
                    await providerLeagueRepo.CreateAsync(providerLeague);
                }
                else
                {
                    existingProviderLeague.LeagueId = newLeague.Id;
                    existingProviderLeague.IsImported = true;
                    await providerLeagueRepo.UpdateAsync(existingProviderLeague);
                }
            }

            // Resolve unmatched league
            await unmatchedRepo.ResolveAsMappedAsync(id, newLeague.Id, request.Notes);

            return Results.Ok(new
            {
                success = true,
                message = $"Created league '{newLeague.Name}' and mapped '{unmatchedLeague.ProviderLeagueName}' to it",
                unmatchedLeagueId = id,
                leagueId = newLeague.Id,
                created = true
            });
        })
        .WithName("ResolveUnmatchedLeagueCreateFromBetExplorer")
        .WithDescription("Create a new league from BetExplorer data and map the unmatched league to it");

        // POST /api/unmatched-leagues/{id}/resolve/ignore
        group.MapPost("/{id:guid}/resolve/ignore", async (
            Guid id,
            ResolveAsIgnoredRequest request,
            IUnmatchedLeagueRepository repo) =>
        {
            var unmatchedLeague = await repo.GetByIdAsync(id);
            if (unmatchedLeague == null)
                return Results.NotFound(new { error = "Unmatched league not found" });

            var resolved = await repo.ResolveAsIgnoredAsync(id, request.Notes);

            return Results.Ok(new
            {
                success = true,
                message = $"League '{unmatchedLeague.ProviderLeagueName}' marked as ignored",
                unmatchedLeagueId = id
            });
        })
        .WithName("ResolveUnmatchedLeagueAsIgnore")
        .WithDescription("Resolve an unmatched league by ignoring it (user decided not to import)");

        // POST /api/unmatched-leagues/{id}/resolve/unavailable
        group.MapPost("/{id:guid}/resolve/unavailable", async (
            Guid id,
            ResolveAsUnavailableRequest request,
            IUnmatchedLeagueRepository repo) =>
        {
            var unmatchedLeague = await repo.GetByIdAsync(id);
            if (unmatchedLeague == null)
                return Results.NotFound(new { error = "Unmatched league not found" });

            var resolved = await repo.ResolveAsUnavailableAsync(id, request.Notes);

            return Results.Ok(new
            {
                success = true,
                message = $"League '{unmatchedLeague.ProviderLeagueName}' marked as unavailable in BetExplorer",
                unmatchedLeagueId = id
            });
        })
        .WithName("ResolveUnmatchedLeagueAsUnavailable")
        .WithDescription("Resolve an unmatched league as unavailable (BetExplorer does not support this league)");

        // POST /api/unmatched-leagues/{id}/unresolve
        group.MapPost("/{id:guid}/unresolve", async (
            Guid id,
            IUnmatchedLeagueRepository repo) =>
        {
            var unmatchedLeague = await repo.GetByIdAsync(id);
            if (unmatchedLeague == null)
                return Results.NotFound(new { error = "Unmatched league not found" });

            unmatchedLeague.IsResolved = false;
            unmatchedLeague.ResolutionType = null;
            unmatchedLeague.ResolvedLeagueId = null;
            unmatchedLeague.ResolvedAt = null;
            unmatchedLeague.ResolutionNotes = null;

            await repo.UpdateAsync(unmatchedLeague);

            return Results.Ok(new
            {
                success = true,
                message = $"Resolution cleared for '{unmatchedLeague.ProviderLeagueName}'",
                unmatchedLeagueId = id
            });
        })
        .WithName("UnresolveUnmatchedLeague")
        .WithDescription("Clear the resolution status of an unmatched league");

        // DELETE /api/unmatched-leagues/{id}
        group.MapDelete("/{id:guid}", async (Guid id, IUnmatchedLeagueRepository repo) =>
        {
            var unmatchedLeague = await repo.GetByIdAsync(id);
            if (unmatchedLeague == null)
                return Results.NotFound(new { error = "Unmatched league not found" });

            await repo.DeleteAsync(id);

            return Results.Ok(new
            {
                success = true,
                message = $"Deleted unmatched league '{unmatchedLeague.ProviderLeagueName}'"
            });
        })
        .WithName("DeleteUnmatchedLeague")
        .WithDescription("Delete an unmatched league record");

        // GET /api/unmatched-leagues/stats
        group.MapGet("/stats", async (IUnmatchedLeagueRepository repo) =>
        {
            var all = await repo.GetAllAsync();
            var unresolved = all.Where(l => !l.IsResolved).ToList();
            var mapped = all.Where(l => l.ResolutionType == ResolutionType.Mapped).ToList();
            var ignored = all.Where(l => l.ResolutionType == ResolutionType.Ignored).ToList();
            var unavailable = all.Where(l => l.ResolutionType == ResolutionType.Unavailable).ToList();

            var byProvider = all.GroupBy(l => l.Provider?.Name ?? "Unknown")
                .Select(g => new { Provider = g.Key, Total = g.Count(), Unresolved = g.Count(l => !l.IsResolved) })
                .ToList();

            var byCountry = unresolved.GroupBy(l => l.CountryName ?? l.CountryCode)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToList();

            return Results.Ok(new
            {
                total = all.Count,
                unresolved = unresolved.Count,
                mapped = mapped.Count,
                ignored = ignored.Count,
                unavailable = unavailable.Count,
                byProvider,
                topUnresolvedCountries = byCountry
            });
        })
        .WithName("GetUnmatchedLeaguesStats")
        .WithDescription("Get statistics about unmatched leagues");

        // POST /api/unmatched-leagues/copy-resolutions/preview
        group.MapPost("/copy-resolutions/preview", async (
            CopyResolutionsRequest request,
            IUnmatchedLeagueRepository repo,
            ILeagueRepository leagueRepo,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("Preview copy resolutions from provider {Source} to {Target}",
                request.SourceProviderId, request.TargetProviderId);

            // Get all leagues from both providers
            var sourceLeagues = await repo.GetByProviderAsync(request.SourceProviderId);
            var targetLeagues = await repo.GetByProviderAsync(request.TargetProviderId);

            // Build lookup for source leagues by (name, countryCode)
            var sourceLookup = sourceLeagues
                .Where(l => l.IsResolved && l.ResolutionType.HasValue)
                .ToDictionary(
                    l => (l.ProviderLeagueName.ToLowerInvariant(), l.CountryCode.ToLowerInvariant()),
                    l => l,
                    new LeagueKeyComparer());

            var matches = new List<CopyResolutionMatch>();
            int skipped = 0;
            int notFound = 0;

            foreach (var target in targetLeagues)
            {
                // Skip if already resolved
                if (target.IsResolved)
                {
                    skipped++;
                    continue;
                }

                var key = (target.ProviderLeagueName.ToLowerInvariant(), target.CountryCode.ToLowerInvariant());
                if (sourceLookup.TryGetValue(key, out var source))
                {
                    // Get resolved league name if applicable
                    string? resolvedLeagueName = null;
                    if (source.ResolvedLeagueId.HasValue)
                    {
                        var resolvedLeague = await leagueRepo.GetByIdAsync(source.ResolvedLeagueId.Value);
                        resolvedLeagueName = resolvedLeague?.Name;
                    }

                    matches.Add(new CopyResolutionMatch
                    {
                        TargetId = target.Id,
                        TargetLeagueName = target.ProviderLeagueName,
                        TargetCountryCode = target.CountryCode,
                        SourceId = source.Id,
                        SourceResolutionType = source.ResolutionType?.ToString() ?? "Unknown",
                        SourceResolvedLeagueName = resolvedLeagueName,
                        SourceResolvedLeagueId = source.ResolvedLeagueId
                    });
                }
                else
                {
                    notFound++;
                }
            }

            logger.LogInformation("Copy resolutions preview: {Matches} matches, {Skipped} skipped, {NotFound} not found",
                matches.Count, skipped, notFound);

            return Results.Ok(new CopyResolutionsPreviewResponse
            {
                Matches = matches,
                Skipped = skipped,
                NotFound = notFound
            });
        })
        .WithName("PreviewCopyResolutions")
        .WithDescription("Preview which resolutions would be copied from source to target provider");

        // POST /api/unmatched-leagues/copy-resolutions/execute
        group.MapPost("/copy-resolutions/execute", async (
            CopyResolutionsRequest request,
            IUnmatchedLeagueRepository repo,
            ILeagueProviderRepository leagueProviderRepo,
            IProviderLeagueRepository providerLeagueRepo,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("Execute copy resolutions from provider {Source} to {Target}",
                request.SourceProviderId, request.TargetProviderId);

            // Get all leagues from both providers
            var sourceLeagues = await repo.GetByProviderAsync(request.SourceProviderId);
            var targetLeagues = await repo.GetByProviderAsync(request.TargetProviderId);

            // Build lookup for source leagues by (name, countryCode)
            var sourceLookup = sourceLeagues
                .Where(l => l.IsResolved && l.ResolutionType.HasValue)
                .ToDictionary(
                    l => (l.ProviderLeagueName.ToLowerInvariant(), l.CountryCode.ToLowerInvariant()),
                    l => l,
                    new LeagueKeyComparer());

            int copied = 0;
            int skipped = 0;
            int notFound = 0;

            foreach (var target in targetLeagues)
            {
                // Skip if already resolved
                if (target.IsResolved)
                {
                    skipped++;
                    continue;
                }

                var key = (target.ProviderLeagueName.ToLowerInvariant(), target.CountryCode.ToLowerInvariant());
                if (sourceLookup.TryGetValue(key, out var source))
                {
                    // Copy resolution fields
                    target.IsResolved = source.IsResolved;
                    target.ResolutionType = source.ResolutionType;
                    target.ResolvedLeagueId = source.ResolvedLeagueId;
                    target.ResolvedAt = DateTime.UtcNow;
                    target.ResolutionNotes = $"Zkopírováno z providera {request.SourceProviderId}";

                    await repo.UpdateAsync(target);

                    // If mapped to a league, create LeagueProvider mapping for target provider
                    if (source.ResolutionType == ResolutionType.Mapped && source.ResolvedLeagueId.HasValue)
                    {
                        // Check if mapping already exists
                        var existingMapping = await leagueProviderRepo.GetByLeagueAndProviderAsync(
                            source.ResolvedLeagueId.Value, request.TargetProviderId);

                        if (existingMapping == null)
                        {
                            var providerSlug = target.ProviderSlug ?? target.ProviderLeagueName.ToLowerInvariant().Replace(" ", "-");

                            var leagueProvider = new Configuration.Entities.LeagueProvider
                            {
                                LeagueId = source.ResolvedLeagueId.Value,
                                ProviderId = request.TargetProviderId,
                                ProviderSlug = providerSlug,
                                ProviderName = target.ProviderLeagueName,
                                IsActive = true
                            };
                            await leagueProviderRepo.AddOrUpdateAsync(leagueProvider);

                            logger.LogDebug("Created/Updated LeagueProvider mapping for league {LeagueId} -> provider {ProviderId}",
                                source.ResolvedLeagueId.Value, request.TargetProviderId);
                        }

                        // Also create/update ProviderLeague record
                        var existingProviderSlug = target.ProviderSlug ?? target.ProviderLeagueName.ToLowerInvariant().Replace(" ", "-");
                        var existingProviderLeague = await providerLeagueRepo.GetByProviderSlugAsync(
                            request.TargetProviderId, existingProviderSlug);

                        if (existingProviderLeague == null)
                        {
                            var providerLeague = new ProviderLeague
                            {
                                ProviderId = request.TargetProviderId,
                                ProviderName = target.ProviderLeagueName,
                                ProviderSlug = existingProviderSlug,
                                CountryCode = target.CountryCode,
                                LeagueId = source.ResolvedLeagueId.Value,
                                IsImported = true,
                                ScrapedAt = DateTime.UtcNow
                            };
                            await providerLeagueRepo.CreateAsync(providerLeague);
                        }
                        else
                        {
                            existingProviderLeague.LeagueId = source.ResolvedLeagueId.Value;
                            existingProviderLeague.IsImported = true;
                            await providerLeagueRepo.UpdateAsync(existingProviderLeague);
                        }
                    }

                    copied++;
                    logger.LogDebug("Copied resolution for league '{LeagueName}' [{CountryCode}]",
                        target.ProviderLeagueName, target.CountryCode);
                }
                else
                {
                    notFound++;
                }
            }

            logger.LogInformation("Copy resolutions complete: {Copied} copied, {Skipped} skipped, {NotFound} not found",
                copied, skipped, notFound);

            return Results.Ok(new CopyResolutionsExecuteResponse
            {
                Copied = copied,
                Skipped = skipped,
                NotFound = notFound
            });
        })
        .WithName("ExecuteCopyResolutions")
        .WithDescription("Execute copying resolutions from source to target provider");

        // GET /api/unmatched-leagues/suggestions/{id}
        group.MapGet("/suggestions/{id:guid}", async (
            Guid id,
            IUnmatchedLeagueRepository unmatchedRepo,
            ILeagueRepository leagueRepo,
            ICountryRepository countryRepo) =>
        {
            var unmatchedLeague = await unmatchedRepo.GetByIdAsync(id);
            if (unmatchedLeague == null)
                return Results.NotFound(new { error = "Unmatched league not found" });

            // Find country by code
            var country = await countryRepo.GetByCodeAsync(unmatchedLeague.CountryCode);

            List<Configuration.Entities.League> suggestions;
            if (country != null)
            {
                // Get leagues from same country
                suggestions = (await leagueRepo.GetByCountryIdAsync(country.Id)).ToList();
            }
            else
            {
                // Fallback to all leagues
                suggestions = (await leagueRepo.GetAllAsync()).Take(50).ToList();
            }

            return Results.Ok(new
            {
                unmatchedLeague = new
                {
                    unmatchedLeague.Id,
                    unmatchedLeague.ProviderLeagueName,
                    unmatchedLeague.CountryCode,
                    unmatchedLeague.CountryName
                },
                suggestions = suggestions.Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.DisplayName,
                    l.BetExplorerSlug,
                    CountryName = l.Country?.Name
                })
            });
        })
        .WithName("GetUnmatchedLeagueSuggestions")
        .WithDescription("Get suggested leagues for mapping an unmatched league");
    }
}

// DTOs
public record UnmatchedLeagueDto
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public string? ProviderName { get; init; }
    public string? ProviderLeagueId { get; init; }
    public string ProviderLeagueName { get; init; } = string.Empty;
    public string? ProviderSlug { get; init; }
    public string CountryCode { get; init; } = string.Empty;
    public string? CountryName { get; init; }
    public DateTime ScrapedAt { get; init; }
    public bool IsResolved { get; init; }
    public string? ResolutionType { get; init; }
    public Guid? ResolvedLeagueId { get; init; }
    public string? ResolvedLeagueName { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
}

public record ResolveAsMappedRequest(Guid LeagueId, string? Notes = null, bool? CreateMapping = null);
public record ResolveAsIgnoredRequest(string? Notes = null);
public record ResolveAsUnavailableRequest(string? Notes = null);
public record CreateFromBetExplorerRequest(string BetExplorerSlug, string? LeagueName = null, Guid? CountryId = null, string? Notes = null);

// Copy resolutions DTOs
public record CopyResolutionsRequest(Guid SourceProviderId, Guid TargetProviderId);

public record CopyResolutionMatch
{
    public Guid TargetId { get; init; }
    public string TargetLeagueName { get; init; } = string.Empty;
    public string TargetCountryCode { get; init; } = string.Empty;
    public Guid SourceId { get; init; }
    public string SourceResolutionType { get; init; } = string.Empty;
    public string? SourceResolvedLeagueName { get; init; }
    public Guid? SourceResolvedLeagueId { get; init; }
}

public record CopyResolutionsPreviewResponse
{
    public List<CopyResolutionMatch> Matches { get; init; } = new();
    public int Skipped { get; init; }  // already resolved in target
    public int NotFound { get; init; } // no match in source
}

public record CopyResolutionsExecuteResponse
{
    public int Copied { get; init; }
    public int Skipped { get; init; }
    public int NotFound { get; init; }
}

// Helper comparer for league key tuples
internal class LeagueKeyComparer : IEqualityComparer<(string Name, string CountryCode)>
{
    public bool Equals((string Name, string CountryCode) x, (string Name, string CountryCode) y)
    {
        return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(x.CountryCode, y.CountryCode, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode((string Name, string CountryCode) obj)
    {
        return HashCode.Combine(
            obj.Name?.ToLowerInvariant() ?? "",
            obj.CountryCode?.ToLowerInvariant() ?? "");
    }
}
