using TelemetrySlice.App.Writer.Queues;
using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Domain.Interfaces;

namespace TelemetrySlice.App.Writer.Services;

/// <summary>
/// Background service that subscribes to telemetry event messages from RabbitMQ and enqueues
/// them into the in-memory <see cref="DatabaseWriterQueue"/> for downstream batch processing.
/// The subscription is disposed on shutdown to cleanly disconnect from the broker.
/// </summary>
public class IncomingTelemetryService(IEventMessageService eventMessageService, DatabaseWriterQueue queue) : BackgroundService
{
    private IDisposable? _subscription;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _subscription = eventMessageService.SubscribeAsync<TelemetryEventMessage>(
            "IncomingTelemetryService", HandleEvent, cancellationToken: stoppingToken);

        return Task.CompletedTask;
    }

    private async Task HandleEvent(IncomingMessage<TelemetryEventMessage> incomingMessage)
    {
        await queue.EnqueueAsync(incomingMessage);
    }

    public override void Dispose()
    {
        _subscription?.Dispose();
        base.Dispose();
    }
}
