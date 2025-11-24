namespace Sazkomat.Configuration.DTOs;

public record UpdateCountryRequest(
    string? Name = null,
    string? NameCs = null,
    string? Code = null,
    string? FlagEmoji = null,
    bool? IsActive = null
);
