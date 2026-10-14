
namespace Mini_Job_Queue.API.DTOs.LogAnalysis
{
    public class LogAnalysisSummary
    {
        public int TotalLines { get; set; }

        public int ParsedLines { get; set; }

        public int InvalidLines { get; set; }

        public int InformationCount { get; set; }

        public int WarningCount { get; set; }

        public int ErrorCount { get; set; }

        public List<TopLogMessage> TopMessages { get; set; } = new();
    }

    public class TopLogMessage
    {
        public string Message { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}
