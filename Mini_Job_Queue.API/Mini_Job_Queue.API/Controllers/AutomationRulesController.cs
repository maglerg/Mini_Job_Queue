
using Microsoft.AspNetCore.Mvc;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.DTOs;
using Mini_Job_Queue.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Mini_Job_Queue.API.Controllers
{
    [ApiController]
    [Route("api/automation-rules")]
    public class AutomationRulesController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public AutomationRulesController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<ActionResult<object>> Create(
            CreateAutomationRuleRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Urls.Any(url =>
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps)))
            {
                return BadRequest(new
                {
                    error = "Все URL должны быть корректными адресами HTTP или HTTPS."
                });
            }

            var now = DateTime.UtcNow;

            var rule = new AutomationRule
            {
                Id = Guid.NewGuid(),
                AutType = request.AutType,
                IntervalSeconds = request.IntervalSeconds,
                IsEnabled = true,
                CreatedAt = now,
                LastRunAt = null,

                // Первый запуск — через заданный интервал.
                NextRunAt = now.AddSeconds(
                    request.IntervalSeconds),

                Urls = request.Urls
                    .Select(url => new AutomationRuleUrl
                    {
                        Id = Guid.NewGuid(),
                        Url = url.Trim()
                    })
                    .ToList()
            };

            _dbContext.Set<AutomationRule>().Add(rule);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return Accepted(new
            {
                ruleId = rule.Id,
                type = rule.AutType.ToString(),
                intervalSeconds = rule.IntervalSeconds,
                isEnabled = rule.IsEnabled,
                nextRunAt = rule.NextRunAt
            });
        }
        // Остановить автоматизацию.
        [HttpPost("{id:guid}/stop")]
        public async Task<ActionResult<object>> Stop(
            Guid id,
            CancellationToken cancellationToken)
        {
            var rule = await _dbContext
                .Set<AutomationRule>()
                .FirstOrDefaultAsync(
                    rule => rule.Id == id,
                    cancellationToken);

            if (rule == null)
            {
                return NotFound(new
                {
                    error = "Автоматизация не найдена."
                });
            }

            if (!rule.IsEnabled)
            {
                return Ok(new
                {
                    ruleId = rule.Id,
                    isEnabled = false,
                    message = "Автоматизация уже остановлена."
                });
            }

            rule.IsEnabled = false;
            rule.NextRunAt = null;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                ruleId = rule.Id,
                isEnabled = rule.IsEnabled,
                message = "Автоматизация остановлена."
            });
        }


        // Получить историю запусков автоматизации.
        [HttpGet("{id:guid}/runs")]
        public async Task<ActionResult<object>> GetRuns(
            Guid id,
            CancellationToken cancellationToken)
        {
            var ruleExists = await _dbContext
                .Set<AutomationRule>()
                .AnyAsync(
                    rule => rule.Id == id,
                    cancellationToken);

            if (!ruleExists)
            {
                return NotFound(new
                {
                    error = "Автоматизация не найдена."
                });
            }

            var jobs = await _dbContext.Jobs
                .AsNoTracking()
                .Where(job => job.AutomationRuleId == id)
                .Include(job => job.Results)
                .OrderByDescending(job => job.CreatedAt)
                .ToListAsync(cancellationToken);

            var runs = jobs.Select(job => new
            {
                id = job.Id,
                status = job.Status.ToString(),
                type = job.Type.ToString(),
                createdAt = job.CreatedAt,
                executionTimeMs = (job.CompletedAt.HasValue && job.StartedAt.HasValue)
    ? (long)(job.CompletedAt.Value - job.StartedAt.Value).TotalMilliseconds
    : (long?)null,

                results = job.Results.Select(result => new
                {
                    id = result.Id,
                    url = result.Url,
                    httpStatusCode = result.HttpStatusCode,
                    responseTimeMs = result.ResponseTimeMs,
                    error = result.Error
                })
            });

            return Ok(runs);
        }
    }
}
