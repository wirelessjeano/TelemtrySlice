namespace TelemetrySlice.Domain.DTOs;

public class DtoPagedResult<T>
{
    public required List<T> Data { get; set; }
    public required int Page { get; set; }
    public required int TotalPages { get; set; }
    public required int TotalCount { get; set; }
}
