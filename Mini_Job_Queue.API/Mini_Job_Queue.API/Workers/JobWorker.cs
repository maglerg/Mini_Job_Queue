
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.Models;
using Mini_Job_Queue.API.Models.Enums;
using Mini_Job_Queue.API.Queue;
using Mini_Job_Queue.API.Services;

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
            await ProcessJobAsync(jobId, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(
        Guid jobId,
        CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var urlCheckService = scope.ServiceProvider
            .GetRequiredService<UrlCheckService>();

        Job? job = null;

        try
        {
            job = await dbContext.Jobs
                .Include(j => j.Results)
                .FirstOrDefaultAsync(
                    j => j.Id == jobId,
                    stoppingToken);

            if (job is null)
            {
                _logger.LogWarning(
                    "Задача {JobId} не найдена",
                    jobId);

                return;
            }
            _logger.LogInformation(
    "Начало обработки задачи {JobId} в {Time}",
    jobId,
    DateTime.UtcNow
);
            job.Status = JobStatus.Running;
            job.StartedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(stoppingToken);


            foreach (var result in job.Results)
            {
                _logger.LogInformation(
    "Начинаю проверку URL: {Url}",
    result.Url);

                var outcome = await urlCheckService.CheckUrlAsync(
                    result.Url,
                    stoppingToken);

                _logger.LogInformation(
                    "Результат проверки URL: {Url}, HTTP: {StatusCode}, Время: {Time} мс, Ошибка: {Error}",
                    result.Url,
                    outcome.HttpStatusCode,
                    outcome.ResponseTimeMs,
                    outcome.Error);

                result.HttpStatusCode = outcome.HttpStatusCode;
                result.ResponseTimeMs = outcome.ResponseTimeMs;
                result.Error = outcome.Error;

                await dbContext.SaveChangesAsync(stoppingToken);
            }
            _logger.LogInformation(
    "Завершение обработки задачи {JobId} в {Time}",
    jobId,
    DateTime.UtcNow
);

            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Задача {JobId} завершена",
                job.Id);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Ошибка обработки задачи {JobId}",
                jobId);

            if (job is not null)
            {
                job.Status = JobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;

                try
                {
                    await dbContext.SaveChangesAsync(
                        CancellationToken.None);
                }
                catch (Exception saveException)
                {
                    _logger.LogError(
                        saveException,
                        "Не удалось сохранить статус Failed для {JobId}",
                        jobId);
                }
            }
        }
    }
}
