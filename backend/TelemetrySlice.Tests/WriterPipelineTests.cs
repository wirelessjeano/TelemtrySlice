using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TelemetrySlice.App.Writer.Queues;
using TelemetrySlice.App.Writer.Services;
using TelemetrySlice.Data;
using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Services;
using Xunit;

namespace TelemetrySlice.Tests;

public class WriterPipelineTests : IDisposable
{
    private readonly TelemetrySliceDbContext _db;
    private readonly DatabaseWriterQueue _queue;
    private readonly IEventMessageService _eventMessageService;
    private readonly ICacheService _cacheService;
    private readonly IServiceScopeFactory _scopeFactory;

    public WriterPipelineTests()
    {
        var options = new DbContextOptionsBuilder<TelemetrySliceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TelemetrySliceDbContext(options);
        _queue = new DatabaseWriterQueue();
        _eventMessageService = Substitute.For<IEventMessageService>();
        _cacheService = Substitute.For<ICacheService>();
        _cacheService.KeyExists(Arg.Any<string>()).Returns(false);
        _cacheService.SetKeysAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<TimeSpan?>()).Returns(Task.CompletedTask);

        // Build a real IServiceScopeFactory that resolves IEventTelemetryService with our in-memory DB
        var services = new ServiceCollection();
        services.AddDbContext<TelemetrySliceDbContext>(o => o.UseInMemoryDatabase(_db.Database.GetConnectionString()!),
            optionsLifetime: ServiceLifetime.Scoped);
        // Register using a factory that returns our shared db instance's options
        services.AddScoped<IEventTelemetryService>(sp =>
            new EventTelemetryService(new TelemetrySliceDbContext(options)));

        var provider = services.BuildServiceProvider();
        _scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task Message_FlowsFromQueue_ThroughWriter_ToDatabase()
    {
        // Arrange: create a telemetry message
        var message = new TelemetryEventMessage
        {
            CustomerId = "c1",
            DeviceId = "d1",
            EventId = "e1",
            RecordedAt = DateTime.UtcNow,
            Value = 42.5
        };

        var incoming = new IncomingMessage<TelemetryEventMessage>
        {
            Message = message,
            DeliveryTag = 1
        };

        // Act: enqueue the message (simulating what IncomingTelemetryService does)
        await _queue.EnqueueAsync(incoming);

        // Start the DatabaseWriterService and let it process
        var writerService = new DatabaseWriterService(
            _scopeFactory, _queue, _eventMessageService, _cacheService,
            NullLogger<DatabaseWriterService>.Instance);

        using var cts = new CancellationTokenSource();

        var writerTask = writerService.StartAsync(cts.Token);
        await writerTask;

        // Give the background service time to drain the queue
        await Task.Delay(1500);

        // Stop the service
        await cts.CancelAsync();
        try { await writerService.StopAsync(CancellationToken.None); }
        catch (OperationCanceledException) { }

        // Assert: message was persisted to the database
        var events = await _db.TelemetryEvents.ToListAsync();
        Assert.Single(events);
        Assert.Equal("c1", events[0].CustomerId);
        Assert.Equal("d1", events[0].DeviceId);
        Assert.Equal("e1", events[0].EventId);
        Assert.Equal(42.5, events[0].Value);

        // Assert: RabbitMQ ack was called
        _eventMessageService.Received().Ack(1, multiple: true);

        // Assert: cache keys were set for deduplication
        await _cacheService.Received().SetKeysAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task DuplicateMessage_IsSkipped_ByCache()
    {
        // Arrange: cache says this key already exists
        var message = new TelemetryEventMessage
        {
            CustomerId = "c1",
            DeviceId = "d1",
            EventId = "dup-1",
            RecordedAt = DateTime.UtcNow,
            Value = 10.0
        };

        _cacheService.KeyExists(message.Key()).Returns(true);

        var incoming = new IncomingMessage<TelemetryEventMessage>
        {
            Message = message,
            DeliveryTag = 2
        };

        await _queue.EnqueueAsync(incoming);

        var writerService = new DatabaseWriterService(
            _scopeFactory, _queue, _eventMessageService, _cacheService,
            NullLogger<DatabaseWriterService>.Instance);

        using var cts = new CancellationTokenSource();

        var writerTask = writerService.StartAsync(cts.Token);
        await writerTask;

        await Task.Delay(1500);

        await cts.CancelAsync();
        try { await writerService.StopAsync(CancellationToken.None); }
        catch (OperationCanceledException) { }

        // Assert: nothing was persisted
        var events = await _db.TelemetryEvents.ToListAsync();
        Assert.Empty(events);

        // Assert: the duplicate was acked (so RabbitMQ doesn't redeliver)
        _eventMessageService.Received().Ack(2, multiple: true);
    }

    [Fact]
    public async Task MultiplMessages_AreProcessedInSingleBatch()
    {
        // Arrange: enqueue 3 messages
        for (var i = 1; i <= 3; i++)
        {
            var msg = new TelemetryEventMessage
            {
                CustomerId = "c1",
                DeviceId = "d1",
                EventId = $"e{i}",
                RecordedAt = DateTime.UtcNow.AddMinutes(-i),
                Value = i * 10.0
            };

            await _queue.EnqueueAsync(new IncomingMessage<TelemetryEventMessage>
            {
                Message = msg,
                DeliveryTag = (ulong)i
            });
        }

        var writerService = new DatabaseWriterService(
            _scopeFactory, _queue, _eventMessageService, _cacheService,
            NullLogger<DatabaseWriterService>.Instance);

        using var cts = new CancellationTokenSource();

        var writerTask = writerService.StartAsync(cts.Token);
        await writerTask;

        await Task.Delay(1500);

        await cts.CancelAsync();
        try { await writerService.StopAsync(CancellationToken.None); }
        catch (OperationCanceledException) { }

        // Assert: all 3 events persisted
        var events = await _db.TelemetryEvents.ToListAsync();
        Assert.Equal(3, events.Count);

        // Assert: ack called with the highest delivery tag
        _eventMessageService.Received().Ack(3, multiple: true);
    }

    [Fact]
    public async Task MixedBatch_OnlyNewMessages_ArePersistedAndCached()
    {
        // Arrange: 3 messages, middle one is a known duplicate
        var msg1 = new TelemetryEventMessage
        {
            CustomerId = "c1", DeviceId = "d1", EventId = "new-1",
            RecordedAt = DateTime.UtcNow, Value = 10.0
        };
        var msgDup = new TelemetryEventMessage
        {
            CustomerId = "c1", DeviceId = "d1", EventId = "dup-1",
            RecordedAt = DateTime.UtcNow, Value = 20.0
        };
        var msg2 = new TelemetryEventMessage
        {
            CustomerId = "c1", DeviceId = "d1", EventId = "new-2",
            RecordedAt = DateTime.UtcNow, Value = 30.0
        };

        // Cache reports only the duplicate key as existing
        _cacheService.KeyExists(msgDup.Key()).Returns(true);

        await _queue.EnqueueAsync(new IncomingMessage<TelemetryEventMessage> { Message = msg1, DeliveryTag = 1 });
        await _queue.EnqueueAsync(new IncomingMessage<TelemetryEventMessage> { Message = msgDup, DeliveryTag = 2 });
        await _queue.EnqueueAsync(new IncomingMessage<TelemetryEventMessage> { Message = msg2, DeliveryTag = 3 });

        var writerService = new DatabaseWriterService(
            _scopeFactory, _queue, _eventMessageService, _cacheService,
            NullLogger<DatabaseWriterService>.Instance);

        using var cts = new CancellationTokenSource();
        await writerService.StartAsync(cts.Token);
        await Task.Delay(1500);
        await cts.CancelAsync();
        try { await writerService.StopAsync(CancellationToken.None); }
        catch (OperationCanceledException) { }

        // Assert: only 2 new messages persisted, duplicate skipped
        var events = await _db.TelemetryEvents.ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.EventId == "new-1");
        Assert.Contains(events, e => e.EventId == "new-2");
        Assert.DoesNotContain(events, e => e.EventId == "dup-1");

        // Assert: cache was checked for all 3 keys
        await _cacheService.Received().KeyExists(msg1.Key());
        await _cacheService.Received().KeyExists(msgDup.Key());
        await _cacheService.Received().KeyExists(msg2.Key());

        // Assert: SetKeysAsync was called with the new message keys (not the duplicate)
        await _cacheService.Received().SetKeysAsync(
            Arg.Is<IEnumerable<string>>(keys =>
                keys.Contains(msg1.Key()) && keys.Contains(msg2.Key()) && !keys.Contains(msgDup.Key())),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task AllDuplicates_NothingPersisted_NoCacheWrite()
    {
        // Arrange: all messages are duplicates
        var messages = new[]
        {
            new TelemetryEventMessage { CustomerId = "c1", DeviceId = "d1", EventId = "dup-a", RecordedAt = DateTime.UtcNow, Value = 1.0 },
            new TelemetryEventMessage { CustomerId = "c1", DeviceId = "d1", EventId = "dup-b", RecordedAt = DateTime.UtcNow, Value = 2.0 }
        };

        _cacheService.KeyExists(Arg.Any<string>()).Returns(true);

        for (ulong i = 0; i < (ulong)messages.Length; i++)
        {
            await _queue.EnqueueAsync(new IncomingMessage<TelemetryEventMessage>
            {
                Message = messages[i],
                DeliveryTag = i + 1
            });
        }

        var writerService = new DatabaseWriterService(
            _scopeFactory, _queue, _eventMessageService, _cacheService,
            NullLogger<DatabaseWriterService>.Instance);

        using var cts = new CancellationTokenSource();
        await writerService.StartAsync(cts.Token);
        await Task.Delay(1500);
        await cts.CancelAsync();
        try { await writerService.StopAsync(CancellationToken.None); }
        catch (OperationCanceledException) { }

        // Assert: nothing persisted
        var events = await _db.TelemetryEvents.ToListAsync();
        Assert.Empty(events);

        // Assert: SetKeysAsync was never called (no new keys to cache)
        await _cacheService.DidNotReceive().SetKeysAsync(
            Arg.Any<IEnumerable<string>>(), Arg.Any<TimeSpan?>());
    }
}