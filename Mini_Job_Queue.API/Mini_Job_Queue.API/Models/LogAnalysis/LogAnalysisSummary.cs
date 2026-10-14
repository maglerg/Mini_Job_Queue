using Mini_Job_Queue.API.Models.Enums;

namespace Mini_Job_Queue.API.Models.LogAnalysis
{
    public class LogAnalysisSummary
    {
        public int TotalLines { get; set; }
        public int ParsedLines { get; set; }
        public int InvalidLines { get; set; }

        public Dictionary<LinuxLogCategory, int> CategoryCounts { get; set; }
            = new();

        public Dictionary<string, int> TopMessages { get; set; }
            = new();

        public Dictionary<string, int> FailedAuthenticationByHost { get; set; }
            = new();
    }
}