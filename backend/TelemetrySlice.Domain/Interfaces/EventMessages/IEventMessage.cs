namespace TelemetrySlice.Domain.Interfaces.EventMessages;

public interface IEventMessage
{
    string Key();
    Guid MessageId { get; set; }
    long UnixTimeStamp { get; }
}