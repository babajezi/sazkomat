using Hangfire.Dashboard;

namespace Sazkomat.Api.Middleware;

/// <summary>
/// Authorization filter for Hangfire Dashboard in production.
/// Requires authenticated admin user to access the dashboard.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // Check for admin role or specific admin email
        var userEmail = httpContext.User.Claims
            .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

        // Allow admin users (configured in AdminSettings)
        var configuration = httpContext.RequestServices.GetService<IConfiguration>();
        var adminEmail = configuration?.GetValue<string>("AdminSettings:AdminEmail");

        if (!string.IsNullOrEmpty(adminEmail) &&
            string.Equals(userEmail, adminEmail, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for Admin role claim
        return httpContext.User.IsInRole("Admin");
    }
}
