using Microsoft.AspNetCore.Mvc;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.App.API.Controllers;

[ApiController]
[Route("[controller]")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    /// <summary>
    /// Gets a list of all customers
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<List<Customer>>> GetAll()
    {
        var customers = await customerService.GetAllAsync();
        return Ok(customers);
    }
}