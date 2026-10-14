
using System.ComponentModel.DataAnnotations;

namespace Mini_Job_Queue.API.DTOs
{
    public class CreateLogAnalysisJobRequest
    {
        [Required]
        [MaxLength(500_000)]
        public string SourceText { get; set; } = string.Empty;
    }
}
