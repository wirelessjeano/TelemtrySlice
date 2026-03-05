using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TelemetrySlice.App.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    /// <summary>
    /// Returns the health status of all registered health checks
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> Get()
    {
        var report = await healthCheckService.CheckHealthAsync();

        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds + "ms",
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds + "ms",
                description = e.Value.Description,
                exception = e.Value.Exception?.Message
            })
        };

        return report.Status == HealthStatus.Healthy ? Ok(result) : StatusCode(503, result);
    }
}