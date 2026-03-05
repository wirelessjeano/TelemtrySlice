using Microsoft.EntityFrameworkCore;
using TelemetrySlice.App.Writer.Queues;
using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Lib;
using TelemetrySlice.Services;

namespace TelemetrySlice.App.Writer.Services;

/// <summary>
/// Background service that reads telemetry messages from an in-memory channel and persists them
/// to the database in batches. Messages are deduplicated via a Redis cache check before being
/// included in a batch. Once a batch is saved, message keys are cached and the batch is
/// acknowledged to RabbitMQ using a single multiple-ack on the highest delivery tag.
/// If a batch insert fails due to a duplicate key, it falls back to saving each message individually.
/// </summary>
public class DatabaseWriterService(
    IServiceScopeFactory scopeFactory,
    DatabaseWriterQueue queue,
    IEventMessageService eventMessageService,
    ICacheService cacheService,
    ILogger<DatabaseWriterService> logger) : BackgroundService
{
    private static readonly TimeSpan BatchTimeout = TimeSpan.FromMilliseconds(500);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<IncomingMessage<TelemetryEventMessage>>(Constants.BatchSize);

        while (await queue.WaitToReadAsync(stoppingToken))
        {
            // Drain all immediately available items
            while (batch.Count < Constants.BatchSize && queue.TryRead(out var item))
            {
                if (item == null)
                    continue;

                //Key exists, we have a duplicate
                if (await cacheService.KeyExists(item.Message.Key()))
                {
                    eventMessageService.Ack(item.DeliveryTag);
                    continue;
                }
                
                batch.Add(item!);
            }

            // If batch isn't full, wait briefly for more items to arrive
            while (batch.Count < Constants.BatchSize)
            {
                using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                delayCts.CancelAfter(BatchTimeout);

                try
                {
                    if (!await queue.WaitToReadAsync(delayCts.Token))
                        break;

                    while (batch.Count < Constants.BatchSize && queue.TryRead(out var item))
                    {
                        if (item == null)
                            continue;
                        
                        //Key exists, we have a duplicate
                        if (await cacheService.KeyExists(item.Message.Key()))
                        {
                            eventMessageService.Ack(item.DeliveryTag);
                            continue;
                        }
                        
                        batch.Add(item!);
                           
                    }
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Timeout elapsed, flush what we have
                    break;
                }
            }

            if (batch.Count > 0)
            {
                var messages = batch.Select(x => x.Message).ToList();
                
                //get highest tag - can effectively acks the entire batch in one call
                var highestDeliveryTag = batch[^1].DeliveryTag;
                
                //save to db
                await SaveBatchAsync(messages, stoppingToken);
                
                //save keys to deduper
                await cacheService.SetKeysAsync(messages.Select(m => m.Key()), TimeSpan.FromMinutes(5));
                
                //Manual ack to rabbit
                eventMessageService.Ack(highestDeliveryTag, multiple: true);
            }

            batch.Clear();
        }
    }

    private async Task SaveBatchAsync(List<TelemetryEventMessage> batch, CancellationToken stoppingToken)
    {
        if (!batch.Any()) return;

        using var scope = scopeFactory.CreateScope();
        var eventTelemetryService = scope.ServiceProvider.GetRequiredService<IEventTelemetryService>();

        try
        {
            //1. Save the batch, this should succeed most of the time
            await eventTelemetryService.SaveBatchAsync(batch, stoppingToken);
        }
        catch (DbUpdateException)
        {
            // 2. The batch failed! The transaction rolled back.
            // This is not efficient, probably better to find the duplicate and save he rest.
            foreach (var item in batch)
            {
                using var fallbackScope = scopeFactory.CreateScope();
                var fallBackEventTelemetryService = fallbackScope.ServiceProvider.GetRequiredService<IEventTelemetryService>();
            
                try
                {
                    await fallBackEventTelemetryService.SaveSingleAsync(item, stoppingToken);
                }
                catch (DbUpdateException ex)
                {
                    // 3. We found the duplicate
                    logger.LogError(ex, $"Failed to insert item with key: {item.Key()}");
                }
            }
        }
        
        logger.LogInformation($"Saved batch: count = {batch.Count}, remaining = {queue.Count}");
    }
}
