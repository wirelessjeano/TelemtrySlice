namespace TelemetrySlice.Domain.Models;

public class Device
{
    public required string DeviceId { get; set; }
    public required string CustomerId { get; set; }
    public required string Label { get; set; }
    public required string Location { get; set; }
}