using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.DTOs;
using Mini_Job_Queue.API.Services;

namespace Mini_Job_Queue.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly JobsService _jobsService;

        public JobsController(AppDbContext dbContext, JobsService jobsService)
        {
            _dbContext = dbContext;
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
            var job = await _dbContext.Jobs
                .AsNoTracking()
                .Include(j => j.Results)
                .FirstOrDefaultAsync(
                    j => j.Id == id,
                    cancellationToken);

            if (job is null)
            {
                return NotFound();
            }

            var response = new JobDetailsResponse
            {
                Id = job.Id,
                Status = job.Status.ToString(),
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ExecutionTimeMs =
    job.StartedAt.HasValue && job.CompletedAt.HasValue
        ? (long)(job.CompletedAt.Value - job.StartedAt.Value)
            .TotalMilliseconds
        : null,
                Results = job.Results.Select(result =>
                    new UrlCheckResultResponse
                    {
                        Id = result.Id,
                        Url = result.Url,
                        HttpStatusCode = result.HttpStatusCode,
                        ResponseTimeMs = result.ResponseTimeMs,
                        Error = result.Error
                    }).ToList()
            };

            return Ok(response);
        }
    }
}