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
        private IChannel _channel;

        public UserConsumer(IMediator mediator, IMapper mapper, IConfiguration configuration, ILogger<UserConsumer> logger)
        {
            _mediator = mediator;
            _mapper = mapper;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SubscribeAsync()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _configuration.GetValue<string>("Communication:EventBus:HostName")
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            var queue = nameof(EmployeeCreateEvent);

            // Quee storage messages in memory, allow multi connections and not is deleted if don't have any consumer
            await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, null);
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += ReceivedEvent;

            // Pusblish routing_key of message
            await _channel.BasicConsumeAsync(queue: queue, autoAck: true, consumer: consumer);
            _logger.LogInformation("RabbitMQ consumer subscribed to queue {Queue}", queue);
        }

        private async Task ReceivedEvent(object? sender, BasicDeliverEventArgs e)
        {
            if (e.RoutingKey == nameof(EmployeeCreateEvent))
            {
                _logger.LogInformation("Received event");
                var message = Encoding.UTF8.GetString(e.Body.Span);
                var employeeCreatedEvent = JsonSerializer.Deserialize<EmployeeCreateEvent>(message);
                
                _logger.LogInformation("Create user event received: {Message}", message);
                var result = await _mediator.Send(_mapper.Map<CreateUserAccountCommand>(employeeCreatedEvent));

                _logger.LogInformation("User created with result: {Result}", result);
                await Task.CompletedTask;
            }
        }

        public async Task UnsubscribeAsync()
        {
            _channel?.CloseAsync();
            _channel?.Dispose();
            _connection?.CloseAsync();
            _connection?.Dispose();
        }
    }
}
