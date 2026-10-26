using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.DTOs;
using Mini_Job_Queue.API.Models;

namespace Mini_Job_Queue.API.Services;

public enum StopAutomationResult
{
    NotFound,
    AlreadyStopped,
    Stopped
}

public class AutomationRulesService
{
    private readonly AppDbContext _dbContext;
    private readonly AutomationScheduleNotifier _scheduleNotifier;

    public AutomationRulesService(
    AppDbContext dbContext,
    AutomationScheduleNotifier scheduleNotifier)
    {
        _dbContext = dbContext;
        _scheduleNotifier = scheduleNotifier;
    }

    public async Task<AutomationRule> CreateAsync(
        CreateAutomationRuleRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var rule = new AutomationRule
        {
            Id = Guid.NewGuid(),
            AutType = request.AutType,
            IntervalSeconds = request.IntervalSeconds,
            IsEnabled = true,
            CreatedAt = now,
            LastRunAt = null,

            // Первый запуск — через заданный интервал.
            NextRunAt = now.AddSeconds(request.IntervalSeconds),

            Urls = request.Urls
                .Select(url => new AutomationRuleUrl
                {
                    Id = Guid.NewGuid(),
                    Url = url.Trim()
                })
                .ToList()
        };

        _dbContext.Set<AutomationRule>().Add(rule);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _scheduleNotifier.NotifyChangedAsync(cancellationToken);

        return rule;
    }

    public async Task<(StopAutomationResult Result, AutomationRule? Rule)>
        StopAsync(
            Guid id,
            CancellationToken cancellationToken)
    {
        var rule = await _dbContext
            .Set<AutomationRule>()
            .FirstOrDefaultAsync(
                rule => rule.Id == id,
                cancellationToken);

        if (rule is null)
            return (StopAutomationResult.NotFound, null);

        if (!rule.IsEnabled)
            return (StopAutomationResult.AlreadyStopped, rule);

        rule.IsEnabled = false;
        rule.NextRunAt = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _scheduleNotifier.NotifyChangedAsync(cancellationToken);

        return (StopAutomationResult.Stopped, rule);
    }

    /// <returns>
    /// null — правило не найдено; иначе — список прогонов (возможно пустой).
    /// </returns>
    public async Task<IReadOnlyList<AutomationRuleRunResponse>?> GetRunsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ruleExists = await _dbContext
            .Set<AutomationRule>()
            .AnyAsync(
                rule => rule.Id == id,
                cancellationToken);

        if (!ruleExists)
            return null;

        var jobs = await _dbContext.Jobs
            .AsNoTracking()
            .Where(job => job.AutomationRuleId == id)
            .Include(job => job.Results)
            .OrderByDescending(job => job.CreatedAt)
            .ToListAsync(cancellationToken);

        return jobs.Select(job => new AutomationRuleRunResponse
        {
            Id = job.Id,
            Status = job.Status.ToString(),
            Type = job.Type.ToString(),
            CreatedAt = job.CreatedAt,
            ExecutionTimeMs =
                job.CompletedAt.HasValue && job.StartedAt.HasValue
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
                }).ToList()
        }).ToList();
    }
}