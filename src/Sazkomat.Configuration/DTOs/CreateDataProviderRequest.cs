using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.DTOs;

public record CreateDataProviderRequest(
    string Name,
    string Code,
    string BaseUrl,
    ProviderType Type,
    bool IsActive = true,
    int Priority = 10,
    string? Notes = null
);
