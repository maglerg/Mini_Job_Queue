
namespace Mini_Job_Queue.API.Models
{
    public class LogAnalysisInput
    {

        public Guid JobId { get; set; }

        public string SourceText { get; set; } = string.Empty;

        public Job? Job { get; set; }
    }
}
