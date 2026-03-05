using TelemetrySlice.Domain.DTOs;
using TelemetrySlice.Domain.EventMessages.Bases;

namespace TelemetrySlice.Domain.EventMessages;

public class TelemetryEventMessage : EventMessage
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public TelemetryEventMessage()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }
    public TelemetryEventMessage(string customerId, DtoTelemetryEvent telemetryEvent)
    {
        CustomerId =  customerId;
        EventId = telemetryEvent.EventId;
        DeviceId = telemetryEvent.DeviceId;
        RecordedAt = telemetryEvent.RecordedAt;
        Value = telemetryEvent.Value;
    }
    
    public override string Key()
    {
        return $"{CustomerId}_{EventId}_{DeviceId}";    
    }

    public string CustomerId { get; set; } 
    public string EventId { get; set; } 
    public string DeviceId { get; set; } 
    
    public DateTime RecordedAt { get; set; } 
    public double Value { get; set; } 
}