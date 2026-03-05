using Microsoft.EntityFrameworkCore;
using TelemetrySlice.Data;
using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Services;
using Xunit;

namespace TelemetrySlice.Tests;

public class EventTelemetryServiceTests : IDisposable
{
    private readonly TelemetrySliceDbContext _db;
    private readonly EventTelemetryService _service;

    public EventTelemetryServiceTests()
    {
        var options = new DbContextOptionsBuilder<TelemetrySliceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TelemetrySliceDbContext(options);
        _service = new EventTelemetryService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task SaveSingleAsync_PersistsEntity()
    {
        var message = CreateMessage("c1", "d1", "e1", 42.0);

        await _service.SaveSingleAsync(message);

        var entity = await _db.TelemetryEvents.SingleAsync();
        Assert.Equal("c1", entity.CustomerId);
        Assert.Equal("d1", entity.DeviceId);
        Assert.Equal("e1", entity.EventId);
        Assert.Equal(42.0, entity.Value);
        Assert.Equal(message.RecordedAt, entity.RecordedAt);
    }

    [Fact]
    public async Task SaveBatchAsync_PersistsAllEntities()
    {
        var messages = new List<TelemetryEventMessage>
        {
            CreateMessage("c1", "d1", "e1", 10.0),
            CreateMessage("c1", "d1", "e2", 20.0),
            CreateMessage("c1", "d1", "e3", 30.0)
        };

        await _service.SaveBatchAsync(messages);

        Assert.Equal(3, await _db.TelemetryEvents.CountAsync());
    }

    [Fact]
    public async Task SaveBatchAsync_DeduplicatesByCompositeKey()
    {
        var messages = new List<TelemetryEventMessage>
        {
            CreateMessage("c1", "d1", "e1", 10.0),
            CreateMessage("c1", "d1", "e1", 10.0),
            CreateMessage("c1", "d1", "e2", 20.0)
        };

        await _service.SaveBatchAsync(messages);

        Assert.Equal(2, await _db.TelemetryEvents.CountAsync());
    }

    [Fact]
    public async Task SaveBatchAsync_EmptyList_DoesNotThrow()
    {
        await _service.SaveBatchAsync([]);

        Assert.Equal(0, await _db.TelemetryEvents.CountAsync());
    }

    [Fact]
    public async Task SaveSingleAsync_MapsAllFieldsCorrectly()
    {
        var recordedAt = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var message = new TelemetryEventMessage
        {
            CustomerId = "cust-abc",
            DeviceId = "dev-xyz",
            EventId = "evt-999",
            RecordedAt = recordedAt,
            Value = 99.9
        };

        await _service.SaveSingleAsync(message);

        var entity = await _db.TelemetryEvents.SingleAsync();
        Assert.Equal("cust-abc", entity.CustomerId);
        Assert.Equal("dev-xyz", entity.DeviceId);
        Assert.Equal("evt-999", entity.EventId);
        Assert.Equal(recordedAt, entity.RecordedAt);
        Assert.Equal(99.9, entity.Value);
    }

    [Fact]
    public async Task SaveBatchAsync_DifferentDevices_PersistsAll()
    {
        var messages = new List<TelemetryEventMessage>
        {
            CreateMessage("c1", "d1", "e1", 10.0),
            CreateMessage("c1", "d2", "e1", 20.0),
            CreateMessage("c2", "d1", "e1", 30.0)
        };

        await _service.SaveBatchAsync(messages);

        Assert.Equal(3, await _db.TelemetryEvents.CountAsync());
    }

    [Fact]
    public async Task SaveSingleAsync_SupportsCancellation()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _service.SaveSingleAsync(CreateMessage("c1", "d1", "e1", 1.0), cts.Token));
    }

    [Fact]
    public async Task SaveBatchAsync_SupportsCancellation()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var messages = new List<TelemetryEventMessage> { CreateMessage("c1", "d1", "e1", 1.0) };

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _service.SaveBatchAsync(messages, cts.Token));
    }

    // --- Duplicate resend tests ---

    [Fact]
    public async Task SaveBatchAsync_DuplicateResends_KeepsFirstOccurrence()
    {
        var recordedAt = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var messages = new List<TelemetryEventMessage>
        {
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = recordedAt, Value = 10.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = recordedAt, Value = 10.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = recordedAt, Value = 10.0 }
        };

        await _service.SaveBatchAsync(messages);

        var events = await _db.TelemetryEvents.ToListAsync();
        Assert.Single(events);
        Assert.Equal("e1", events[0].EventId);
        Assert.Equal(10.0, events[0].Value);
    }

    [Fact]
    public async Task SaveBatchAsync_DuplicatesAcrossDevices_AreNotDeduplicated()
    {
        // Same EventId but different DeviceId — these are distinct events
        var messages = new List<TelemetryEventMessage>
        {
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = DateTime.UtcNow, Value = 10.0 },
            new() { CustomerId = "c1", DeviceId = "d2", EventId = "e1", RecordedAt = DateTime.UtcNow, Value = 20.0 }
        };

        await _service.SaveBatchAsync(messages);

        Assert.Equal(2, await _db.TelemetryEvents.CountAsync());
    }

    [Fact]
    public async Task SaveBatchAsync_DuplicatesAcrossCustomers_AreNotDeduplicated()
    {
        // Same EventId + DeviceId but different CustomerId — these are distinct events
        var messages = new List<TelemetryEventMessage>
        {
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = DateTime.UtcNow, Value = 10.0 },
            new() { CustomerId = "c2", DeviceId = "d1", EventId = "e1", RecordedAt = DateTime.UtcNow, Value = 20.0 }
        };

        await _service.SaveBatchAsync(messages);

        Assert.Equal(2, await _db.TelemetryEvents.CountAsync());
    }

    [Fact]
    public async Task SaveBatchAsync_MixedDuplicatesAndUnique_PersistsCorrectCount()
    {
        // Simulates ~1% duplicate resends mixed in with unique messages
        var messages = new List<TelemetryEventMessage>
        {
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = DateTime.UtcNow, Value = 10.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e2", RecordedAt = DateTime.UtcNow, Value = 20.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e3", RecordedAt = DateTime.UtcNow, Value = 30.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = DateTime.UtcNow, Value = 10.0 }, // dup of e1
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e4", RecordedAt = DateTime.UtcNow, Value = 40.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e2", RecordedAt = DateTime.UtcNow, Value = 20.0 }, // dup of e2
        };

        await _service.SaveBatchAsync(messages);

        var events = await _db.TelemetryEvents.ToListAsync();
        Assert.Equal(4, events.Count);
        Assert.Equal(4, events.Select(e => e.EventId).Distinct().Count());
    }

    // --- Out-of-order arrival tests ---

    [Fact]
    public async Task SaveBatchAsync_OutOfOrderArrivals_AllPersisted()
    {
        // Messages arrive out of chronological order (simulating late delivery)
        var now = DateTime.UtcNow;
        var messages = new List<TelemetryEventMessage>
        {
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e3", RecordedAt = now.AddMinutes(-1), Value = 30.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = now.AddMinutes(-3), Value = 10.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e5", RecordedAt = now.AddMinutes(-5), Value = 50.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e2", RecordedAt = now.AddMinutes(-2), Value = 20.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e4", RecordedAt = now.AddMinutes(-4), Value = 40.0 },
        };

        await _service.SaveBatchAsync(messages);

        var events = await _db.TelemetryEvents.OrderBy(e => e.RecordedAt).ToListAsync();
        Assert.Equal(5, events.Count);
        // Verify all events persisted with their original timestamps regardless of arrival order
        Assert.Equal("e5", events[0].EventId);
        Assert.Equal("e4", events[1].EventId);
        Assert.Equal("e1", events[2].EventId);
        Assert.Equal("e2", events[3].EventId);
        Assert.Equal("e3", events[4].EventId);
    }

    [Fact]
    public async Task SaveBatchAsync_OutOfOrderWithDuplicates_DeduplicatesAndPersistsAll()
    {
        // Combines out-of-order arrivals with duplicate resends
        var now = DateTime.UtcNow;
        var messages = new List<TelemetryEventMessage>
        {
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e3", RecordedAt = now.AddMinutes(-1), Value = 30.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = now.AddMinutes(-3), Value = 10.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e3", RecordedAt = now.AddMinutes(-1), Value = 30.0 }, // dup resend
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e2", RecordedAt = now.AddMinutes(-2), Value = 20.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = now.AddMinutes(-3), Value = 10.0 }, // dup resend
        };

        await _service.SaveBatchAsync(messages);

        var events = await _db.TelemetryEvents.OrderBy(e => e.RecordedAt).ToListAsync();
        Assert.Equal(3, events.Count);
        Assert.Equal("e1", events[0].EventId);
        Assert.Equal("e2", events[1].EventId);
        Assert.Equal("e3", events[2].EventId);
    }

    [Fact]
    public async Task SaveBatchAsync_OutOfOrder_PreservesOriginalTimestamps()
    {
        // Ensures RecordedAt reflects when the event actually occurred, not when it arrived
        var t1 = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2025, 6, 15, 11, 0, 0, DateTimeKind.Utc);
        var t3 = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Arrive in reverse chronological order
        var messages = new List<TelemetryEventMessage>
        {
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e3", RecordedAt = t3, Value = 30.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e1", RecordedAt = t1, Value = 10.0 },
            new() { CustomerId = "c1", DeviceId = "d1", EventId = "e2", RecordedAt = t2, Value = 20.0 },
        };

        await _service.SaveBatchAsync(messages);

        var e1 = await _db.TelemetryEvents.SingleAsync(e => e.EventId == "e1");
        var e2 = await _db.TelemetryEvents.SingleAsync(e => e.EventId == "e2");
        var e3 = await _db.TelemetryEvents.SingleAsync(e => e.EventId == "e3");

        Assert.Equal(t1, e1.RecordedAt);
        Assert.Equal(t2, e2.RecordedAt);
        Assert.Equal(t3, e3.RecordedAt);
    }

    private static TelemetryEventMessage CreateMessage(string customerId, string deviceId, string eventId, double value)
    {
        return new TelemetryEventMessage
        {
            CustomerId = customerId,
            DeviceId = deviceId,
            EventId = eventId,
            RecordedAt = DateTime.UtcNow,
            Value = value
        };
    }
}