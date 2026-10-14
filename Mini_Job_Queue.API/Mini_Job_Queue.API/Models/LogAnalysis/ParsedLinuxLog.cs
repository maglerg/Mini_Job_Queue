namespace Mini_Job_Queue.API.Models.LogAnalysis
{
    public class ParsedLinuxLog
    {
        public string Timestamp { get; set; } = string.Empty;

        public string Host { get; set; } = string.Empty;

        public string Service { get; set; } = string.Empty;

        public int? ProcessId { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? RemoteHost { get; set; }
    }
}