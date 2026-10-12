namespace Mini_Job_Queue.API.DTOs
{
    public class UrlCheckResultResponse
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;

        public int? HttpStatusCode { get; set; }

        public long? ResponseTimeMs { get; set; }

        public string? Error { get; set; }
    }
}
