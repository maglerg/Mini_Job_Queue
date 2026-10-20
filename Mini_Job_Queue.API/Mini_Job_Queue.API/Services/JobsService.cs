using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.DTOs;
using Mini_Job_Queue.API.Models;
using Mini_Job_Queue.API.Models.Enums;
using Mini_Job_Queue.API.Queue;
using System.Text.Json;

namespace Mini_Job_Queue.API.Services;

public class JobsService
{
    private readonly AppDbContext _dbContext;
    private readonly IJobQueue _jobQueue;

    public JobsService(
        AppDbContext dbContext,
        IJobQueue jobQueue)
    {
        _dbContext = dbContext;
        _jobQueue = jobQueue;
    }

    public async Task<Job> CreateJobAsync(
        CreateUrlCheckJobRequest request,
        CancellationToken cancellationToken)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Status = JobStatus.Queued,
            Type = JobType.UrlCheck,
            CreatedAt = DateTime.UtcNow,
            Results = request.Urls.Select(url => new UrlCheckResult
            {
                Id = Guid.NewGuid(),
                Url = url
            }).ToList()
        };

        _dbContext.Jobs.Add(job);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _jobQueue.EnqueueAsync(job.Id, cancellationToken);

        return job;
    }

    public async Task<Job> CreateLogAnalysisJobAsync(
        CreateLogAnalysisJobRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var job = new Job
        {
            Type = JobType.LogAnalysis,
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            LogAnalysisInput = new LogAnalysisInput
            {
                SourceText = request.SourceText
            }
        };

        _dbContext.Jobs.Add(job);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _jobQueue.EnqueueAsync(job.Id, cancellationToken);

        return job;
    }

    public async Task<JobDetailsResponse?> GetJobDetailsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var job = await _dbContext.Jobs
            .AsNoTracking()
            .Include(j => j.Results)
            .Include(j => j.LogAnalysisResult)
            .FirstOrDefaultAsync(
                j => j.Id == id,
                cancellationToken);

        if (job is null)
            return null;

        LogAnalysisResultResponse? logResult = null;

        if (job.LogAnalysisResult is not null)
        {
            var savedResult = job.LogAnalysisResult;

            logResult = new LogAnalysisResultResponse
            {
                TotalLines = savedResult.TotalLines,
                ParsedLines = savedResult.ParsedLines,
                InvalidLines = savedResult.InvalidLines,
                CategoryCounts = DeserializeDictionary(
                    savedResult.CategoryCountsJson),
                TopMessages = DeserializeDictionary(
                    savedResult.TopMessagesJson),
                FailedAuthenticationByHost = DeserializeDictionary(
                    savedResult.FailedAuthenticationByHostJson)
            };
        }

        return new JobDetailsResponse
        {
            Id = job.Id,
            Type = job.Type.ToString(),
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,

            ExecutionTimeMs =
                job.StartedAt.HasValue && job.CompletedAt.HasValue
                    ? (long)(
                        job.CompletedAt.Value - job.StartedAt.Value)
                        .TotalMilliseconds
                    : null,

            Results = job.Results.Select(result =>
                new UrlCheckResultResponse
                {
                    Id = result.Id,
                    Url = result.Url,
                    HttpStatusCode = result.HttpStatusCode,
                    ResponseTimeMs = result.ResponseTimeMs,
                    Error = result.Error
                }).ToList(),

            LogAnalysisResult = logResult
        };
    }

    private static Dictionary<string, int> DeserializeDictionary(
        string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
            ?? new Dictionary<string, int>();
    }
}