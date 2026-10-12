using Mini_Job_Queue.API.Models.Enums;
namespace Mini_Job_Queue.API.Models
{
    public class Job
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public JobStatus Status { get; set; } = JobStatus.Queued;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public List<UrlCheckResult> Results { get; set; } = new();
    }
}
