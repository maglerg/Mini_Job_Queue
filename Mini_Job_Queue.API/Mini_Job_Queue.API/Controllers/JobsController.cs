using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.DTOs;
using Mini_Job_Queue.API.Models;
using Mini_Job_Queue.API.Models.Enums;
using Mini_Job_Queue.API.Queue;
using Mini_Job_Queue.API.Services;
using System.Text.Json;

namespace Mini_Job_Queue.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly JobsService _jobsService;
        private readonly IJobQueue _jobQueue;

        public JobsController(
    AppDbContext dbContext,
    JobsService jobsService,
    IJobQueue jobQueue)
        {
            _dbContext = dbContext;
            _jobsService = jobsService;
            _jobQueue = jobQueue;
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
                .Include(j => j.LogAnalysisResult)
                .FirstOrDefaultAsync(
                    j => j.Id == id,
                    cancellationToken);

            if (job is null)
            {
                return NotFound();
            }

            LogAnalysisResultResponse? logResult = null;

            if (job.LogAnalysisResult is not null)
            {
                var savedResult = job.LogAnalysisResult;

                logResult = new LogAnalysisResultResponse
                {
                    TotalLines = savedResult.TotalLines,
                    ParsedLines = savedResult.ParsedLines,
                    InvalidLines = savedResult.InvalidLines,

                    CategoryCounts = DeserializeDictionary(
                        savedResult.CategoryCountsJson),

                    TopMessages = DeserializeDictionary(
                        savedResult.TopMessagesJson),

                    FailedAuthenticationByHost = DeserializeDictionary(
                        savedResult.FailedAuthenticationByHostJson)
                };
            }

            return Ok(new JobDetailsResponse
            {
                Id = job.Id,
                Type = job.Type.ToString(),
                Status = job.Status.ToString(),
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,

                ExecutionTimeMs =
                    job.StartedAt.HasValue && job.CompletedAt.HasValue
                        ? (long)(
                            job.CompletedAt.Value - job.StartedAt.Value)
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
                    }).ToList(),

                LogAnalysisResult = logResult
            });
        }

        private static Dictionary<string, int> DeserializeDictionary(
            string json)
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                ?? new Dictionary<string, int>();
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

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            var job = new Job
            {
                Type = JobType.LogAnalysis,
                Status = JobStatus.Queued,
                CreatedAt = DateTime.UtcNow,
                LogAnalysisInput = new LogAnalysisInput
                {
                    SourceText = request.SourceText
                }
            };

            _dbContext.Jobs.Add(job);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _jobQueue.EnqueueAsync(
                job.Id,
                cancellationToken);

            return Accepted(new
            {
                jobId = job.Id,
                status = job.Status.ToString()
            });
        }

    }
}