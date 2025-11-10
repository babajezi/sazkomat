using System.Text.Json.Serialization;
using Sazkomat.Configuration.Entities;
using Sazkomat.Core.Entities;

namespace Sazkomat.DataImport.Entities;

/// <summary>
/// Represents a sync job in the queue for background processing.
/// Tracks scan, import, and live update operations with their progress and status.
/// </summary>
public class SyncJob : Entity
{
    public Guid ProviderId { get; set; }

    [JsonPropertyName("jobType")]
    public SyncJobType Type { get; set; }

    public SyncEntityType EntityType { get; set; }
    public SyncJobStatus Status { get; set; } = SyncJobStatus.Pending;
    public int Priority { get; set; } = 5;  // 0 = highest, 9 = lowest

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }  // For delayed/recurring jobs

    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;

    // Progress tracking (stored as JSON)
    public string? ProgressData { get; set; }  // JSONB - { total, processed, created, updated, skipped, errors }

    // Optional filters for scoped operations
    public List<Guid> CountryIds { get; set; } = new();
    public List<Guid> LeagueIds { get; set; } = new();
    public List<Guid> SeasonIds { get; set; } = new();

    // Navigation properties
    public DataProvider Provider { get; set; } = null!;
}
