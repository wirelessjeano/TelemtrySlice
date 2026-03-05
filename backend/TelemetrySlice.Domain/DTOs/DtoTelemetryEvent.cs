namespace TelemetrySlice.Domain.DTOs;

public class DtoTelemetryEvent
{
    public required string EventId { get; set; }
    
    public required string DeviceId { get; set; }
    
    public required DateTime RecordedAt { get; set; }
    
    public required double Value { get; set; }
}