
namespace Mini_Job_Queue.API.Models;

public class WeatherRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WeatherTrackingId { get; set; }

    public double TemperatureCelsius { get; set; }

    public int? RelativeHumidityPercent { get; set; }

    public double? PrecipitationMm { get; set; }

    public double? WindSpeedKmh { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public WeatherTracking WeatherTracking { get; set; } = null!;
}
