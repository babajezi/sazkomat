using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Common;
using Sazkomat.Core.Enums;

namespace Sazkomat.Configuration.Services;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Authentication response with JWT token</returns>
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Login a user with email and password
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Authentication response with JWT token</returns>
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);

    /// <summary>
    /// Generate JWT token for a user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <returns>JWT token string</returns>
    string GenerateJwtToken(ApplicationUser user);

    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">JWT token string</param>
    /// <returns>True if token is valid</returns>
    Task<Result<bool>> ValidateTokenAsync(string token);

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User info DTO</returns>
    Task<Result<UserInfoDto>> GetUserByIdAsync(string userId);

    /// <summary>
    /// Update user's language preference
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="language">New language preference</param>
    /// <returns>Updated user info DTO</returns>
    Task<Result<UserInfoDto>> UpdateLanguagePreferenceAsync(string userId, LanguagePreference language);

    /// <summary>
    /// Login or register a user via Google OAuth
    /// </summary>
    /// <param name="request">Google login request with ID token</param>
    /// <returns>Authentication response with JWT token</returns>
    Task<Result<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request);

    // ===== Admin Methods =====

    /// <summary>
    /// Get all users (admin only)
    /// </summary>
    /// <returns>List of all users</returns>
    Task<Result<IEnumerable<UserInfoDto>>> GetAllUsersAsync();

    /// <summary>
    /// Get users pending approval (admin only)
    /// </summary>
    /// <returns>List of users awaiting approval</returns>
    Task<Result<IEnumerable<UserInfoDto>>> GetPendingUsersAsync();

    /// <summary>
    /// Approve a user (admin only)
    /// </summary>
    /// <param name="userId">User ID to approve</param>
    /// <param name="approvedBy">Admin email who approved</param>
    /// <returns>Updated user info</returns>
    Task<Result<UserInfoDto>> ApproveUserAsync(string userId, string approvedBy);

    /// <summary>
    /// Reject (delete) a user (admin only)
    /// </summary>
    /// <param name="userId">User ID to reject</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> RejectUserAsync(string userId);

    /// <summary>
    /// Delete a user (admin only)
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> DeleteUserAsync(string userId);

    /// <summary>
    /// Update user information (admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated user info</returns>
    Task<Result<UserInfoDto>> UpdateUserAsync(string userId, UpdateUserRequest request);

    /// <summary>
    /// Check if an email is the admin email
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <returns>True if email is admin</returns>
    bool IsAdmin(string email);
}
