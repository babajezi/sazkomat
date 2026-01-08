using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Sazkomat.Configuration.DTOs;
using Sazkomat.Configuration.Services;

namespace Sazkomat.Api.Endpoints;

/// <summary>
/// Admin endpoints for user management (approval, listing, deletion)
/// </summary>
public static class UserAdminEndpoints
{
    public static void MapUserAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("Admin - Users")
            .WithOpenApi()
            .RequireAuthorization();

        // GET /api/admin/users - List all users (admin only)
        group.MapGet("/", async (
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            // Check if user is admin
            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email) || !authService.IsAdmin(email))
                return Results.Forbid();

            var result = await authService.GetAllUsersAsync();
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });
            return Results.Ok(result.Value);
        })
        .WithName("GetAllUsers")
        .Produces<IEnumerable<UserInfoDto>>(200)
        .Produces(401)
        .Produces(403);

        // GET /api/admin/users/pending - List users pending approval (admin only)
        group.MapGet("/pending", async (
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            // Check if user is admin
            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email) || !authService.IsAdmin(email))
                return Results.Forbid();

            var result = await authService.GetPendingUsersAsync();
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });
            return Results.Ok(result.Value);
        })
        .WithName("GetPendingUsers")
        .Produces<IEnumerable<UserInfoDto>>(200)
        .Produces(401)
        .Produces(403);

        // POST /api/admin/users/{id}/approve - Approve a user (admin only)
        group.MapPost("/{id}/approve", async (
            string id,
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            // Check if user is admin
            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email) || !authService.IsAdmin(email))
                return Results.Forbid();

            var result = await authService.ApproveUserAsync(id, email);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });
            return Results.Ok(result.Value);
        })
        .WithName("ApproveUser")
        .Produces<UserInfoDto>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);

        // POST /api/admin/users/{id}/reject - Reject (delete) a user (admin only)
        group.MapPost("/{id}/reject", async (
            string id,
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            // Check if user is admin
            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email) || !authService.IsAdmin(email))
                return Results.Forbid();

            var result = await authService.RejectUserAsync(id);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });
            return Results.Ok(new { message = "User rejected and deleted" });
        })
        .WithName("RejectUser")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);

        // DELETE /api/admin/users/{id} - Delete a user (admin only)
        group.MapDelete("/{id}", async (
            string id,
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            // Check if user is admin
            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email) || !authService.IsAdmin(email))
                return Results.Forbid();

            var result = await authService.DeleteUserAsync(id);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });
            return Results.Ok(new { message = "User deleted" });
        })
        .WithName("DeleteUser")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);

        // PATCH /api/admin/users/{id} - Update a user (admin only)
        group.MapPatch("/{id}", async (
            string id,
            [FromBody] UpdateUserRequest request,
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            // Check if user is admin
            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email) || !authService.IsAdmin(email))
                return Results.Forbid();

            var result = await authService.UpdateUserAsync(id, request);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });
            return Results.Ok(result.Value);
        })
        .WithName("UpdateUser")
        .Produces<UserInfoDto>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403);
    }
}
