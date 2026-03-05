namespace TelemetrySlice.Domain.DTOs;

public class DtoTelemetryMetrics
{
    public required double Latest { get; set; }
    public required double Minimum { get; set; }
    public required double Average { get; set; }
    public required double Maximum { get; set; }
    public required int TotalCount { get; set; }
}
