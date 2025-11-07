using RabbitMQ.Client;

namespace Common.Messaging;

public interface IRabbitMQService : IDisposable
{
    void PublishMessage(string queueName, object message);
    void StartConsuming<T>(string queueName, Action<T> messageHandler);
}
