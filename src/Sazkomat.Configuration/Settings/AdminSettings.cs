namespace Sazkomat.Configuration.Settings;

/// <summary>
/// Settings for admin functionality
/// </summary>
public class AdminSettings
{
    /// <summary>
    /// Email address of the admin user who can approve new registrations
    /// </summary>
    public string AdminEmail { get; set; } = "";
}
