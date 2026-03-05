using TelemetrySlice.Data;
using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.Services;

public class EventTelemetryService(TelemetrySliceDbContext dbContext) : IEventTelemetryService
{
    public async Task SaveBatchAsync(List<TelemetryEventMessage> messages, CancellationToken cancellationToken = default)
    {
        //We get new objects so DbContext doesnt track batch items - this is important for failover
        var entities = messages
            .DistinctBy(m => new { m.CustomerId, m.DeviceId, m.EventId })
            .Select(m => new TelemetryEvent
            {
                CustomerId = m.CustomerId,
                DeviceId = m.DeviceId,
                EventId = m.EventId,
                RecordedAt = m.RecordedAt,
                Value = m.Value
            }).ToList();

        await dbContext.TelemetryEvents.AddRangeAsync(entities, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveSingleAsync(TelemetryEventMessage message, CancellationToken cancellationToken = default)
    {
        var entity = new TelemetryEvent
        {
            CustomerId = message.CustomerId,
            DeviceId = message.DeviceId,
            EventId = message.EventId,
            RecordedAt = message.RecordedAt,
            Value = message.Value
        };

        await dbContext.TelemetryEvents.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
