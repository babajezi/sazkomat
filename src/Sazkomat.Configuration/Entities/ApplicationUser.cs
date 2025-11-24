using Microsoft.AspNetCore.Identity;
using Sazkomat.Core.Enums;

namespace Sazkomat.Configuration.Entities;

/// <summary>
/// Application user entity extending ASP.NET Core Identity
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's preferred language for UI and display names
    /// </summary>
    public LanguagePreference LanguagePreference { get; set; } = LanguagePreference.Czech;

    /// <summary>
    /// User's display name (optional, defaults to Email or UserName)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time user updated their profile
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
