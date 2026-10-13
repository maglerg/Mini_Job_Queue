
namespace Mini_Job_Queue.API.DTOs;

public class JobDetailsResponse
{
    public Guid Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
    public long? ExecutionTimeMs { get; set; }

    public List<UrlCheckResultResponse> Results { get; set; } = [];
}
