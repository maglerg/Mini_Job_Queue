using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.DTOs;
using Mini_Job_Queue.API.Models;
using Mini_Job_Queue.API.Models.Enums;
using Mini_Job_Queue.API.Queue;

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
}