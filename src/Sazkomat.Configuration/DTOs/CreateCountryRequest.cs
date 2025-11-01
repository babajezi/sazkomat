namespace Sazkomat.Configuration.DTOs;

public record CreateCountryRequest(
    string Name,
    string Code,
    string FlagEmoji
);
