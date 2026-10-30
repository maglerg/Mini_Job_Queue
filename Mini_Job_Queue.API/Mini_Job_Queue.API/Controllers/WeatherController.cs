
using Microsoft.AspNetCore.Mvc;
using Mini_Job_Queue.API.DTOs.Weather;
using Mini_Job_Queue.API.Services;

namespace Mini_Job_Queue.API.Controllers;

[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly WeatherService _weatherService;

    public WeatherController(WeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    // Создать отслеживание погоды.
    [HttpPost("trackings")]
    public async Task<ActionResult<WeatherTrackingResponse>>
        CreateTracking(
            [FromBody] CreateWeatherTrackingRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var tracking = await _weatherService.CreateTrackingAsync(
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetHistory),
                new { id = tracking.Id },
                tracking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "Не удалось получить данные геокодирования." });
        }
    }

    // Получить все отслеживания.
    [HttpGet("trackings")]
    public async Task<ActionResult<List<WeatherTrackingResponse>>>
        GetTrackings(CancellationToken cancellationToken)
    {
        var trackings = await _weatherService.GetTrackingsAsync(
            cancellationToken);

        return Ok(trackings);
    }

    // Остановить отслеживание.
    [HttpPost("trackings/{id:guid}/stop")]
    public async Task<IActionResult> StopTracking(
        Guid id,
        CancellationToken cancellationToken)
    {
        var stopped = await _weatherService.StopTrackingAsync(
            id,
            cancellationToken);

        if (!stopped)
        {
            return NotFound(new
            {
                message = $"Отслеживание {id} не найдено."
            });
        }

        return NoContent();
    }
    // Получить текущую погоду с использованием Redis.
    [HttpGet("trackings/{id:guid}/current")]
    public async Task<ActionResult<CurrentWeatherResponse>>
        GetCurrentWeather(
            Guid id,
            CancellationToken cancellationToken)
    {
        try
        {
            var weather = await _weatherService.GetCurrentWeatherAsync(
                id,
                cancellationToken);

            return Ok(weather);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "Не удалось получить данные Open-Meteo." });
        }
    }
    // Разовая проверка погоды без создания отслеживания.
    [HttpPost("check")]
    public async Task<ActionResult<WeatherCheckResponse>> CheckWeather(
        [FromBody] WeatherCheckRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var weather = await _weatherService.CheckWeatherAsync(
                request.LocationName,
                cancellationToken);

            return Ok(weather);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "Не удалось получить данные Open-Meteo." });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "Open-Meteo не вернул погодные данные." });
        }
    }
    // Получить историю замеров конкретного города.
    [HttpGet("trackings/{id:guid}/history")]
    public async Task<ActionResult<List<WeatherRecordResponse>>>
        GetHistory(
            Guid id,
            CancellationToken cancellationToken)
    {
        try
        {
            var history = await _weatherService.GetHistoryAsync(
                id,
                cancellationToken);

            return Ok(history);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
