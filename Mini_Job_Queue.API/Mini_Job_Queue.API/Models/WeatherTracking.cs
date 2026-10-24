
namespace Mini_Job_Queue.API.Models;

public class WeatherTracking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string LocationName { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int IntervalMinutes { get; set; } = 30;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastRunAt { get; set; }

    public DateTime? NextRunAt { get; set; }

    public List<WeatherRecord> Records { get; set; } = new();
    public List<Job> Jobs { get; set; } = new();
}
