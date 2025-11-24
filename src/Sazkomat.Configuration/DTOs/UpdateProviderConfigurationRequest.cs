namespace Sazkomat.Configuration.DTOs;

public record UpdateProviderConfigurationRequest(
    int? Timeout,
    string? ProxyUrl,
    List<string>? ExcludedCountryIds,
    List<string>? ExcludedLeagueIds,
    Dictionary<string, string>? CustomSettings
);
