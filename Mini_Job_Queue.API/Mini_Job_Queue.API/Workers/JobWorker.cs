
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.Queue;

namespace Mini_Job_Queue.API.Workers;

public class JobWorker : BackgroundService
{
    private readonly IJobQueue _jobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobWorker> _logger;

    public JobWorker(
        IJobQueue jobQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<JobWorker> logger)
    {
        _jobQueue = jobQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobWorker запущен");

        await foreach (
            Guid jobId in _jobQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var dbContext = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var job = await dbContext.Jobs
                    .Include(j => j.Results)
                    .FirstOrDefaultAsync(
                        j => j.Id == jobId,
                        stoppingToken);

                if (job is null)
                {
                    _logger.LogWarning(
                        "Задача {JobId} не найдена",
                        jobId);

                    continue;
                }

                _logger.LogInformation(
                    "Задача {JobId} получена. Статус: {Status}. URL: {Count}",
                    job.Id,
                    job.Status,
                    job.Results.Count);

                foreach (var result in job.Results)
                {
                    _logger.LogInformation(
                        "URL для проверки: {Url}",
                        result.Url);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ошибка обработки задачи {JobId}",
                    jobId);
            }
        }
    }
}
