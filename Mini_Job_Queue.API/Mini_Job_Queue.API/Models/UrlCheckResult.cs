namespace Mini_Job_Queue.API.Models
{
    public class UrlCheckResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid JobId { get; set; }

        public string Url { get; set; } = string.Empty;

        public int? HttpStatusCode { get; set; }

        public long? ResponseTimeMs { get; set; }

        public string? Error { get; set; }

        public Job Job { get; set; } = null!;
    }
}
