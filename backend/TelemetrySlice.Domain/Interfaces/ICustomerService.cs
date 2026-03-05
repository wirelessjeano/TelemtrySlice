using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.Domain.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<bool> CustomerExistsAsync(string customerId);
}