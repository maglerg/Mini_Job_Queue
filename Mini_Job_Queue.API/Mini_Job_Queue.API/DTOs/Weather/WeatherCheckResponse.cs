namespace Mini_Job_Queue.API.DTOs.Weather;

public class WeatherCheckResponse
{
    public string LocationName { get; set; } = string.Empty;
    public double TemperatureCelsius { get; set; }
    public int? RelativeHumidityPercent { get; set; }
    public double? PrecipitationMm { get; set; }
    public double? WindSpeedKmh { get; set; }
    public DateTime RetrievedAt { get; set; }
}