
using System.Diagnostics;
using System.Net.Http;
using Mini_Job_Queue.API.Models;

namespace Mini_Job_Queue.API.Services;

public class UrlCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UrlCheckService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<UrlCheckOutcome> CheckUrlAsync(
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

            return new UrlCheckOutcome
            {
                HttpStatusCode = (int)response.StatusCode,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Error = null
            };
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

            return new UrlCheckOutcome
            {
                HttpStatusCode = null,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }
}
