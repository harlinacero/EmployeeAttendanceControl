using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ms.attendances.api.Consumers;
using ms.attendances.api.Events;
using ms.attendances.application.Commands;
using ms.attendances.application.Extensions;
using ms.rabbitmq.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ms_attendances.api.test.Consumers
{
    public class AttendancesConsumerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<AttendancesConsumer>> _loggerMock;
        private readonly Mock<IReadOnlyBasicProperties> _propertiesMock;
        private readonly Mock<IConnectionFactory> _connectionFactoryMock;
        private readonly Mock<IConnection> _connectionMock;
        private readonly Mock<IChannel> _channelMock;
        private readonly IOptions<SettingsOptions> _options;

        public AttendancesConsumerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<AttendancesConsumer>>();
            _propertiesMock = new Mock<IReadOnlyBasicProperties>();
            _connectionFactoryMock = new Mock<IConnectionFactory>();
            _connectionMock = new Mock<IConnection>();
            _channelMock = new Mock<IChannel>();

            _options = Options.Create(new SettingsOptions
            {
                Communication = new Communication
                {
                    EventBus = new EventBus
                    {
                        HostName = "localhost"
                    }
                }
            });
        }

        [Fact]
        public async Task SubscribeAsyncCreatesQueueAndConsumer()
        {
            // Arrange
            _connectionMock
                .Setup(c => c.CreateChannelAsync())
                .ReturnsAsync(_channelMock.Object);
            _connectionFactoryMock
                .Setup(f => f.CreateConnectionAsync())
                .ReturnsAsync(_connectionMock.Object);
            var consumer = new AttendancesConsumer(
                _mediatorMock.Object,
                _mapperMock.Object,
                _options,
                _loggerMock.Object,
                _connectionFactoryMock.Object);

            // Act
            await consumer.SubscribeAsync();

            // Assert
            _channelMock.Verify(c => c.QueueDeclareAsync(
                nameof(AttendanceStateChangedEvent),
                true, false, false, null), Times.Once);
        }

        [Fact]
        public async Task ReceivedEventProcessesAttendanceStateChangedEvent()
        {
            // Arrange
            var consumer = new AttendancesConsumer(
                _mediatorMock.Object,
                _mapperMock.Object,
                _options,
                _loggerMock.Object,
                _connectionFactoryMock.Object);

            var eventMessage = new AttendanceStateChangedEvent
            {
                UserName = "testuser",
                Attendance = true,
                Date = DateTime.UtcNow,
                FullName = "Test User",
                Notes = "Test notes"
            };

            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventMessage));
            var body = new ReadOnlyMemory<byte>(messageBody);
            _propertiesMock.Setup(p => p.Headers).Returns(new Dictionary<string, object>());

            var eventArgs = new BasicDeliverEventArgs(It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<string>(),
        nameof(AttendanceStateChangedEvent), _propertiesMock.Object, body, It.IsAny<CancellationToken>());

            // Act
            var method = typeof(AttendancesConsumer).GetMethod("ReceivedEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)method.Invoke(consumer, new object[] { null, eventArgs });

            // Assert
            _mediatorMock.Verify(m => m.Send(It.IsAny<CreateAttendanceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReceivedEventInvalidJsonDoesNotInvokeMediator()
        {
            // Arrange
            var consumer = new AttendancesConsumer(
                _mediatorMock.Object,
                _mapperMock.Object,
                _options,
                _loggerMock.Object,
                _connectionFactoryMock.Object);
            var invalidJson = "{}";
            var messageBody = JsonSerializer.Serialize(invalidJson);
            var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(messageBody));

            _propertiesMock.Setup(p => p.Headers).Returns(new Dictionary<string, object>());
            var eventArgs = new BasicDeliverEventArgs(It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<string>(),
                nameof(AttendanceStateChangedEvent), _propertiesMock.Object, body, It.IsAny<CancellationToken>());

            var method = typeof(AttendancesConsumer).GetMethod("ReceivedEvent", BindingFlags.NonPublic | BindingFlags.Instance);

            // act
            var task = (Task)method.Invoke(consumer, [null, eventArgs]);
            await task;

            // Assert
            _mediatorMock.Verify(m => m.Send(It.IsAny<CreateAttendanceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
            _loggerMock.Verify(l => l.Log(
                It.Is<LogLevel>(ll => ll == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to deserialize message")),
                It.IsAny<JsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ReceivedEventWrongRoutingKeyDoesNotInvokeMediator()
        {
            // Arrange
            var consumer = new AttendancesConsumer(
                _mediatorMock.Object,
                _mapperMock.Object,
                _options,
                _loggerMock.Object,
                _connectionFactoryMock.Object);
            var eventMessage = new AttendanceStateChangedEvent
            {
                UserName = "testuser",
                Attendance = true,
                Date = DateTime.UtcNow,
                FullName = "Test User",
                Notes = "Test notes"
            };
            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventMessage));
            var body = new ReadOnlyMemory<byte>(messageBody);
            _propertiesMock.Setup(p => p.Headers).Returns(new Dictionary<string, object>());
            var eventArgs = new BasicDeliverEventArgs(It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<string>(),
                "SomeOtherEvent", _propertiesMock.Object, body, It.IsAny<CancellationToken>());
            var method = typeof(AttendancesConsumer).GetMethod("ReceivedEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            // Act
            var task = (Task)method.Invoke(consumer, new object[] { null, eventArgs });
            await task;
            // Assert
            _mediatorMock.Verify(m => m.Send(It.IsAny<CreateAttendanceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UnsubscribeAsyncClosesConnectionAndChannel()
        {
            // Arrange
            _connectionFactoryMock
                .Setup(f => f.CreateConnectionAsync())
                .ReturnsAsync(_connectionMock.Object);
            _connectionMock
                .Setup(c => c.CreateChannelAsync())
                .ReturnsAsync(_channelMock.Object);

            var consumer = new AttendancesConsumer(
                _mediatorMock.Object,
                _mapperMock.Object,
                _options,
                _loggerMock.Object,
                _connectionFactoryMock.Object);
            // Act
            await consumer.SubscribeAsync();
            await consumer.UnsubscribeAsync();
            // Assert
            Assert.NotNull(consumer);
        }
    }
}
