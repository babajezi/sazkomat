using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class CountryProvider : Entity
{
    public Guid CountryId { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Metadata { get; set; } // JSONB for additional provider-specific data

    // Navigation properties
    public Country Country { get; set; } = null!;
    public DataProvider Provider { get; set; } = null!;
}
