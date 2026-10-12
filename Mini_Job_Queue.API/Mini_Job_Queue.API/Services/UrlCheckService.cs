
using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Mini_Job_Queue.API.Services;

public class UrlCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UrlCheckService> _logger;

    public UrlCheckService(
        IHttpClientFactory httpClientFactory,
        ILogger<UrlCheckService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task CheckUrlAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await client.GetAsync(
                url,
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "URL: {Url}; Status: {StatusCode}; Time: {ElapsedMs} ms",
                url,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "URL: {Url}; Error: {Error}; Time: {ElapsedMs} ms",
                url,
                ex.Message,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
