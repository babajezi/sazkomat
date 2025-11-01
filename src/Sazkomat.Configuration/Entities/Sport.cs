using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class Sport : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 10; // For display ordering

    // Navigation
    public ICollection<League> Leagues { get; set; } = new List<League>();
    public ICollection<SportProvider> SportProviders { get; set; } = new List<SportProvider>();
}
