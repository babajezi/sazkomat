namespace Sazkomat.Configuration.DTOs;

public record UpdateProviderConfigurationRequest(
    int? Timeout,
    string? ProxyUrl,
    Dictionary<string, string>? CustomSettings
);
