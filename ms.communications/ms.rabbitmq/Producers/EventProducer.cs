﻿using Microsoft.Extensions.Configuration;
using ms.rabbitmq.Events;
using RabbitMQ.Client;
using System.Text;

namespace ms.rabbitmq.Producers
{
    public class EventProducer : IProducer
    {
        private readonly IConfiguration _configuration;

        public EventProducer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Produce(IRabbitMqEvent rabbitMqEvent)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration.GetSection("Communication:EventBus:HostName").Value
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            var queue = rabbitMqEvent.GetType().Name;

            // Quee storage messages in memory, allow multi connections and not is deleted if don't have any consumer
            channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, null);
            var body = Encoding.UTF8.GetBytes(rabbitMqEvent.Serialize());

            // Pusblish routing_key of message
            channel.BasicPublish("", queue, null, body);
        }
    }
}
