
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.DTOs.Weather;
using Mini_Job_Queue.API.Models;
using Mini_Job_Queue.API.Services.Caching;

namespace Mini_Job_Queue.API.Services;

public class WeatherService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherService> _logger;
    private readonly AutomationScheduleNotifier _scheduleNotifier;
    private readonly IRedisCacheService _redisCache;

    public WeatherService(
        AppDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<WeatherService> logger,
        AutomationScheduleNotifier scheduleNotifier,
        IRedisCacheService redisCache)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _scheduleNotifier = scheduleNotifier;
        _redisCache = redisCache;
    }

    // Создание отслеживания погоды.
    public async Task<WeatherTrackingResponse> CreateTrackingAsync(
        CreateWeatherTrackingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LocationName))
        {
            throw new ArgumentException(
                "Укажи название города.");
        }

        if (request.IntervalMinutes < 1)
        {
            throw new ArgumentException(
                "Интервал должен быть не меньше одной минуты.");
        }

        var cityName = request.LocationName.Trim();

        var httpClient = _httpClientFactory.CreateClient();

        var geocodingUrl =
            "https://geocoding-api.open-meteo.com/v1/search" +
            $"?name={Uri.EscapeDataString(cityName)}" +
            "&count=1&language=en&format=json";

        using var response = await httpClient.GetAsync(
            geocodingUrl,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var geocoding = await response.Content
            .ReadFromJsonAsync<GeocodingResponse>(
                cancellationToken);

        var location = geocoding?.Results?.FirstOrDefault();

        if (location is null)
        {
            throw new ArgumentException(
                $"Не удалось найти город «{cityName}».");
        }

        var now = DateTime.UtcNow;

        var tracking = new WeatherTracking
        {
            Id = Guid.NewGuid(),
            LocationName = location.Name,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            IntervalMinutes = request.IntervalMinutes,
            IsEnabled = true,
            CreatedAt = now,
            NextRunAt = now
        };

        _dbContext.WeatherTrackings.Add(tracking);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _scheduleNotifier.NotifyChangedAsync(cancellationToken);

        _logger.LogInformation(
            "Создано отслеживание {TrackingId} для города {LocationName}.",
            tracking.Id,
            tracking.LocationName);

        return MapTracking(tracking);
    }

    // Получение текущей погоды с использованием Redis.
    // Получение текущей погоды для существующего отслеживания.
    public async Task<CurrentWeatherResponse> GetCurrentWeatherAsync(
        Guid trackingId,
        CancellationToken cancellationToken = default)
    {
        var tracking = await _dbContext.WeatherTrackings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == trackingId,
                cancellationToken);

        if (tracking is null)
        {
            throw new KeyNotFoundException(
                $"Отслеживание {trackingId} не найдено.");
        }

        return await GetWeatherWithCacheAsync(
            tracking.Latitude,
            tracking.Longitude,
            tracking.LocationName,
            cancellationToken);
    }
    // Общий механизм получения погоды через Redis.
    private async Task<CurrentWeatherResponse> GetWeatherWithCacheAsync(
        double latitude,
        double longitude,
        string locationName,
        CancellationToken cancellationToken)
    {
        var latitudeString = latitude.ToString(
            CultureInfo.InvariantCulture);

        var longitudeString = longitude.ToString(
            CultureInfo.InvariantCulture);

        // Формируем единый ключ Redis для обоих сценариев.
        var cacheKey = $"weather:current:{latitudeString}:{longitudeString}";

        // Проверяем наличие данных в Redis.
        var cachedWeather = await _redisCache.GetAsync<CurrentWeatherResponse>(
            cacheKey,
            cancellationToken);

        if (cachedWeather is not null)
        {
            _logger.LogInformation(
                "Cache HIT для города {LocationName}.",
                locationName);

            return cachedWeather;
        }

        _logger.LogInformation(
            "Cache MISS для города {LocationName}.",
            locationName);

        // Если данных нет, получаем их из Open-Meteo.
        var current = await FetchCurrentWeatherAsync(
            latitude,
            longitude,
            cancellationToken);

        var result = new CurrentWeatherResponse
        {
            TemperatureCelsius = current.TemperatureCelsius,
            RelativeHumidityPercent = current.RelativeHumidityPercent,
            PrecipitationMm = current.PrecipitationMm,
            WindSpeedKmh = current.WindSpeedKmh,
            RetrievedAt = DateTime.UtcNow
        };

        // Сохраняем результат в Redis на 3 минуты.
        await _redisCache.SetAsync(
            cacheKey,
            result,
            cancellationToken);

        return result;
    }
    // Получение всех отслеживаний.
    public async Task<List<WeatherTrackingResponse>> GetTrackingsAsync(
        CancellationToken cancellationToken = default)
    {
        var trackings = await _dbContext.WeatherTrackings
            .AsNoTracking()
            .OrderBy(tracking => tracking.LocationName)
            .ToListAsync(cancellationToken);

        return trackings
            .Select(MapTracking)
            .ToList();
    }

    // Остановка отслеживания.
    public async Task<bool> StopTrackingAsync(
        Guid trackingId,
        CancellationToken cancellationToken = default)
    {
        var tracking = await _dbContext.WeatherTrackings
            .FirstOrDefaultAsync(
                item => item.Id == trackingId,
                cancellationToken);

        if (tracking is null)
        {
            return false;
        }

        tracking.IsEnabled = false;
        tracking.NextRunAt = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _scheduleNotifier.NotifyChangedAsync(cancellationToken);

        _logger.LogInformation(
            "Отслеживание погоды {TrackingId} остановлено.",
            trackingId);

        return true;
    }

    // Получение истории измерений из PostgreSQL.
    public async Task<List<WeatherRecordResponse>> GetHistoryAsync(
        Guid trackingId,
        CancellationToken cancellationToken = default)
    {
        var trackingExists = await _dbContext.WeatherTrackings
            .AnyAsync(
                tracking => tracking.Id == trackingId,
                cancellationToken);

        if (!trackingExists)
        {
            throw new KeyNotFoundException(
                $"Отслеживание {trackingId} не найдено.");
        }

        var records = await _dbContext.WeatherRecords
            .AsNoTracking()
            .Where(record =>
                record.WeatherTrackingId == trackingId)
            .OrderByDescending(record => record.RecordedAt)
            .ToListAsync(cancellationToken);

        return records.Select(record => new WeatherRecordResponse
        {
            Id = record.Id,
            TemperatureCelsius = record.TemperatureCelsius,
            RelativeHumidityPercent =
                record.RelativeHumidityPercent,
            PrecipitationMm = record.PrecipitationMm,
            WindSpeedKmh = record.WindSpeedKmh,
            RecordedAt = record.RecordedAt
        }).ToList();
    }

    // Выполнение автоматической задачи сбора погоды из JobWorker.
    public async Task CollectWeatherAsync(
        Guid trackingId,
        CancellationToken cancellationToken)
    {
        var tracking = await _dbContext.WeatherTrackings
            .FirstOrDefaultAsync(
                item => item.Id == trackingId,
                cancellationToken);

        if (tracking is null)
        {
            throw new InvalidOperationException(
                $"Отслеживание погоды {trackingId} не найдено.");
        }

        if (!tracking.IsEnabled)
        {
            _logger.LogInformation(
                "Отслеживание {TrackingId} отключено. Замер пропущен.",
                trackingId);

            return;
        }

        // Получаем свежие данные непосредственно из Open-Meteo.
        // Здесь Redis не используется, чтобы не дублировать
        // старые данные в истории измерений.
        var current = await FetchCurrentWeatherAsync(
            tracking.Latitude,
            tracking.Longitude,
            cancellationToken);

        var record = new WeatherRecord
        {
            Id = Guid.NewGuid(),
            WeatherTrackingId = tracking.Id,
            TemperatureCelsius = current.TemperatureCelsius,
            RelativeHumidityPercent =
                current.RelativeHumidityPercent,
            PrecipitationMm = current.PrecipitationMm,
            WindSpeedKmh = current.WindSpeedKmh,
            RecordedAt = DateTime.UtcNow
        };

        _dbContext.WeatherRecords.Add(record);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Погода сохранена для {LocationName}. " +
            "Температура: {Temperature} °C.",
            tracking.LocationName,
            record.TemperatureCelsius);
    }
    // Разовая проверка погоды по названию города.
    public async Task<WeatherCheckResponse> CheckWeatherAsync(
        string locationName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            throw new ArgumentException("Укажи название города.");
        }

        var cityName = locationName.Trim();

        var httpClient = _httpClientFactory.CreateClient();

        // Получаем координаты города через геокодирование.
        var geocodingUrl =
            "https://geocoding-api.open-meteo.com/v1/search" +
            $"?name={Uri.EscapeDataString(cityName)}" +
            "&count=1&language=en&format=json";

        using var response = await httpClient.GetAsync(
            geocodingUrl,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var geocoding = await response.Content
            .ReadFromJsonAsync<GeocodingResponse>(
                cancellationToken);

        var location = geocoding?.Results?.FirstOrDefault();

        if (location is null)
        {
            throw new ArgumentException(
                $"Не удалось найти город «{cityName}».");
        }

        // Получаем погоду через общий механизм кэширования.
        var weather = await GetWeatherWithCacheAsync(
            location.Latitude,
            location.Longitude,
            location.Name,
            cancellationToken);

        return new WeatherCheckResponse
        {
            LocationName = location.Name,
            TemperatureCelsius = weather.TemperatureCelsius,
            RelativeHumidityPercent = weather.RelativeHumidityPercent,
            PrecipitationMm = weather.PrecipitationMm,
            WindSpeedKmh = weather.WindSpeedKmh,
            RetrievedAt = weather.RetrievedAt
        };
    }
    // Общий метод получения текущей погоды из Open-Meteo.
    private async Task<CurrentWeather> FetchCurrentWeatherAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var latitudeString = latitude.ToString(
            CultureInfo.InvariantCulture);

        var longitudeString = longitude.ToString(
            CultureInfo.InvariantCulture);

        var weatherUrl =
            "https://api.open-meteo.com/v1/forecast" +
            $"?latitude={latitudeString}" +
            $"&longitude={longitudeString}" +
            "&current=temperature_2m,relative_humidity_2m,precipitation,wind_speed_10m";

        var httpClient = _httpClientFactory.CreateClient();

        using var response = await httpClient.GetAsync(
            weatherUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken);

        var weather = await JsonSerializer
            .DeserializeAsync<OpenMeteoResponse>(
                stream,
                cancellationToken: cancellationToken);

        if (weather?.Current is null)
        {
            throw new InvalidOperationException(
                "Open-Meteo не вернул текущие погодные данные.");
        }

        return weather.Current;
    }

    // Преобразование модели отслеживания в DTO.
    private static WeatherTrackingResponse MapTracking(
        WeatherTracking tracking)
    {
        return new WeatherTrackingResponse
        {
            Id = tracking.Id,
            LocationName = tracking.LocationName,
            IntervalMinutes = tracking.IntervalMinutes,
            IsEnabled = tracking.IsEnabled,
            CreatedAt = tracking.CreatedAt,
            LastRunAt = tracking.LastRunAt,
            NextRunAt = tracking.NextRunAt
        };
    }

    // DTO ответа геокодирования Open-Meteo.
    private sealed class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodingLocation>? Results { get; set; }
    }

    private sealed class GeocodingLocation
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    // DTO ответа Open-Meteo с текущей погодой.
    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")]
        public CurrentWeather? Current { get; set; }
    }

    private sealed class CurrentWeather
    {
        [JsonPropertyName("temperature_2m")]
        public double TemperatureCelsius { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public int? RelativeHumidityPercent { get; set; }

        [JsonPropertyName("precipitation")]
        public double? PrecipitationMm { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public double? WindSpeedKmh { get; set; }
    }
}
