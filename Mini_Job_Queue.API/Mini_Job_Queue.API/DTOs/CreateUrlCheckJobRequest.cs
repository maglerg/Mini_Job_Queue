namespace Mini_Job_Queue.API.DTOs
{
    public class CreateUrlCheckJobRequest
    {
        public List<string> Urls { get; set; } = new();
    }
}
