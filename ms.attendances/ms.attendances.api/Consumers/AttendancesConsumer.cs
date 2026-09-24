using AutoMapper;
using MediatR;
using ms.rabbitmq.Consumers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text;
using ms.attendances.application.Request;
using ms.attendances.application.Commands;
using ms.attendances.api.Events;

namespace ms.attendances.api.Consumers
{
    public class AttendancesConsumer : IConsumer
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AttendancesConsumer> _logger;
        private IConnection _connection;
        private IChannel _channel;

        public AttendancesConsumer(IMediator mediator, IMapper mapper, IConfiguration configuration, ILogger<AttendancesConsumer> logger)
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

            var queue = nameof(AttendanceStateChangedEvent);

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
            if (e.RoutingKey == nameof(AttendanceStateChangedEvent))
            {
                _logger.LogInformation("Received event");
                var message = Encoding.UTF8.GetString(e.Body.Span);
                var attendanceStateChangedEvent = JsonSerializer.Deserialize<AttendanceStateChangedEvent>(message);

                _logger.LogInformation("Create attendance {AttendanceStateChangedEvent}", attendanceStateChangedEvent);
                var createAttendanceCommand = new CreateAttendanceCommand(attendanceStateChangedEvent?.UserName,
                    _mapper.Map<CreateAttendanceRequest>(attendanceStateChangedEvent));
                var result = await _mediator.Send(createAttendanceCommand);

                _logger.LogInformation("Attendance updated with result: {Result}", result);
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
