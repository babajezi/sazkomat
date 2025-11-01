using Sazkomat.Core.Entities;

namespace Sazkomat.Configuration.Entities;

public class SportProvider : Entity
{
    public Guid SportId { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Metadata { get; set; } // JSONB for provider-specific configuration

    // Navigation properties
    public Sport Sport { get; set; } = null!;
    public DataProvider Provider { get; set; } = null!;
}
