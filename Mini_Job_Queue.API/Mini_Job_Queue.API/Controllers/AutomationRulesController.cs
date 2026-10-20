using Microsoft.AspNetCore.Mvc;
using Mini_Job_Queue.API.DTOs;
using Mini_Job_Queue.API.Services;

namespace Mini_Job_Queue.API.Controllers
{
    [ApiController]
    [Route("api/automation-rules")]
    public class AutomationRulesController : ControllerBase
    {
        private readonly AutomationRulesService _automationRulesService;

        public AutomationRulesController(
            AutomationRulesService automationRulesService)
        {
            _automationRulesService = automationRulesService;
        }

        [HttpPost]
        public async Task<ActionResult<object>> Create(
            CreateAutomationRuleRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Urls.Any(url => !IsValidHttpUrl(url)))
            {
                return BadRequest(new
                {
                    error = "Все URL должны быть корректными адресами HTTP или HTTPS."
                });
            }

            var rule = await _automationRulesService.CreateAsync(
                request,
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
            var (result, rule) = await _automationRulesService.StopAsync(
                id,
                cancellationToken);

            return result switch
            {
                StopAutomationResult.NotFound => NotFound(new
                {
                    error = "Автоматизация не найдена."
                }),

                StopAutomationResult.AlreadyStopped => Ok(new
                {
                    ruleId = rule!.Id,
                    isEnabled = false,
                    message = "Автоматизация уже остановлена."
                }),

                _ => Ok(new
                {
                    ruleId = rule!.Id,
                    isEnabled = rule.IsEnabled,
                    message = "Автоматизация остановлена."
                })
            };
        }

        // Получить историю запусков автоматизации.
        [HttpGet("{id:guid}/runs")]
        public async Task<ActionResult<object>> GetRuns(
            Guid id,
            CancellationToken cancellationToken)
        {
            var runs = await _automationRulesService.GetRunsAsync(
                id,
                cancellationToken);

            if (runs is null)
            {
                return NotFound(new
                {
                    error = "Автоматизация не найдена."
                });
            }

            return Ok(runs);
        }

        private static bool IsValidHttpUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}