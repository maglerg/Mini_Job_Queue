using Mini_Job_Queue.API.Models.Enums;

namespace Mini_Job_Queue.API.DTOs
{
    public class JobStatusResponse
    {
        public Guid Id { get; set; }

        public JobStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public List<UrlCheckResultResponse> Results { get; set; } = new();
    }
}
