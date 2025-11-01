namespace Sazkomat.Configuration.DTOs;

public record CreateCountryProviderRequest(
    Guid CountryId,
    Guid ProviderId,
    string ProviderCode,
    string? ProviderName = null,
    bool IsActive = true,
    string? Metadata = null
);
