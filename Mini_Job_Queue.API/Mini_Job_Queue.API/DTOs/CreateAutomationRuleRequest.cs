
using Mini_Job_Queue.API.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Mini_Job_Queue.API.DTOs
{
    public class CreateAutomationRuleRequest
    {
        [Required]
        public AutomationType AutType { get; set; } = AutomationType.UrlCheck;

        [Range(1, 86400)]
        public int IntervalSeconds { get; set; }

        [Required]
        [MinLength(1)]
        public List<string> Urls { get; set; } = new();
    }
}
