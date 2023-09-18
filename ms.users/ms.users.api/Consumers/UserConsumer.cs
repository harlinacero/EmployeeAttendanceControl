using AutoMapper;
using MediatR;
using ms.rabbitmq.Consumers;
using ms.rabbitmq.Events;
using ms.users.application.Commands;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ms.users.api.Consumers
{
    public class UserConsumer : IConsumer
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private IConnection _connection;

        public UserConsumer(IMediator mediator, IMapper mapper, IConfiguration configuration)
        {
            _mediator = mediator;
            _mapper = mapper;
            _configuration = configuration;
        }

        public void Subscribe()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration.GetValue<string>("Communication:EventBus:HostName")
            };

            _connection = factory.CreateConnection();
            using var channel = _connection.CreateModel();
            
            var queue = typeof(EmployeeCreateEvent).Name;

            // Quee storage messages in memory, allow multi connections and not is deleted if don't have any consumer
            channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += ReceivedEvent;

            // Pusblish routing_key of message
            channel.BasicConsume(queue: queue, autoAck: true, consumer: consumer);
        }

        private async void ReceivedEvent(object? sender, BasicDeliverEventArgs e)
        {
            if(e.RoutingKey == typeof(EmployeeCreateEvent).Name)
            {
                var message = Encoding.UTF8.GetString(e.Body.Span);
                var employeeCreatedEvent = JsonSerializer.Deserialize<EmployeeCreateEvent>(message);
                var result = await _mediator.Send(_mapper.Map<CreateUserAccountCommand>(employeeCreatedEvent));
            }
        }

        public void Unsubscribe() => _connection.Dispose();
    }
}
