using Microsoft.EntityFrameworkCore;
using TelemetrySlice.Data;
using TelemetrySlice.Domain.DTOs;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.Services;

public class TelemetryService(TelemetrySliceDbContext db) : ITelemetryService
{
    public async Task<List<DtoChartPoint>> GetChartDataAsync(string customerId, string deviceId)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        return await db.TelemetryEvents
            .Where(e => e.CustomerId == customerId && e.DeviceId == deviceId && e.RecordedAt >= cutoff)
            .OrderBy(e => e.RecordedAt)
            .Select(e => new DtoChartPoint
            {
                Value = e.Value,
                RecordedAt = e.RecordedAt
            })
            .ToListAsync();
    }

    public async Task<DtoPagedResult<TelemetryEvent>> GetPagedDataAsync(string customerId, string deviceId, int page, int pageSize)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var query = db.TelemetryEvents
            .Where(e => e.CustomerId == customerId && e.DeviceId == deviceId && e.RecordedAt >= cutoff)
            .OrderByDescending(e => e.RecordedAt);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new DtoPagedResult<TelemetryEvent>
        {
            Data = data,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };
    }

    public async Task<DtoTelemetryMetrics?> GetMetricsAsync(string customerId, string deviceId)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var query = db.TelemetryEvents
            .Where(e => e.CustomerId == customerId && e.DeviceId == deviceId && e.RecordedAt >= cutoff);

        // Single round-trip for min/max/avg/count
        var aggregates = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Min = g.Min(e => e.Value),
                Max = g.Max(e => e.Value),
                Avg = g.Average(e => e.Value),
                Count = g.Count()
            })
            .FirstOrDefaultAsync();

        if (aggregates is null || aggregates.Count == 0)
            return null;

        // Second round-trip for latest value (uses the composite index for ordering)
        var latest = await query
            .OrderByDescending(e => e.RecordedAt)
            .Select(e => e.Value)
            .FirstAsync();

        return new DtoTelemetryMetrics
        {
            Latest = latest,
            Minimum = aggregates.Min,
            Maximum = aggregates.Max,
            Average = aggregates.Avg,
            TotalCount = aggregates.Count
        };
    }
}
