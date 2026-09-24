using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ms.rabbitmq.Events;
using RabbitMQ.Client;
using System.Text;

namespace ms.rabbitmq.Producers
{
    public class EventProducer : IProducer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventProducer> _logger;

        public EventProducer(IConfiguration configuration, ILogger<EventProducer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Produce(IRabbitMqEvent rabbitMqEvent)
        {
            ArgumentNullException.ThrowIfNull(rabbitMqEvent);

            var hostName = _configuration.GetValue<string>("Communication:EventBus:HostName");
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new InvalidOperationException("RabbitMQ host name is not configured.");
            }

            var factory = new ConnectionFactory()
            {
                HostName = hostName
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            var queue = rabbitMqEvent.GetType().Name;

            // Quee storage messages in memory, allow multi connections and not is deleted if don't have any consumer

            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, null);
            var body = Encoding.UTF8.GetBytes(rabbitMqEvent.Serialize());
            _logger.LogInformation($"Send event {queue}");

            // Pusblish routing_key of message
            //channel.BasicPublish("", queue, null, body);
            await channel.BasicPublishAsync("", queue, body);
        }
    }
}
