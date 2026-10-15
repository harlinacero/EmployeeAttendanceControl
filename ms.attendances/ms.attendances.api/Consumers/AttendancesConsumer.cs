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
using Microsoft.Extensions.Options;
using ms.attendances.application.Extensions;

namespace ms.attendances.api.Consumers
{
    public class AttendancesConsumer : IConsumer
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ILogger<AttendancesConsumer> _logger;
        private readonly SettingsOptions _settingsOptions;
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IChannel _channel;

        public AttendancesConsumer(IMediator mediator, IMapper mapper,
            IOptions<SettingsOptions> configuration, ILogger<AttendancesConsumer> logger,
            IConnectionFactory connectionFactory = null)
        {
            _mediator = mediator;
            _mapper = mapper;
            _settingsOptions = configuration.Value;
            _logger = logger;
            _connectionFactory = connectionFactory ?? new ConnectionFactory()
            {
                HostName = _settingsOptions.Communication.EventBus.HostName,
            };
        }

        public async Task SubscribeAsync()
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
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
            try
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
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message: {Message}", Encoding.UTF8.GetString(e.Body.Span));
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
