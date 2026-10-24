
namespace Mini_Job_Queue.API.DTOs.Weather;

public class WeatherRecordResponse
{
    public Guid Id { get; set; }

    public double TemperatureCelsius { get; set; }

    public int? RelativeHumidityPercent { get; set; }

    public double? PrecipitationMm { get; set; }

    public double? WindSpeedKmh { get; set; }

    public DateTime RecordedAt { get; set; }
}
