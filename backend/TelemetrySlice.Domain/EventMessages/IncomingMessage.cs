namespace TelemetrySlice.Domain.EventMessages;

public class IncomingMessage<T> where T : class
{
    public required T Message { get; init; }
    public required ulong DeliveryTag { get; init; }
}
