using AutoMapper;
using MediatR;
using ms.rabbitmq.Consumers;
using ms.users.api.Events;
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
        private readonly ILogger<UserConsumer> _logger;
        private IConnection _connection;

        public UserConsumer(IMediator mediator, IMapper mapper, IConfiguration configuration, ILogger<UserConsumer> logger)
        {
            _mediator = mediator;
            _mapper = mapper;
            _configuration = configuration;
            _logger = logger;
        }

        public void Subscribe()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration.GetValue<string>("Communication:EventBus:HostName")
            };

            _connection = factory.CreateConnection();
            using var channel = _connection.CreateModel();
            
            var queue = nameof(EmployeeCreateEvent);

            // Quee storage messages in memory, allow multi connections and not is deleted if don't have any consumer
            channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += ReceivedEvent;

            // Pusblish routing_key of message
            channel.BasicConsume(queue: queue, autoAck: true, consumer: consumer);
        }

        private async void ReceivedEvent(object? sender, BasicDeliverEventArgs e)
        {
            if(e.RoutingKey == nameof(EmployeeCreateEvent))
            {
                _logger.LogInformation("Received event");
                var message = Encoding.UTF8.GetString(e.Body.Span);
                var employeeCreatedEvent = JsonSerializer.Deserialize<EmployeeCreateEvent>(message);
                _logger.LogInformation("Send Create user ", message);
                var result = await _mediator.Send(_mapper.Map<CreateUserAccountCommand>(employeeCreatedEvent));
            }
        }

        public void Unsubscribe() => _connection.Dispose();
    }
}
