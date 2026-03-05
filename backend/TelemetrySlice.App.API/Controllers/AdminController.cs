using Microsoft.AspNetCore.Mvc;
using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Domain.Interfaces;

namespace TelemetrySlice.App.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AdminController(IAdminService adminService, IEventMessageService eventMessageService) : ControllerBase
{
    /// <summary>
    /// A seed function
    /// </summary>
    /// <returns></returns>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(int intervalSeconds = 30)
    {
        await adminService.SeedAsync(intervalSeconds);
        return Ok("Seed data applied.");
    }
    
    [HttpPost("LoadTest")]
    public async Task<IActionResult> LoadTest(int count = 100000)
    {
        for (int i = 0; i < count; i++)
        {
            await eventMessageService.PublishAsync(new TelemetryEventMessage
            {
                MessageId = Guid.NewGuid(),
                CustomerId = "awe",
                EventId = Guid.NewGuid().ToString(),
                Value = 23.00,
                DeviceId = "awdwa",
                RecordedAt = DateTime.UtcNow
            });
        }
        
        return Ok("Published data applied.");
    }
}