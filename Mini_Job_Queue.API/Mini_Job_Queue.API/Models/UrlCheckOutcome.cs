
namespace Mini_Job_Queue.API.Models;

public class UrlCheckOutcome
{
    public int? HttpStatusCode { get; set; }

    public long? ResponseTimeMs { get; set; }

    public string? Error { get; set; }
}
