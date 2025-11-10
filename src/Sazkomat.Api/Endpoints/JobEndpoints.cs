using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Sazkomat.DataImport.Services;

namespace Sazkomat.Api.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Jobs")
            .WithOpenApi();

        // Get job status
        group.MapGet("/{jobId:guid}", async (
            Guid jobId,
            ISyncJobProcessor jobProcessor) =>
        {
            var job = await jobProcessor.GetJobStatusAsync(jobId);
            if (job == null)
                return Results.NotFound(new { message = "Job not found" });

            return Results.Ok(job);
        })
        .WithName("GetJobStatus")
        .Produces(200)
        .Produces(404);

        // Get recent jobs for provider
        group.MapGet("/recent", async (
            [FromQuery] Guid providerId,
            [FromQuery] int count,
            ISyncJobProcessor jobProcessor) =>
        {
            var jobs = await jobProcessor.GetRecentJobsAsync(providerId, count);
            return Results.Ok(jobs);
        })
        .WithName("GetRecentJobs")
        .Produces(200);

        // Enqueue scan job
        group.MapPost("/scan", (
            [FromBody] EnqueueScanJobRequest request,
            IBackgroundJobClient backgroundJobClient,
            ISyncJobProcessor jobProcessor) =>
        {
            var hangfireJobId = backgroundJobClient.Enqueue(() =>
                jobProcessor.ProcessScanJobAsync(request.JobId));

            return Results.Ok(new {
                jobId = request.JobId,
                hangfireJobId,
                message = "Scan job enqueued"
            });
        })
        .WithName("EnqueueScanJob")
        .Produces(200);

        // Enqueue import job
        group.MapPost("/import", (
            [FromBody] EnqueueImportJobRequest request,
            IBackgroundJobClient backgroundJobClient,
            ISyncJobProcessor jobProcessor) =>
        {
            var hangfireJobId = backgroundJobClient.Enqueue(() =>
                jobProcessor.ProcessImportJobAsync(request.JobId));

            return Results.Ok(new {
                jobId = request.JobId,
                hangfireJobId,
                message = "Import job enqueued"
            });
        })
        .WithName("EnqueueImportJob")
        .Produces(200);

        // Enqueue live sync job
        group.MapPost("/livesync", (
            [FromBody] EnqueueLiveSyncJobRequest request,
            IBackgroundJobClient backgroundJobClient,
            ISyncJobProcessor jobProcessor) =>
        {
            var hangfireJobId = backgroundJobClient.Enqueue(() =>
                jobProcessor.ProcessLiveSyncJobAsync(request.JobId));

            return Results.Ok(new {
                jobId = request.JobId,
                hangfireJobId,
                message = "Live sync job enqueued"
            });
        })
        .WithName("EnqueueLiveSyncJob")
        .Produces(200);
    }
}

public record EnqueueScanJobRequest(Guid JobId);
public record EnqueueImportJobRequest(Guid JobId);
public record EnqueueLiveSyncJobRequest(Guid JobId);
