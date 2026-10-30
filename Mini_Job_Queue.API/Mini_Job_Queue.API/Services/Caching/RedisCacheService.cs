using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Mini_Job_Queue.API.Services.Caching;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly TimeSpan CacheExpiration =
        TimeSpan.FromMinutes(3);

    public RedisCacheService(
        IDistributedCache cache,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        var cachedData = await _cache.GetStringAsync(
            key,
            cancellationToken);

        if (cachedData is null)
        {
            _logger.LogDebug(
                "Cache MISS for key {CacheKey}",
                key);

            return default;
        }

        _logger.LogDebug(
            "Cache HIT for key {CacheKey}",
            key);

        return JsonSerializer.Deserialize<T>(cachedData);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        var serializedData = JsonSerializer.Serialize(value);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration
        };

        await _cache.SetStringAsync(
            key,
            serializedData,
            options,
            cancellationToken);
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(
            key,
            cancellationToken);
    }
}