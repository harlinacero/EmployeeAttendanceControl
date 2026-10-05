using System.Reflection;
using System.Text;
using System.Text.Json;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client.Events;
using ms.users.api.Consumers;
using ms.users.api.Events;
using ms.users.application.Commands;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using ms.users.application.Extensions;
using ms.rabbitmq.Settings;

namespace ms.users.api.test.Consumers
{
    public class UserConsumerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<UserConsumer>> _loggerMock;
        private readonly Mock<IReadOnlyBasicProperties> _propertiesMock;
        private readonly Mock<IConnectionFactory> _connectionFactoryMock;
        private readonly Mock<IConnection> _connectionMock;
        private readonly Mock<IChannel> _channelMock;
        private readonly IOptions<SettingsOptions> _options;

        public UserConsumerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<UserConsumer>>();
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

            var consumer = new UserConsumer(_mediatorMock.Object, _mapperMock.Object, _options, _loggerMock.Object, _connectionFactoryMock.Object);

            // Act
            await consumer.SubscribeAsync();

            // Assert
            _channelMock.Verify(c => c.QueueDeclareAsync(
                It.Is<string>(q => q == nameof(EmployeeCreateEvent)),
                It.Is<bool>(d => d == true),
                It.Is<bool>(e => e == false),
                It.Is<bool>(a => a == false),
                It.IsAny<IDictionary<string, object>>()), Times.Once);

            _loggerMock.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("RabbitMQ consumer subscribed to queue")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ReceivedEventValidEmployeeCreateEventInvokesMediatorSend()
        {
            // Arrange
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserAccountCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync("ok");

            _mapperMock.Setup(m => m.Map<CreateUserAccountCommand>(It.IsAny<EmployeeCreateEvent>()))
                      .Returns<EmployeeCreateEvent>(ev => new CreateUserAccountCommand(ev.UserName, ev.Password, ev.Role));


            var consumer = new UserConsumer(_mediatorMock.Object, _mapperMock.Object, _options, _loggerMock.Object, _connectionFactoryMock.Object);

            var employeeEvent = new EmployeeCreateEvent { UserName = "user1", Password = "pass", Role = "role" };
            var payload = JsonSerializer.Serialize(employeeEvent);
            var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(payload));


            _propertiesMock.Setup(p => p.Headers).Returns(new Dictionary<string, object>());
            var eventArgs = new BasicDeliverEventArgs(It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<string>(),
                nameof(EmployeeCreateEvent), _propertiesMock.Object, body, It.IsAny<CancellationToken>());


            var method = typeof(UserConsumer).GetMethod("ReceivedEvent", BindingFlags.Instance | BindingFlags.NonPublic);

            // Act
            var task = (Task)method.Invoke(consumer, new object[] { null, eventArgs });
            await task;

            // Assert
            _mediatorMock.Verify(m => m.Send(It.Is<CreateUserAccountCommand>(c => c.UserName == "user1" && c.Password == "pass" && c.Role == "role"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReceivedEventInvalidJsonDoesNotInvokeMediator()
        {
            // Arrange

            var consumer = new UserConsumer(_mediatorMock.Object, _mapperMock.Object, _options, _loggerMock.Object, _connectionFactoryMock.Object);

            var employeeEvent = "{}";
            var payload = JsonSerializer.Serialize(employeeEvent);
            var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(payload));

            _propertiesMock.Setup(p => p.Headers).Returns(new Dictionary<string, object>());
            var eventArgs = new BasicDeliverEventArgs(It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<string>(),
                nameof(EmployeeCreateEvent), _propertiesMock.Object, body, It.IsAny<CancellationToken>());


            var method = typeof(UserConsumer).GetMethod("ReceivedEvent", BindingFlags.Instance | BindingFlags.NonPublic);

            // Act
            var task = (Task)method.Invoke(consumer, new object[] { null, eventArgs });
            await task;

            // Assert
            _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserAccountCommand>(), It.IsAny<CancellationToken>()), Times.Never);
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to deserialize message")),
                It.IsAny<JsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ReceivedEventWrongRoutingKeyDoesNotInvokeMediator()
        {
            // Arrange
            var consumer = new UserConsumer(_mediatorMock.Object, _mapperMock.Object, _options, _loggerMock.Object, _connectionFactoryMock.Object);

            var employeeEvent = new EmployeeCreateEvent { UserName = "user2", Password = "p2", Role = "r2" };
            var payload = JsonSerializer.Serialize(employeeEvent);
            var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(payload));

            _propertiesMock.Setup(p => p.Headers).Returns(new Dictionary<string, object>());

            var eventArgs = new BasicDeliverEventArgs(It.IsAny<string>(), It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<string>(),
                "SomeOtherEvent", _propertiesMock.Object, body, It.IsAny<CancellationToken>());


            var method = typeof(UserConsumer).GetMethod("ReceivedEvent", BindingFlags.Instance | BindingFlags.NonPublic);

            // Act
            var task = (Task)method.Invoke(consumer, new object[] { null, eventArgs });
            await task;

            // Assert
            _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserAccountCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UnsusbcribeAsyncSucceeds()
        {
            // Arrange
            _connectionFactoryMock
                .Setup(f => f.CreateConnectionAsync())
                .ReturnsAsync(_connectionMock.Object);
            _connectionMock
                .Setup(c => c.CreateChannelAsync())
                .ReturnsAsync(_channelMock.Object);
            var consumer = new UserConsumer(_mediatorMock.Object, _mapperMock.Object, _options, _loggerMock.Object, _connectionFactoryMock.Object);
            await consumer.SubscribeAsync();
            // Act
            await consumer.UnsubscribeAsync();
            // Assert
            Assert.NotNull(consumer);

        }
    }
}
