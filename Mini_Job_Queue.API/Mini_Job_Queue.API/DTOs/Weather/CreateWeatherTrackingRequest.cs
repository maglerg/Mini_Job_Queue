
namespace Mini_Job_Queue.API.DTOs.Weather;

public class CreateWeatherTrackingRequest
{
    public string LocationName { get; set; } = string.Empty;

    public int IntervalMinutes { get; set; } = 30;
}
