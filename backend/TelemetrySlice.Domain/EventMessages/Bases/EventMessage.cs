using TelemetrySlice.Domain.Interfaces.EventMessages;
using TelemetrySlice.Lib.Extensions;

namespace TelemetrySlice.Domain.EventMessages.Bases;

public class EventMessage : IEventMessage
{
    public virtual string Key()
    {
        throw new NotImplementedException("Every message needs a unique key.");
    }
    
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public long UnixTimeStamp { get; } = DateTime.UtcNow.ToUnixTimeStamp();
}