using Microsoft.EntityFrameworkCore;
using TelemetrySlice.Data;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.Services;

public class CustomerService(TelemetrySliceDbContext db, ICacheService cacheService) : ICustomerService
{
    public async Task<List<Customer>> GetAllAsync()
    {
        return await db.Customers.ToListAsync();
    }

    public async Task<bool> CustomerExistsAsync(string customerId)
    {
        var cacheKey = $"customer_exists:{customerId}";
        var cached = await cacheService.GetAsync<Customer>(cacheKey);
        if (cached is not null) return true;

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (customer is null) return false;

        await cacheService.SetAsync(cacheKey, customer);
        return true;
    }
}