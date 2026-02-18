using Sazkomat.Configuration.Repositories;
using Sazkomat.Data.Entities;
using Sazkomat.Data.Repositories;

namespace Sazkomat.Api.Endpoints;

public static class UnmatchedCountryEndpoints
{
    public static void MapUnmatchedCountryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/unmatched-countries")
            .WithTags("Unmatched Countries")
            .WithOpenApi();

        // GET /api/unmatched-countries
        group.MapGet("/", async (
            IUnmatchedCountryRepository repo,
            IDataProviderRepository providerRepo,
            ICountryRepository countryRepo,
            Guid? providerId,
            bool? unresolvedOnly) =>
        {
            List<UnmatchedCountry> countries;

            if (providerId.HasValue)
            {
                countries = unresolvedOnly == true
                    ? await repo.GetUnresolvedByProviderAsync(providerId.Value)
                    : await repo.GetByProviderAsync(providerId.Value);
            }
            else
            {
                countries = unresolvedOnly == true
                    ? await repo.GetUnresolvedAsync()
                    : await repo.GetAllAsync();
            }

            // Load provider names
            var providerIds = countries.Select(c => c.ProviderId).Distinct().ToList();
            var providers = new Dictionary<Guid, string>();
            foreach (var pid in providerIds)
            {
                var provider = await providerRepo.GetByIdAsync(pid);
                if (provider != null)
                    providers[pid] = provider.Name;
            }

            // Load resolved country names
            var resolvedCountryIds = countries
                .Where(c => c.ResolvedCountryId.HasValue)
                .Select(c => c.ResolvedCountryId!.Value)
                .Distinct()
                .ToList();
            var resolvedCountries = new Dictionary<Guid, (string Name, string Code)>();
            foreach (var cid in resolvedCountryIds)
            {
                var country = await countryRepo.GetByIdAsync(cid);
                if (country != null)
                    resolvedCountries[cid] = (country.Name, country.Code);
            }

            return Results.Ok(countries.Select(c => new UnmatchedCountryDto
            {
                Id = c.Id,
                ProviderId = c.ProviderId,
                ProviderName = providers.GetValueOrDefault(c.ProviderId),
                ProviderCountryId = c.ProviderCountryId,
                ProviderCountryName = c.ProviderCountryName,
                ProviderSlug = c.ProviderSlug,
                ScrapedAt = c.ScrapedAt,
                IsResolved = c.IsResolved,
                ResolutionType = c.ResolutionType?.ToString(),
                ResolvedCountryId = c.ResolvedCountryId,
                ResolvedCountryName = c.ResolvedCountryId.HasValue && resolvedCountries.ContainsKey(c.ResolvedCountryId.Value)
                    ? resolvedCountries[c.ResolvedCountryId.Value].Name : null,
                ResolvedCountryCode = c.ResolvedCountryId.HasValue && resolvedCountries.ContainsKey(c.ResolvedCountryId.Value)
                    ? resolvedCountries[c.ResolvedCountryId.Value].Code : null,
                ResolvedAt = c.ResolvedAt,
                ResolutionNotes = c.ResolutionNotes
            }));
        })
        .WithName("GetUnmatchedCountries")
        .WithDescription("Get all unmatched countries, optionally filtered by provider and resolution status");

        // GET /api/unmatched-countries/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            IUnmatchedCountryRepository repo,
            IDataProviderRepository providerRepo,
            ICountryRepository countryRepo) =>
        {
            var country = await repo.GetByIdAsync(id);
            if (country == null)
                return Results.NotFound();

            var provider = await providerRepo.GetByIdAsync(country.ProviderId);
            Configuration.Entities.Country? resolvedCountry = null;
            if (country.ResolvedCountryId.HasValue)
                resolvedCountry = await countryRepo.GetByIdAsync(country.ResolvedCountryId.Value);

            return Results.Ok(new UnmatchedCountryDto
            {
                Id = country.Id,
                ProviderId = country.ProviderId,
                ProviderName = provider?.Name,
                ProviderCountryId = country.ProviderCountryId,
                ProviderCountryName = country.ProviderCountryName,
                ProviderSlug = country.ProviderSlug,
                ScrapedAt = country.ScrapedAt,
                IsResolved = country.IsResolved,
                ResolutionType = country.ResolutionType?.ToString(),
                ResolvedCountryId = country.ResolvedCountryId,
                ResolvedCountryName = resolvedCountry?.Name,
                ResolvedCountryCode = resolvedCountry?.Code,
                ResolvedAt = country.ResolvedAt,
                ResolutionNotes = country.ResolutionNotes
            });
        })
        .WithName("GetUnmatchedCountry")
        .WithDescription("Get a specific unmatched country by ID");

        // POST /api/unmatched-countries/{id}/resolve/map
        group.MapPost("/{id:guid}/resolve/map", async (
            Guid id,
            ResolveCountryAsMappedRequest request,
            IUnmatchedCountryRepository unmatchedRepo,
            ICountryRepository countryRepo,
            ICountryProviderRepository countryProviderRepo,
            ILogger<Program> logger) =>
        {
            var unmatchedCountry = await unmatchedRepo.GetByIdAsync(id);
            if (unmatchedCountry == null)
                return Results.NotFound(new { error = "Unmatched country not found" });

            // Verify target country exists
            var targetCountry = await countryRepo.GetByIdAsync(request.CountryId);
            if (targetCountry == null)
                return Results.BadRequest(new { error = "Target country not found" });

            // Resolve as mapped
            await unmatchedRepo.ResolveAsMappedAsync(id, request.CountryId, request.Notes);

            // Create CountryProvider mapping if it doesn't exist
            var existingMapping = await countryProviderRepo.GetByCountryAndProviderAsync(
                request.CountryId, unmatchedCountry.ProviderId);

            if (existingMapping == null)
            {
                var countryProvider = new Configuration.Entities.CountryProvider
                {
                    CountryId = request.CountryId,
                    ProviderId = unmatchedCountry.ProviderId,
                    ProviderCode = unmatchedCountry.ProviderSlug ?? unmatchedCountry.ProviderCountryName.ToLowerInvariant().Replace(" ", "-"),
                    ProviderName = unmatchedCountry.ProviderCountryName,
                    IsActive = true
                };
                await countryProviderRepo.AddAsync(countryProvider);
                logger.LogInformation("Created CountryProvider mapping: {ProviderCountryName} -> {CountryName}",
                    unmatchedCountry.ProviderCountryName, targetCountry.Name);
            }

            return Results.Ok(new
            {
                success = true,
                message = $"Country '{unmatchedCountry.ProviderCountryName}' mapped to '{targetCountry.Name}'",
                unmatchedCountryId = id,
                targetCountryId = request.CountryId
            });
        })
        .WithName("ResolveUnmatchedCountryAsMap")
        .WithDescription("Resolve an unmatched country by mapping it to an existing BetExplorer country");

        // POST /api/unmatched-countries/{id}/resolve/ignore
        group.MapPost("/{id:guid}/resolve/ignore", async (
            Guid id,
            ResolveCountryAsIgnoredRequest request,
            IUnmatchedCountryRepository repo) =>
        {
            var unmatchedCountry = await repo.GetByIdAsync(id);
            if (unmatchedCountry == null)
                return Results.NotFound(new { error = "Unmatched country not found" });

            await repo.ResolveAsIgnoredAsync(id, request.Notes);

            return Results.Ok(new
            {
                success = true,
                message = $"Country '{unmatchedCountry.ProviderCountryName}' marked as ignored",
                unmatchedCountryId = id
            });
        })
        .WithName("ResolveUnmatchedCountryAsIgnore")
        .WithDescription("Resolve an unmatched country by ignoring it");

        // POST /api/unmatched-countries/{id}/resolve/unavailable
        group.MapPost("/{id:guid}/resolve/unavailable", async (
            Guid id,
            ResolveCountryAsUnavailableRequest request,
            IUnmatchedCountryRepository repo) =>
        {
            var unmatchedCountry = await repo.GetByIdAsync(id);
            if (unmatchedCountry == null)
                return Results.NotFound(new { error = "Unmatched country not found" });

            await repo.ResolveAsUnavailableAsync(id, request.Notes);

            return Results.Ok(new
            {
                success = true,
                message = $"Country '{unmatchedCountry.ProviderCountryName}' marked as unavailable in BetExplorer",
                unmatchedCountryId = id
            });
        })
        .WithName("ResolveUnmatchedCountryAsUnavailable")
        .WithDescription("Resolve an unmatched country as unavailable in BetExplorer");

        // POST /api/unmatched-countries/{id}/unresolve
        group.MapPost("/{id:guid}/unresolve", async (
            Guid id,
            IUnmatchedCountryRepository repo) =>
        {
            var unmatchedCountry = await repo.GetByIdAsync(id);
            if (unmatchedCountry == null)
                return Results.NotFound(new { error = "Unmatched country not found" });

            await repo.UnresolveAsync(id);

            return Results.Ok(new
            {
                success = true,
                message = $"Resolution cleared for '{unmatchedCountry.ProviderCountryName}'",
                unmatchedCountryId = id
            });
        })
        .WithName("UnresolveUnmatchedCountry")
        .WithDescription("Clear the resolution status of an unmatched country");

        // DELETE /api/unmatched-countries/{id}
        group.MapDelete("/{id:guid}", async (Guid id, IUnmatchedCountryRepository repo) =>
        {
            var unmatchedCountry = await repo.GetByIdAsync(id);
            if (unmatchedCountry == null)
                return Results.NotFound(new { error = "Unmatched country not found" });

            await repo.DeleteAsync(id);

            return Results.Ok(new
            {
                success = true,
                message = $"Deleted unmatched country '{unmatchedCountry.ProviderCountryName}'"
            });
        })
        .WithName("DeleteUnmatchedCountry")
        .WithDescription("Delete an unmatched country record");

        // GET /api/unmatched-countries/stats
        group.MapGet("/stats", async (
            IUnmatchedCountryRepository repo,
            Guid? providerId) =>
        {
            var stats = await repo.GetStatsAsync(providerId);

            return Results.Ok(new
            {
                total = stats.Total,
                unresolved = stats.Unresolved,
                mapped = stats.Mapped,
                ignored = stats.Ignored,
                unavailable = stats.Unavailable
            });
        })
        .WithName("GetUnmatchedCountriesStats")
        .WithDescription("Get statistics about unmatched countries");

        // GET /api/unmatched-countries/suggestions/{id}
        group.MapGet("/suggestions/{id:guid}", async (
            Guid id,
            string? search,
            IUnmatchedCountryRepository unmatchedRepo,
            ICountryRepository countryRepo) =>
        {
            var unmatchedCountry = await unmatchedRepo.GetByIdAsync(id);
            if (unmatchedCountry == null)
                return Results.NotFound(new { error = "Unmatched country not found" });

            var allCountries = await countryRepo.GetAllAsync();

            // Filter by search if provided
            IEnumerable<Configuration.Entities.Country> suggestions = allCountries;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLowerInvariant();
                suggestions = allCountries.Where(c =>
                    c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    c.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.NameCs?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.IsoCode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return Results.Ok(new
            {
                unmatchedCountry = new
                {
                    unmatchedCountry.Id,
                    unmatchedCountry.ProviderCountryName,
                    unmatchedCountry.ProviderSlug
                },
                suggestions = suggestions.Take(50).Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.NameCs,
                    c.Code,
                    c.IsoCode,
                    c.FlagEmoji
                })
            });
        })
        .WithName("GetUnmatchedCountrySuggestions")
        .WithDescription("Get suggested countries for mapping an unmatched country");
    }
}

// DTOs
public record UnmatchedCountryDto
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public string? ProviderName { get; init; }
    public string? ProviderCountryId { get; init; }
    public string ProviderCountryName { get; init; } = string.Empty;
    public string? ProviderSlug { get; init; }
    public DateTime ScrapedAt { get; init; }
    public bool IsResolved { get; init; }
    public string? ResolutionType { get; init; }
    public Guid? ResolvedCountryId { get; init; }
    public string? ResolvedCountryName { get; init; }
    public string? ResolvedCountryCode { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
}

public record ResolveCountryAsMappedRequest(Guid CountryId, string? Notes = null);
public record ResolveCountryAsIgnoredRequest(string? Notes = null);
public record ResolveCountryAsUnavailableRequest(string? Notes = null);
