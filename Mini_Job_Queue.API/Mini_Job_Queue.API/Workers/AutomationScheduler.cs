using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.Models;
using Mini_Job_Queue.API.Models.Enums;
using Mini_Job_Queue.API.Queue;

namespace Mini_Job_Queue.API.Workers;

public sealed class AutomationScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AutomationScheduleSignal _signal;
    private readonly ILogger<AutomationScheduler> _logger;

    public AutomationScheduler(
        IServiceScopeFactory scopeFactory,
        AutomationScheduleSignal signal,
        ILogger<AutomationScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _signal = signal;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutomationScheduler запущен.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Обрабатываем все уже наступившие запуски.
                    await ProcessDueRulesAsync(stoppingToken);

                    // Находим ближайшее следующее время запуска.
                    var nextRunAt =
                        await GetNextRunAtAsync(stoppingToken);

                    TimeSpan waitTime;

                    if (nextRunAt is null)
                    {
                        // Активных расписаний нет.
                        waitTime = Timeout.InfiniteTimeSpan;
                    }
                    else
                    {
                        waitTime = nextRunAt.Value - DateTime.UtcNow;

                        if (waitTime < TimeSpan.Zero)
                        {
                            waitTime = TimeSpan.Zero;
                        }
                    }

                    _logger.LogDebug(
                        "Следующий запуск: {NextRunAt}. " +
                        "Время ожидания: {WaitTime}.",
                        nextRunAt,
                        waitTime);

                    // Просыпаемся либо по сигналу изменения,
                    // либо когда наступит время запуска.
                    await _signal.WaitForChangeAsync(
                        waitTime,
                        stoppingToken);
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
                        "Ошибка планировщика. Повторная попытка через 5 секунд.");

                    // Не допускаем бесконечного цикла при ошибке БД.
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Нормальное завершение приложения.
        }

        _logger.LogInformation("AutomationScheduler остановлен.");
    }

    private async Task<DateTime?> GetNextRunAtAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var nextUrlRun = await dbContext.AutomationRules
            .Where(rule =>
                rule.IsEnabled &&
                rule.NextRunAt != null)
            .Select(rule => rule.NextRunAt)
            .MinAsync(cancellationToken);

        var nextWeatherRun = await dbContext.WeatherTrackings
            .Where(tracking =>
                tracking.IsEnabled &&
                tracking.NextRunAt != null)
            .Select(tracking => tracking.NextRunAt)
            .MinAsync(cancellationToken);

        if (nextUrlRun is null)
            return nextWeatherRun;

        if (nextWeatherRun is null)
            return nextUrlRun;

        return nextUrlRun.Value <= nextWeatherRun.Value
            ? nextUrlRun
            : nextWeatherRun;
    }

    private async Task ProcessDueRulesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var jobQueue = scope.ServiceProvider
            .GetRequiredService<IJobQueue>();

        var now = DateTime.UtcNow;

        // 1. Обрабатываем правила проверки URL.

        var rules = await dbContext.AutomationRules
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
                    "Правило {RuleId} не содержит URL. " +
                    "Пропускаем запуск.",
                    rule.Id);

                // Не оставляем правило с просроченным временем:
                // иначе планировщик будет постоянно просыпаться.
                rule.LastRunAt = now;
                rule.NextRunAt = now.AddSeconds(
                    rule.IntervalSeconds);

                await dbContext.SaveChangesAsync(
                    cancellationToken);

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
                "Создана задача {JobId} для правила URL {RuleId}.",
                job.Id,
                rule.Id);
        }

        // 2. Обрабатываем расписания отслеживания погоды.

        var weatherTrackings = await dbContext.WeatherTrackings
            .Where(tracking =>
                tracking.IsEnabled &&
                tracking.NextRunAt != null &&
                tracking.NextRunAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var tracking in weatherTrackings)
        {
            var job = new Job
            {
                Id = Guid.NewGuid(),
                Type = JobType.WeatherCheck,
                Status = JobStatus.Queued,
                CreatedAt = now,
                WeatherTrackingId = tracking.Id
            };

            dbContext.Jobs.Add(job);

            tracking.LastRunAt = now;
            tracking.NextRunAt = now.AddMinutes(
                tracking.IntervalMinutes);

            await dbContext.SaveChangesAsync(
                cancellationToken);

            await jobQueue.EnqueueAsync(
                job.Id,
                cancellationToken);

            _logger.LogInformation(
                "Создана задача погоды {JobId} для города {LocationName}. " +
                "Следующий запуск: {NextRunAt}.",
                job.Id,
                tracking.LocationName,
                tracking.NextRunAt);
        }
    }
}