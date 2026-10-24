
namespace Mini_Job_Queue.API.DTOs.Weather;

public class WeatherTrackingResponse
{
    public Guid Id { get; set; }

    public string LocationName { get; set; } = string.Empty;

    public int IntervalMinutes { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastRunAt { get; set; }

    public DateTime? NextRunAt { get; set; }
}
