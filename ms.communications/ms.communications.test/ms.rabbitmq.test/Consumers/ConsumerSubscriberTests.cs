using Microsoft.Extensions.Logging;
using Moq;
using ms.rabbitmq.Consumers;

namespace ms.rabbitmq.test.Consumers
{
    public class ConsumerSubscriberTests
    {
        private readonly Mock<IConsumer> _consumer;
        private readonly Mock<ILogger<ConsumerSubscriber>> _logger;
        private readonly ConsumerSubscriber _subscriber;

        public ConsumerSubscriberTests()
        {
            _consumer = new Mock<IConsumer>();
            _logger = new Mock<ILogger<ConsumerSubscriber>>();
            _subscriber = new ConsumerSubscriber(_consumer.Object, _logger.Object, TimeSpan.FromMilliseconds(1));
        }

        [Fact]
        public async Task SubscribeAsyncShouldSubscribeOnceWhenBrokerIsAvailable()
        {
            // Act
            await _subscriber.SubscribeAsync(CancellationToken.None);

            // Assert
            _consumer.Verify(consumer => consumer.SubscribeAsync(), Times.Once);
        }

        [Fact]
        public async Task SubscribeAsyncShouldRetryUntilBrokerIsAvailable()
        {
            // Arrange
            FailTwiceThenSucceed();

            // Act
            await _subscriber.SubscribeAsync(CancellationToken.None);

            // Assert
            _consumer.Verify(consumer => consumer.SubscribeAsync(), Times.Exactly(3));
        }

        [Fact]
        public async Task SubscribeAsyncShouldLogEachFailedAttempt()
        {
            // Arrange
            FailTwiceThenSucceed();

            // Act
            await _subscriber.SubscribeAsync(CancellationToken.None);

            // Assert
            _logger.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(exception => exception is InvalidOperationException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SubscribeAsyncShouldStopRetryingWhenApplicationStops()
        {
            // Arrange
            using var stopping = new CancellationTokenSource();
            var calls = 0;
            _consumer.Setup(consumer => consumer.SubscribeAsync()).Returns(() =>
            {
                if (++calls == 3)
                {
                    stopping.Cancel();
                }

                return Task.FromException(new InvalidOperationException("broker unreachable"));
            });

            // Act
            await _subscriber.SubscribeAsync(stopping.Token);

            // Assert
            Assert.Equal(3, calls);
        }

        [Fact]
        public async Task SubscribeAsyncShouldNotSubscribeWhenAlreadyStopping()
        {
            // Arrange
            using var stopping = new CancellationTokenSource();
            stopping.Cancel();

            // Act
            await _subscriber.SubscribeAsync(stopping.Token);

            // Assert
            _consumer.Verify(consumer => consumer.SubscribeAsync(), Times.Never);
        }

        private void FailTwiceThenSucceed()
        {
            _consumer.SetupSequence(consumer => consumer.SubscribeAsync())
                .ThrowsAsync(new InvalidOperationException("broker unreachable"))
                .ThrowsAsync(new InvalidOperationException("broker unreachable"))
                .Returns(Task.CompletedTask);
        }
    }
}
