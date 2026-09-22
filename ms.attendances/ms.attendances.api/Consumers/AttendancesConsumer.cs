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
        private IModel _channel;

        public AttendancesConsumer(IMediator mediator, IMapper mapper, IConfiguration configuration, ILogger<AttendancesConsumer> logger)
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
            _channel = _connection.CreateModel();

            var queue = nameof(AttendanceStateChangedEvent);

            // Quee storage messages in memory, allow multi connections and not is deleted if don't have any consumer
            _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, null);
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += ReceivedEvent;

            // Pusblish routing_key of message
            _channel.BasicConsume(queue: queue, autoAck: true, consumer: consumer);
            _logger.LogInformation("RabbitMQ consumer subscribed to queue {Queue}", queue);
        }

        private async void ReceivedEvent(object? sender, BasicDeliverEventArgs e)
        {
            if (e.RoutingKey == typeof(AttendanceStateChangedEvent).Name)
            {
                _logger.LogInformation("Received event");
                var message = Encoding.UTF8.GetString(e.Body.Span);
                var attendanceStateChangedEvent = JsonSerializer.Deserialize<AttendanceStateChangedEvent>(message);

                _logger.LogInformation("Send Create attendance ", attendanceStateChangedEvent);
                var result = await _mediator.Send(new CreateAttendanceCommand(attendanceStateChangedEvent?.UserName,
                    _mapper.Map<CreateAttendanceRequest>(attendanceStateChangedEvent)));
            }
        }

        public void Unsubscribe()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
