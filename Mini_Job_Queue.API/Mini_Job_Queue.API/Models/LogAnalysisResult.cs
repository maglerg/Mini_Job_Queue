
namespace Mini_Job_Queue.API.Models
{
    public class LogAnalysisResult
    {
        public Guid JobId { get; set; }

        public int TotalLines { get; set; }

        public int ParsedLines { get; set; }

        public int InvalidLines { get; set; }

        public string CategoryCountsJson { get; set; } = "{}";
        public string FailedAuthenticationByHostJson { get; set; } = "{}";
        public string TopMessagesJson { get; set; } = "{}";

        public Job? Job { get; set; }
    }
}
