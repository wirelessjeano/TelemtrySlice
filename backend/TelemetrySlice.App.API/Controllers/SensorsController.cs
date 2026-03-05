using Microsoft.AspNetCore.Mvc;
using TelemetrySlice.Domain.DTOs;
using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.App.API.Controllers;

[ApiController]
[Route("[controller]")]
public class SensorsController(
    IEventMessageService eventMessageService,
    ICustomerService customerService,
    ITelemetryService telemetryService) : ControllerBase
{
    /// <summary>
    /// Receives a collection of telemetry events
    /// </summary>
    [HttpPost("{customerId}")]
    public async Task<IActionResult> Post(string customerId, [FromBody] List<DtoTelemetryEvent> models)
    {
        if (!await customerService.CustomerExistsAsync(customerId))
            return NotFound($"Customer '{customerId}' not found.");

        var messages = models.Select(m => new TelemetryEventMessage(customerId, m));

        foreach (var message in messages)
        {
            await eventMessageService.PublishAsync(message);
        }

        return Ok("Success!");
    }

    /// <summary>
    /// Returns chart data points for a device over the last 24 hours
    /// </summary>
    [HttpGet("{customerId}/{deviceId}/chart")]
    public async Task<ActionResult<List<DtoChartPoint>>> GetChartData(string customerId, string deviceId)
    {
        if (!await customerService.CustomerExistsAsync(customerId))
            return NotFound($"Customer '{customerId}' not found.");

        var data = await telemetryService.GetChartDataAsync(customerId, deviceId);
        return Ok(data);
    }

    /// <summary>
    /// Returns paginated telemetry events for a device over the last 24 hours
    /// </summary>
    [HttpGet("{customerId}/{deviceId}/table")]
    public async Task<ActionResult<DtoPagedResult<TelemetryEvent>>> GetTableData(
        string customerId, string deviceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        if (!await customerService.CustomerExistsAsync(customerId))
            return NotFound($"Customer '{customerId}' not found.");

        var data = await telemetryService.GetPagedDataAsync(customerId, deviceId, page, pageSize);
        return Ok(data);
    }

    /// <summary>
    /// Returns aggregation metrics for a device over the last 24 hours
    /// </summary>
    [HttpGet("{customerId}/{deviceId}/metrics")]
    public async Task<ActionResult<DtoTelemetryMetrics>> GetMetrics(string customerId, string deviceId)
    {
        if (!await customerService.CustomerExistsAsync(customerId))
            return NotFound($"Customer '{customerId}' not found.");

        var metrics = await telemetryService.GetMetricsAsync(customerId, deviceId);
        if (metrics is null)
            return NotFound("No telemetry data found for the last 24 hours.");

        return Ok(metrics);
    }
}