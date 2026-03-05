using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TelemetrySlice.Domain.EventMessages;
using TelemetrySlice.Domain.Interfaces;
using TelemetrySlice.Domain.Interfaces.EventMessages;

namespace TelemetrySlice.Services;

public class EventMessageService : IEventMessageService, IDisposable
{
    private const string ExchangeName = "telemetry.events";

    private readonly IConnection _connection;
    private readonly IModel _publishChannel;
    private readonly IModel _consumeChannel;
    private readonly object _ackLock = new();
    private bool _disposed;

    public EventMessageService(string rabbitConnection)
    {
        var factory = ParseConnectionString(rabbitConnection);
        _connection = factory.CreateConnection();
        _publishChannel = _connection.CreateModel();
        _consumeChannel = _connection.CreateModel();

        _publishChannel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
        _consumeChannel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync<T>(T e) where T : class, IEventMessage
    {
        var routingKey = typeof(T).Name.ToLowerInvariant();
        var body = JsonSerializer.SerializeToUtf8Bytes(e);

        var properties = _publishChannel.CreateBasicProperties();
        properties.DeliveryMode = 2; // persistent
        properties.ContentType = "application/json";

        _publishChannel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    public IDisposable? SubscribeAsync<T>(string subscriptionId, Func<IncomingMessage<T>, Task> onMessage, bool autoDelete = false, CancellationToken cancellationToken = default) where T : class, IEventMessage
    {
        var routingKey = typeof(T).Name.ToLowerInvariant();

        _consumeChannel.BasicQos(prefetchSize: 0, prefetchCount: TelemetrySlice.Lib.Constants.BatchSize * 2, global: false);

        _consumeChannel.QueueDeclare(
            queue: subscriptionId,
            durable: !autoDelete,
            exclusive: false,
            autoDelete: autoDelete);

        _consumeChannel.QueueBind(
            queue: subscriptionId,
            exchange: ExchangeName,
            routingKey: routingKey);

        var consumer = new EventingBasicConsumer(_consumeChannel);

        consumer.Received += (_, ea) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<T>(ea.Body.Span);
                if (message is null)
                {
                    Nack(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                var incomingMessage = new IncomingMessage<T>
                {
                    Message = message,
                    DeliveryTag = ea.DeliveryTag
                };

                onMessage(incomingMessage).GetAwaiter().GetResult();
            }
            catch (JsonException)
            {
                Nack(ea.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception)
            {
                Nack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        var consumerTag = _consumeChannel.BasicConsume(
            queue: subscriptionId,
            autoAck: false,
            consumer: consumer);

        return new SubscriptionDisposable(_consumeChannel, consumerTag);
    }

    public void Ack(ulong deliveryTag, bool multiple = false)
    {
        lock (_ackLock)
        {
            _consumeChannel.BasicAck(deliveryTag, multiple);
        }
    }

    public void Nack(ulong deliveryTag, bool multiple = false, bool requeue = true)
    {
        lock (_ackLock)
        {
            _consumeChannel.BasicNack(deliveryTag, multiple, requeue);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _publishChannel.Close();
        _publishChannel.Dispose();
        _consumeChannel.Close();
        _consumeChannel.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    private static ConnectionFactory ParseConnectionString(string connectionString)
    {
        var factory = new ConnectionFactory();
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim().ToLowerInvariant();
            var value = kv[1].Trim();

            switch (key)
            {
                case "host":
                    factory.HostName = value;
                    break;
                case "username":
                    factory.UserName = value;
                    break;
                case "password":
                    factory.Password = value;
                    break;
                case "port":
                    factory.Port = int.Parse(value);
                    break;
                case "virtualhost":
                    factory.VirtualHost = value;
                    break;
            }
        }

        return factory;
    }

    private sealed class SubscriptionDisposable(IModel channel, string consumerTag) : IDisposable
    {
        public void Dispose()
        {
            try
            {
                if (channel.IsOpen)
                    channel.BasicCancel(consumerTag);
            }
            catch
            {
                // Channel may already be closed
            }
        }
    }
}
