using Sazkomat.Core.Enums;

namespace Sazkomat.Configuration.DTOs;

/// <summary>
/// User registration request
/// </summary>
public record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName = null,
    LanguagePreference? LanguagePreference = null
);

/// <summary>
/// User login request
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);

/// <summary>
/// Authentication response with JWT token
/// </summary>
public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    UserInfoDto User
);

/// <summary>
/// User information DTO
/// </summary>
public record UserInfoDto(
    string Id,
    string Email,
    string? DisplayName,
    LanguagePreference LanguagePreference,
    DateTime CreatedAt,
    bool IsApproved = false,
    bool IsAdmin = false
);

/// <summary>
/// Request to update user's language preference
/// </summary>
public record UpdateLanguageRequest(LanguagePreference LanguagePreference);

/// <summary>
/// Google OAuth login request - contains the ID token from Google Sign-In
/// </summary>
public record GoogleLoginRequest(
    /// <summary>
    /// Google ID token obtained from Google Sign-In on the frontend
    /// </summary>
    string IdToken,

    /// <summary>
    /// Optional language preference for new users
    /// </summary>
    LanguagePreference? LanguagePreference = null
);

/// <summary>
/// Admin request to update user information
/// </summary>
public record UpdateUserRequest(
    string? DisplayName = null,
    LanguagePreference? LanguagePreference = null,
    bool? IsApproved = null
);
