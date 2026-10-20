using Microsoft.AspNetCore.Mvc;
using Mini_Job_Queue.API.DTOs;
using Mini_Job_Queue.API.Services;

namespace Mini_Job_Queue.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly JobsService _jobsService;

        public JobsController(JobsService jobsService)
        {
            _jobsService = jobsService;
        }

        // создать задачу
        [HttpPost]
        public async Task<ActionResult<object>> Create(
            CreateUrlCheckJobRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Urls is null || request.Urls.Count == 0)
            {
                return BadRequest(new
                {
                    error = "Необходимо указать хотя бы один URL."
                });
            }

            var job = await _jobsService.CreateJobAsync(
                request,
                cancellationToken);

            return Accepted(new
            {
                jobId = job.Id,
                status = job.Status.ToString()
            });
        }

        // найти задачу по айди и вернуть результат проверки
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<JobDetailsResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var details = await _jobsService.GetJobDetailsAsync(
                id,
                cancellationToken);

            if (details is null)
                return NotFound();

            return Ok(details);
        }

        [HttpPost("log-analysis")]
        public async Task<ActionResult<object>> CreateLogAnalysis(
            CreateLogAnalysisJobRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SourceText))
            {
                return BadRequest(new
                {
                    error = "Необходимо передать текст логов."
                });
            }

            var job = await _jobsService.CreateLogAnalysisJobAsync(
                request,
                cancellationToken);

            return Accepted(new
            {
                jobId = job.Id,
                status = job.Status.ToString()
            });
        }
    }
}