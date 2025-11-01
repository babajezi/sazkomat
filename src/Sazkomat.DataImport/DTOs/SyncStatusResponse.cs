namespace Sazkomat.DataImport.DTOs;

public class SyncStatusResponse
{
    public bool IsRunning { get; set; }
    public SyncType? CurrentSyncType { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public SyncResponse? LastResult { get; set; }
}
