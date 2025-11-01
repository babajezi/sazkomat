namespace Sazkomat.Configuration.DTOs;

public record UpdateProviderCredentialsRequest(
    string? Username,
    string? Password,
    string? SessionCookies
);
