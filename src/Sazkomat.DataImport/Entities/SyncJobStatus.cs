namespace Sazkomat.DataImport.Entities;

public enum SyncJobStatus
{
    Pending,
    Running,
    Completed,
    CompletedWithWarnings,
    PartiallyCompleted,
    Failed,
    Cancelled
}
