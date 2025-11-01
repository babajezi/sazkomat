using Sazkomat.Configuration.Entities;

namespace Sazkomat.Configuration.DTOs;

public record UpdateDataProviderRequest(
    string? Name = null,
    string? Code = null,
    string? BaseUrl = null,
    ProviderType? Type = null,
    bool? IsActive = null,
    int? Priority = null,
    string? Notes = null
);
