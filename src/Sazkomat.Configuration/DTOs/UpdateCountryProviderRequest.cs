namespace Sazkomat.Configuration.DTOs;

public record UpdateCountryProviderRequest(
    string? ProviderCode = null,
    string? ProviderName = null,
    bool? IsActive = null,
    string? Metadata = null
);
