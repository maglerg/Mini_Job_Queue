using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.Models;
using Mini_Job_Queue.API.Models.Enums;
using Mini_Job_Queue.API.Queue;

namespace Mini_Job_Queue.API.Workers;

public class AutomationScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationScheduler> _logger;

    public AutomationScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomationScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationScheduler запущен.");

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessRulesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Приложение завершает работу.
        }

        _logger.LogInformation("AutomationScheduler остановлен.");
    }

    private async Task ProcessRulesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var jobQueue = scope.ServiceProvider
                .GetRequiredService<IJobQueue>();

            var now = DateTime.UtcNow;

            var rules = await dbContext.Set<AutomationRule>()
                .Include(rule => rule.Urls)
                .Where(rule =>
                    rule.IsEnabled &&
                    rule.NextRunAt != null &&
                    rule.NextRunAt <= now)
                .ToListAsync(cancellationToken);

            foreach (var rule in rules)
            {
                if (rule.Urls.Count == 0)
                {
                    _logger.LogWarning(
                        "Правило {RuleId} не содержит URL.",
                        rule.Id);

                    continue;
                }

                var job = new Job
                {
                    Id = Guid.NewGuid(),
                    Type = JobType.UrlCheck,
                    Status = JobStatus.Queued,
                    CreatedAt = now,
                    AutomationRuleId = rule.Id,

                    Results = rule.Urls.Select(item =>
                        new UrlCheckResult
                        {
                            Id = Guid.NewGuid(),
                            Url = item.Url
                        }).ToList()
                };

                dbContext.Jobs.Add(job);

                rule.LastRunAt = now;
                rule.NextRunAt = now.AddSeconds(
                    rule.IntervalSeconds);

                await dbContext.SaveChangesAsync(
                    cancellationToken);

                await jobQueue.EnqueueAsync(
                    job.Id,
                    cancellationToken);

                _logger.LogInformation(
                    "Создана задача {JobId} для правила {RuleId}.",
                    job.Id,
                    rule.Id);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Ошибка при обработке правил автоматизации.");
        }
    }
}