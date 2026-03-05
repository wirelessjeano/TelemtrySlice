namespace TelemetrySlice.Domain.DTOs;

public class DtoChartPoint
{
    public required double Value { get; set; }
    public required DateTime RecordedAt { get; set; }
}
