namespace Mini_Job_Queue.API.DTOs;

public class AutomationRuleRunResponse
{
    public Guid Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public long? ExecutionTimeMs { get; set; }

    public List<UrlCheckResultResponse> Results { get; set; } = [];
}