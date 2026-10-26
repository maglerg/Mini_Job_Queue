namespace Mini_Job_Queue.API.Workers;

public sealed class AutomationScheduleSignal
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    public void NotifyScheduleChanged()
    {
        // Если сигнал уже ожидает обработки,
        // второй такой же сигнал не нужен.
        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Сигнал уже есть.
        }
    }

    public async Task<bool> WaitForChangeAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return await _semaphore.WaitAsync(
            timeout,
            cancellationToken);
    }
}