using Microsoft.AspNetCore.Mvc;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.App.API.Controllers;

[ApiController]
[Route("[controller]")]
public class DevicesController(IDeviceService deviceService, ICustomerService customerService) : ControllerBase
{
    /// <summary>
    /// Gets a list of devices by customer id
    /// </summary>
    /// <param name="customerId"></param>
    /// <returns></returns>
    [HttpGet("{customerId}")]
    public async Task<ActionResult<List<Device>>> GetByCustomerId(string customerId)
    {
        var devices = await deviceService.GetByCustomerIdAsync(customerId);
        return Ok(devices);
    }

    /// <summary>
    /// Gets a single device by customer id and device id
    /// </summary>
    [HttpGet("{customerId}/{deviceId}")]
    public async Task<ActionResult<Device>> GetById(string customerId, string deviceId)
    {
        if (!await customerService.CustomerExistsAsync(customerId))
            return NotFound($"Customer '{customerId}' not found.");

        var device = await deviceService.GetByIdAsync(customerId, deviceId);
        if (device is null)
            return NotFound($"Device '{deviceId}' not found for customer '{customerId}'.");

        return Ok(device);
    }
}