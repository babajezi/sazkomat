using Sazkomat.Core.Entities;

namespace Sazkomat.Strategy.Entities;

// TODO: Implement Strategy business logic in Phase 2
// This is a placeholder entity for future implementation
public class Strategy : Entity
{
    public Guid SportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = false;
}
