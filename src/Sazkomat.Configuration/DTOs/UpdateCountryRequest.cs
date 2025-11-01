namespace Sazkomat.Configuration.DTOs;

public record UpdateCountryRequest(
    string? Name = null,
    string? Code = null,
    string? FlagEmoji = null,
    bool? IsActive = null
);
