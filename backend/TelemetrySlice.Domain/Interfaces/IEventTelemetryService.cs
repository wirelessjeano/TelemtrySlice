using TelemetrySlice.Domain.EventMessages;

namespace TelemetrySlice.Domain.Interfaces;

public interface IEventTelemetryService
{
    Task SaveBatchAsync(List<TelemetryEventMessage> messages, CancellationToken cancellationToken = default);
    Task SaveSingleAsync(TelemetryEventMessage message, CancellationToken cancellationToken = default);
}
