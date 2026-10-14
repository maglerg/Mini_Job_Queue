
namespace Mini_Job_Queue.API.DTOs;

public class LogAnalysisResultResponse
{
    public int TotalLines { get; set; }

    public int ParsedLines { get; set; }

    public int InvalidLines { get; set; }

    public Dictionary<string, int> CategoryCounts { get; set; } = [];

    public Dictionary<string, int> TopMessages { get; set; } = [];

    public Dictionary<string, int> FailedAuthenticationByHost { get; set; } = [];
}
