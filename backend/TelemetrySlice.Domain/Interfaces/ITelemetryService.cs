using TelemetrySlice.Domain.DTOs;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.Domain.Interfaces;

public interface ITelemetryService
{
    Task<List<DtoChartPoint>> GetChartDataAsync(string customerId, string deviceId);
    Task<DtoPagedResult<TelemetryEvent>> GetPagedDataAsync(string customerId, string deviceId, int page, int pageSize);
    Task<DtoTelemetryMetrics?> GetMetricsAsync(string customerId, string deviceId);
}
