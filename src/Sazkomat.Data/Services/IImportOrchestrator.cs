using Sazkomat.Core.Common;
using Sazkomat.Data.DTOs;
using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Services;

public interface IImportOrchestrator
{
    Task<Result<ImportJob>> StartHistoricalImportAsync(HistoricalImportRequest request);
    Task<ImportJob?> GetJobStatusAsync(Guid jobId);
    Task<ImportStatsResponse?> GetImportStatsAsync(Guid leagueId);
    Task<DashboardStatsResponse> GetDashboardStatsAsync();
}
