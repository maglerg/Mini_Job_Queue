using Npgsql;

namespace Mini_Job_Queue.API.Workers;

public sealed class PostgresScheduleNotificationListener
    : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly AutomationScheduleSignal _signal;
    private readonly ILogger<PostgresScheduleNotificationListener> _logger;

    public PostgresScheduleNotificationListener(
        IConfiguration configuration,
        AutomationScheduleSignal signal,
        ILogger<PostgresScheduleNotificationListener> logger)
    {
        _configuration = configuration;
        _signal = signal;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Не найдена строка подключения DefaultConnection.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenAsync(
                    connectionString,
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
                    "Ошибка слушателя уведомлений PostgreSQL. " +
                    "Повторное подключение через 5 секунд.");

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        _logger.LogInformation(
            "Слушатель уведомлений PostgreSQL остановлен.");
    }

    private async Task ListenAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        connection.Notification += (_, notification) =>
        {
            _logger.LogInformation(
                "Получено уведомление PostgreSQL: {Channel}.",
                notification.Channel);

            _signal.NotifyScheduleChanged();
        };

        await using (var command = new NpgsqlCommand(
            "LISTEN automation_changed;",
            connection))
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Подписка PostgreSQL на automation_changed активна.");

        // После подключения обязательно перечитываем расписания.
        // Это помогает восстановиться после потери уведомлений
        // во время отключения от PostgreSQL.
        _signal.NotifyScheduleChanged();

        while (!cancellationToken.IsCancellationRequested)
        {
            await connection.WaitAsync(cancellationToken);
        }
    }
}