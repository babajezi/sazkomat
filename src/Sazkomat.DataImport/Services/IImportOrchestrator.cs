using Sazkomat.Core.Common;
using Sazkomat.DataImport.DTOs;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Services;

public interface IImportOrchestrator
{
    Task<Result<ImportJob>> StartHistoricalImportAsync(HistoricalImportRequest request);
    Task<ImportJob?> GetJobStatusAsync(Guid jobId);
    Task<ImportStatsResponse?> GetImportStatsAsync(Guid leagueId);
    Task<DashboardStatsResponse> GetDashboardStatsAsync();
}
