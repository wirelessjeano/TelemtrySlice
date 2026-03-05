using Microsoft.EntityFrameworkCore;
using TelemetrySlice.Data;
using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Domain.Models;

namespace TelemetrySlice.Services;

public class AdminService(TelemetrySliceDbContext db, IEventMessageService eventMessageService) : IAdminService
{
    public async Task SeedAsync(int intervalSeconds = 30)
    {
        var customers = new[]
        {
            new Customer { CustomerId = "acme-123" },
            new Customer { CustomerId = "beta-456" }
        };

        foreach (var customer in customers)
        {
            if (!await db.Customers.AnyAsync(c => c.CustomerId == customer.CustomerId))
                db.Customers.Add(customer);
        }

        var devices = new[]
        {
            new Device { CustomerId = "acme-123", DeviceId = "dev-001", Label = "Boiler #3", Location = "Plant A" },
            new Device { CustomerId = "acme-123", DeviceId = "dev-002", Label = "Chiller #1", Location = "Plant A" },
            new Device { CustomerId = "beta-456", DeviceId = "dev-100", Label = "Pump #9", Location = "Site B" }
        };

        foreach (var device in devices)
        {
            if (!await db.Devices.AnyAsync(d => d.CustomerId == device.CustomerId && d.DeviceId == device.DeviceId))
                db.Devices.Add(device);
        }

        await db.SaveChangesAsync();

        var messages = GenerateTelemetryMessages(devices, intervalSeconds);

        foreach (var message in messages)
        {
            await eventMessageService.PublishAsync(message);
        }
    }

    private static List<TelemetryEventMessage> GenerateTelemetryMessages(Device[] devices, int intervalSeconds)
    {
        var random = new Random(42);
        var now = DateTime.UtcNow;
        var start = now.AddHours(-24);
        var interval = TimeSpan.FromSeconds(intervalSeconds);
        var messages = new List<TelemetryEventMessage>();

        foreach (var device in devices)
        {
            var timestamp = start;
            while (timestamp <= now)
            {
                var progress = (timestamp - start).TotalSeconds / (now - start).TotalSeconds;
                // Bell curve centered at 0.5, width controlled by sigma
                var sigma = 0.02;
                var hill = Math.Exp(-Math.Pow(progress - 0.5, 2) / (2 * sigma * sigma));
                var baseValue = 12.0;
                var peakValue = 50.0;
                var center = baseValue + (peakValue - baseValue) * hill;
                var noise = (random.NextDouble() - 0.5) * 8.0;
                var value = Math.Round(center + noise, 1);

                messages.Add(new TelemetryEventMessage
                {
                    CustomerId = device.CustomerId,
                    DeviceId = device.DeviceId,
                    EventId = Guid.NewGuid().ToString(),
                    RecordedAt = timestamp,
                    Value = value
                });

                timestamp += interval;
            }
        }

        // Duplicate resends: pick ~1% of messages and re-add them
        var duplicateCount = Math.Max(1, messages.Count / 100);
        for (var i = 0; i < duplicateCount; i++)
        {
            var original = messages[random.Next(messages.Count)];
            messages.Add(new TelemetryEventMessage
            {
                CustomerId = original.CustomerId,
                DeviceId = original.DeviceId,
                EventId = original.EventId,
                RecordedAt = original.RecordedAt,
                Value = original.Value
            });
        }

        // Out-of-order arrivals: shuffle ~5% of messages to simulate late delivery
        var outOfOrderCount = Math.Max(1, messages.Count / 20);
        for (var i = 0; i < outOfOrderCount; i++)
        {
            var indexA = random.Next(messages.Count);
            var indexB = random.Next(messages.Count);
            (messages[indexA], messages[indexB]) = (messages[indexB], messages[indexA]);
        }

        return messages;
    }
}