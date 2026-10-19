
using Microsoft.AspNetCore.Mvc;
using Mini_Job_Queue.API.Context;
using Mini_Job_Queue.API.DTOs;
using Mini_Job_Queue.API.Models;

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
    }
}
