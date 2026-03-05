using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.Domain.Interfaces;

public interface IDeviceService
{
    Task<List<Device>> GetByCustomerIdAsync(string customerId);
    Task<Device?> GetByIdAsync(string customerId, string deviceId);
}