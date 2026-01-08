using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Settings;
using Sazkomat.Core.Common;
using Sazkomat.Core.Enums;

namespace Sazkomat.Configuration.Services;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly AdminSettings _adminSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings,
        IOptions<AdminSettings> adminSettings,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
        _adminSettings = adminSettings.Value;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Check if an email is the admin email
    /// </summary>
    private bool IsAdminEmail(string email)
    {
        return !string.IsNullOrEmpty(_adminSettings.AdminEmail) &&
               string.Equals(email, _adminSettings.AdminEmail, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure("User with this email already exists");
        }

        // Check if this is the admin email (auto-approve)
        var isAdmin = IsAdminEmail(request.Email);

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            LanguagePreference = request.LanguagePreference ?? LanguagePreference.Czech,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsApproved = isAdmin, // Auto-approve admin
            ApprovedAt = isAdmin ? DateTime.UtcNow : null,
            ApprovedBy = isAdmin ? "System (Admin Email)" : null
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<AuthResponse>.Failure($"Failed to create user: {errors}");
        }

        _logger.LogInformation("User registered: {Email}, IsApproved: {IsApproved}, IsAdmin: {IsAdmin}",
            user.Email, user.IsApproved, isAdmin);

        // Return response (user may not be approved yet)
        var authResponse = new AuthResponse(
            user.IsApproved ? GenerateJwtToken(user) : "", // Only generate token if approved
            user.IsApproved ? DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes) : DateTime.MinValue,
            new UserInfoDto(
                user.Id,
                user.Email!,
                user.DisplayName,
                user.LanguagePreference,
                user.CreatedAt,
                user.IsApproved,
                isAdmin
            )
        );

        return Result<AuthResponse>.Success(authResponse);
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password");
        }

        // Verify password
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Result<AuthResponse>.Failure("Invalid email or password");
        }

        // Check if user is approved
        if (!user.IsApproved)
        {
            _logger.LogWarning("Login attempt by unapproved user: {Email}", user.Email);
            return Result<AuthResponse>.Failure("Váš účet čeká na schválení administrátorem");
        }

        var isAdmin = IsAdminEmail(user.Email!);

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var authResponse = new AuthResponse(
            token,
            expiresAt,
            new UserInfoDto(
                user.Id,
                user.Email!,
                user.DisplayName,
                user.LanguagePreference,
                user.CreatedAt,
                user.IsApproved,
                isAdmin
            )
        );

        return Result<AuthResponse>.Success(authResponse);
    }

    /// <inheritdoc />
    public string GenerateJwtToken(ApplicationUser user)
    {
        var isAdmin = IsAdminEmail(user.Email!);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("language_preference", user.LanguagePreference.ToString()),
            new Claim("display_name", user.DisplayName ?? user.Email!),
            new Claim("is_admin", isAdmin.ToString().ToLower())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Token validation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<UserInfoDto>> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<UserInfoDto>.Failure("User not found");
        }

        var isAdmin = IsAdminEmail(user.Email!);

        var userInfo = new UserInfoDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.LanguagePreference,
            user.CreatedAt,
            user.IsApproved,
            isAdmin
        );

        return Result<UserInfoDto>.Success(userInfo);
    }

    /// <inheritdoc />
    public async Task<Result<UserInfoDto>> UpdateLanguagePreferenceAsync(string userId, LanguagePreference language)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<UserInfoDto>.Failure("User not found");
        }

        user.LanguagePreference = language;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserInfoDto>.Failure($"Failed to update language preference: {errors}");
        }

        var isAdmin = IsAdminEmail(user.Email!);

        var userInfo = new UserInfoDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.LanguagePreference,
            user.CreatedAt,
            user.IsApproved,
            isAdmin
        );

        return Result<UserInfoDto>.Success(userInfo);
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request)
    {
        try
        {
            // Get Google Client ID from configuration
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrEmpty(googleClientId) || googleClientId == "YOUR_GOOGLE_CLIENT_ID")
            {
                _logger.LogError("Google OAuth not configured - ClientId is missing or placeholder");
                return Result<AuthResponse>.Failure("Google OAuth is not configured");
            }

            // Validate Google ID token
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning("Invalid Google ID token: {Message}", ex.Message);
                return Result<AuthResponse>.Failure("Invalid Google ID token");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(payload.Email);
            var isAdmin = IsAdminEmail(payload.Email);

            ApplicationUser user;

            if (existingUser != null)
            {
                // Existing user - update info if needed
                user = existingUser;

                // Check if user is approved
                if (!user.IsApproved)
                {
                    _logger.LogWarning("Google login attempt by unapproved user: {Email}", user.Email);
                    return Result<AuthResponse>.Failure("Váš účet čeká na schválení administrátorem");
                }

                // Update display name from Google if not set
                if (string.IsNullOrEmpty(user.DisplayName) && !string.IsNullOrEmpty(payload.Name))
                {
                    user.DisplayName = payload.Name;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                }

                _logger.LogInformation("Google OAuth login for existing user: {Email}", payload.Email);
            }
            else
            {
                // New user - create account
                user = new ApplicationUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    EmailConfirmed = payload.EmailVerified, // Google already verified
                    DisplayName = payload.Name,
                    LanguagePreference = request.LanguagePreference ?? LanguagePreference.Czech,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsApproved = isAdmin, // Auto-approve admin
                    ApprovedAt = isAdmin ? DateTime.UtcNow : null,
                    ApprovedBy = isAdmin ? "System (Admin Email)" : null
                };

                // Create user without password (OAuth user)
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create Google OAuth user: {Errors}", errors);
                    return Result<AuthResponse>.Failure($"Failed to create user: {errors}");
                }

                // Add Google login to user
                var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
                if (!addLoginResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add Google login info: {Errors}",
                        string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                }

                _logger.LogInformation("Created new user via Google OAuth: {Email}, IsApproved: {IsApproved}",
                    payload.Email, user.IsApproved);
            }

            // Return response (user may not be approved yet for new users)
            var authResponse = new AuthResponse(
                user.IsApproved ? GenerateJwtToken(user) : "",
                user.IsApproved ? DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes) : DateTime.MinValue,
                new UserInfoDto(
                    user.Id,
                    user.Email!,
                    user.DisplayName,
                    user.LanguagePreference,
                    user.CreatedAt,
                    user.IsApproved,
                    isAdmin
                )
            );

            return Result<AuthResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google OAuth login");
            return Result<AuthResponse>.Failure($"Google login failed: {ex.Message}");
        }
    }

    // ===== Admin Methods =====

    /// <inheritdoc />
    public bool IsAdmin(string email)
    {
        return IsAdminEmail(email);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<UserInfoDto>>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        var userDtos = users.Select(u => new UserInfoDto(
            u.Id,
            u.Email!,
            u.DisplayName,
            u.LanguagePreference,
            u.CreatedAt,
            u.IsApproved,
            IsAdminEmail(u.Email!)
        ));

        return Result<IEnumerable<UserInfoDto>>.Success(userDtos);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<UserInfoDto>>> GetPendingUsersAsync()
    {
        var users = _userManager.Users.Where(u => !u.IsApproved).ToList();
        var userDtos = users.Select(u => new UserInfoDto(
            u.Id,
            u.Email!,
            u.DisplayName,
            u.LanguagePreference,
            u.CreatedAt,
            u.IsApproved,
            IsAdminEmail(u.Email!)
        ));

        return Result<IEnumerable<UserInfoDto>>.Success(userDtos);
    }

    /// <inheritdoc />
    public async Task<Result<UserInfoDto>> ApproveUserAsync(string userId, string approvedBy)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<UserInfoDto>.Failure("User not found");
        }

        if (user.IsApproved)
        {
            return Result<UserInfoDto>.Failure("User is already approved");
        }

        user.IsApproved = true;
        user.ApprovedAt = DateTime.UtcNow;
        user.ApprovedBy = approvedBy;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserInfoDto>.Failure($"Failed to approve user: {errors}");
        }

        _logger.LogInformation("User approved: {Email} by {ApprovedBy}", user.Email, approvedBy);

        return Result<UserInfoDto>.Success(new UserInfoDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.LanguagePreference,
            user.CreatedAt,
            user.IsApproved,
            IsAdminEmail(user.Email!)
        ));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> RejectUserAsync(string userId)
    {
        return await DeleteUserAsync(userId);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<bool>.Failure("User not found");
        }

        // Prevent deleting admin
        if (IsAdminEmail(user.Email!))
        {
            return Result<bool>.Failure("Cannot delete admin user");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<bool>.Failure($"Failed to delete user: {errors}");
        }

        _logger.LogInformation("User deleted: {Email}", user.Email);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<UserInfoDto>> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<UserInfoDto>.Failure("User not found");
        }

        if (request.DisplayName != null)
        {
            user.DisplayName = request.DisplayName;
        }

        if (request.LanguagePreference.HasValue)
        {
            user.LanguagePreference = request.LanguagePreference.Value;
        }

        if (request.IsApproved.HasValue && request.IsApproved.Value && !user.IsApproved)
        {
            user.IsApproved = true;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedBy = "Admin";
        }

        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserInfoDto>.Failure($"Failed to update user: {errors}");
        }

        _logger.LogInformation("User updated: {Email}", user.Email);

        return Result<UserInfoDto>.Success(new UserInfoDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.LanguagePreference,
            user.CreatedAt,
            user.IsApproved,
            IsAdminEmail(user.Email!)
        ));
    }
}
