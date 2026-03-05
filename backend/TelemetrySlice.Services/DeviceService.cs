using Microsoft.EntityFrameworkCore;
using TelemetrySlice.Data;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.Services;

public class DeviceService(TelemetrySliceDbContext db) : IDeviceService
{
    public async Task<List<Device>> GetByCustomerIdAsync(string customerId)
    {
        return await db.Devices.Where(d => d.CustomerId == customerId).ToListAsync();
    }

    public async Task<Device?> GetByIdAsync(string customerId, string deviceId)
    {
        return await db.Devices
            .FirstOrDefaultAsync(d => d.CustomerId == customerId && d.DeviceId == deviceId);
    }
}