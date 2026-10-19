namespace Mini_Job_Queue.API.Models
{
    public class AutomationRuleUrl
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AutomationRuleId { get; set; }

        public string Url { get; set; } = string.Empty;

        public AutomationRule AutomationRule { get; set; } = null!;
    }
}