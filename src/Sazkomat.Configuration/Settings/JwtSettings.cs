namespace Sazkomat.Configuration.Settings;

/// <summary>
/// JWT authentication settings
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing JWT tokens (min 32 characters)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT token issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// JWT token audience
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 1440; // 24 hours default
}
