using System.Threading.Channels;
using TelemetrySlice.Domain.EventMessages;

namespace TelemetrySlice.App.Writer.Queues;

/// <summary>
/// Bounded in-memory queue that sits between the RabbitMQ subscriber and the database writer.
/// Uses a <see cref="Channel{T}"/> with a 1 million capacity and wait-on-full backpressure to prevent
/// out-of-memory conditions when the database writer falls behind the incoming message rate.
/// </summary>
public class DatabaseWriterQueue
{
    // A Bounded channel provides "backpressure". If the DB falls behind,
    // it prevents the app from running out of memory.
    private readonly Channel<IncomingMessage<TelemetryEventMessage>> _channel = Channel.CreateBounded<IncomingMessage<TelemetryEventMessage>>(new BoundedChannelOptions(100_000)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

    // Called by your API/MQTT handlers
    public async ValueTask EnqueueAsync(IncomingMessage<TelemetryEventMessage> data, CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(data, ct);
    }

    // Waits until at least one item is available
    public ValueTask<bool> WaitToReadAsync(CancellationToken ct = default)
    {
        return _channel.Reader.WaitToReadAsync(ct);
    }

    public bool TryRead(out IncomingMessage<TelemetryEventMessage>? item)
    {
        return _channel.Reader.TryRead(out item);
    }

    public int Count => _channel.Reader.Count;
}
