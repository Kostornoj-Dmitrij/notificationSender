using System.Text.Json;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Messaging;

public class RabbitMQService : IRabbitMQService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly List<string> _declaredQueues = new();

    public RabbitMQService(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQService> logger)
    {
        _logger = logger;
        var config = settings.Value;

        var maxRetries = 5;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to RabbitMQ (attempt {Attempt}/{MaxRetries})...",
                    attempt, maxRetries);

                var factory = new ConnectionFactory()
                {
                    HostName = config.Host,
                    UserName = config.Username,
                    Password = config.Password,
                    Port = config.Port,
                    VirtualHost = "/",
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                    SocketReadTimeout = TimeSpan.FromSeconds(30),
                    SocketWriteTimeout = TimeSpan.FromSeconds(30)
                };

                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                _logger.LogInformation("RabbitMQ connection established to {Host}:{Port}",
                    config.Host, config.Port);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries})",
                    attempt, maxRetries);

                if (attempt == maxRetries)
                {
                    _logger.LogError(ex, "Unable to connect to RabbitMQ after {MaxRetries} attempts", maxRetries);
                    throw;
                }

                _logger.LogInformation("Waiting {RetryDelay} before next attempt...", retryDelay);
                Thread.Sleep(retryDelay);
            }
        }
    }

    public void PublishMessage(string queueName, object message)
    {
        try
        {
            // объявляем очередь если еще не объявлена
            if (!_declaredQueues.Contains(queueName))
            {
                _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false).GetAwaiter().GetResult();
                _declaredQueues.Add(queueName);
            }

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            _channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: body
            ).GetAwaiter().GetResult();

            _logger.LogDebug("Message published to {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to {QueueName}", queueName);
            throw;
        }
    }

    public void StartConsuming<T>(string queueName, Action<T> messageHandler)
    {
        try
        {
            // объявляем очередь если еще не объявлена
            if (!_declaredQueues.Contains(queueName))
            {
                _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false).GetAwaiter().GetResult();
                _declaredQueues.Add(queueName);
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var deserializedMessage = JsonSerializer.Deserialize<T>(message);

                    if (deserializedMessage != null)
                    {
                        messageHandler(deserializedMessage);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize message from queue {QueueName}", queueName);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer).GetAwaiter().GetResult();

            _logger.LogInformation("Started consuming from queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting consumer for queue {QueueName}", queueName);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}