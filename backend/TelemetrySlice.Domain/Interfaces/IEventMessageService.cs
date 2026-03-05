using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Domain.Interfaces.EventMessages;

namespace TelemetrySlice.Domain.Interfaces;

public interface IEventMessageService
{
    Task PublishAsync<T>(T e) where T : class, IEventMessage;

    IDisposable? SubscribeAsync<T>(string subscriptionId, Func<IncomingMessage<T>, Task> onMessage, bool autoDelete = false, CancellationToken cancellationToken = default) where T : class, IEventMessage;

    void Ack(ulong deliveryTag, bool multiple = false);

    void Nack(ulong deliveryTag, bool multiple = false, bool requeue = true);
}
