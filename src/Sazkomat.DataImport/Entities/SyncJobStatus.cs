namespace Sazkomat.DataImport.Entities;

public enum SyncJobStatus
{
    Pending,
    Running,
    Completed,
    PartiallyCompleted,
    Failed,
    Cancelled
}
