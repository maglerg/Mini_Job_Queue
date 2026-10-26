using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.Workers;

namespace Mini_Job_Queue.API.Services;

public sealed class AutomationScheduleNotifier
{
    private readonly AppDbContext _dbContext;
    private readonly AutomationScheduleSignal _signal;
    private readonly ILogger<AutomationScheduleNotifier> _logger;

    public AutomationScheduleNotifier(
        AppDbContext dbContext,
        AutomationScheduleSignal signal,
        ILogger<AutomationScheduleNotifier> logger)
    {
        _dbContext = dbContext;
        _signal = signal;
        _logger = logger;
    }

    public async Task NotifyChangedAsync(
        CancellationToken cancellationToken)
    {
        // Сразу будим планировщик в этом экземпляре приложения.
        _signal.NotifyScheduleChanged();

        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "NOTIFY automation_changed, 'changed';",
                cancellationToken);
        }
        catch (Exception ex)
            when (ex is not OperationCanceledException)
        {
            // Изменение уже могло сохраниться.
            // Не возвращаем клиенту ошибку только из-за уведомления.
            _logger.LogWarning(
                ex,
                "Не удалось отправить уведомление об изменении расписания.");
        }
    }
}