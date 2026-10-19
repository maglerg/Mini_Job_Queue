using Mini_Job_Queue.API.Models.Enums;

namespace Mini_Job_Queue.API.Models
{
    public class AutomationRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public AutomationType AutType { get; set; } = AutomationType.UrlCheck;

        public JobType Type { get; set; } = JobType.UrlCheck;

        public int IntervalSeconds { get; set; } = 10;

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastRunAt { get; set; }

        public DateTime? NextRunAt { get; set; }

        public List<AutomationRuleUrl> Urls { get; set; } = new();

        public List<Job> Jobs { get; set; } = new();
    }
}