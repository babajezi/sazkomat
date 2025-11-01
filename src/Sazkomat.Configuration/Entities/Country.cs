using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class Country : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<League> Leagues { get; set; } = new List<League>();
    public ICollection<CountryProvider> CountryProviders { get; set; } = new List<CountryProvider>();
}
