using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Services;
using Sazkomat.Core.Enums;

namespace Sazkomat.Api.Endpoints;

/// <summary>
/// Authentication endpoints for user registration, login, and profile management
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        // POST /api/auth/register - User registration
        group.MapPost("/register", async (
            [FromBody] RegisterRequest request,
            IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(request);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error, errors = result.Errors });
            return Results.Created("/api/auth/me", result.Value);
        })
        .RequireRateLimiting("auth_register")
        .WithName("Register")
        .Produces<AuthResponse>(201)
        .Produces(400)
        .Produces(429);

        // POST /api/auth/login - Login (returns JWT)
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });
            return Results.Ok(result.Value);
        })
        .RequireRateLimiting("auth_login")
        .WithName("Login")
        .Produces<AuthResponse>(200)
        .Produces(400)
        .Produces(429);

        // GET /api/auth/me - Get current user (requires auth)
        group.MapGet("/me", async (
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await authService.GetUserByIdAsync(userId);
            if (!result.IsSuccess)
                return Results.NotFound(new { error = result.Error });
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .Produces<UserInfoDto>(200)
        .Produces(401)
        .Produces(404);

        // PATCH /api/auth/me/language - Update language preference
        group.MapPatch("/me/language", async (
            [FromBody] UpdateLanguageRequest request,
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var result = await authService.UpdateLanguagePreferenceAsync(userId, request.LanguagePreference);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .WithName("UpdateLanguagePreference")
        .Produces<UserInfoDto>(200)
        .Produces(400)
        .Produces(401);

        // POST /api/auth/logout - Logout (for API consistency)
        group.MapPost("/logout", (ClaimsPrincipal user) =>
        {
            // JWT is stateless - client removes token
            // This endpoint exists for API consistency and future cookie-based auth
            return Results.Ok(new { message = "Logged out successfully" });
        })
        .RequireAuthorization()
        .WithName("Logout")
        .Produces(200)
        .Produces(401);

        // POST /api/auth/google - Login with Google OAuth (ID token flow)
        group.MapPost("/google", async (
            [FromBody] GoogleLoginRequest request,
            IAuthService authService) =>
        {
            var result = await authService.GoogleLoginAsync(request);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });
            return Results.Ok(result.Value);
        })
        .RequireRateLimiting("auth_login")
        .WithName("GoogleLogin")
        .Produces<AuthResponse>(200)
        .Produces(400)
        .Produces(429);
    }
}
