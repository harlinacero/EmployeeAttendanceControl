using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using ms.rabbitmq.Consumers;
using ms.rabbitmq.Middlewares;

namespace ms.rabbitmq.test.Middlewares
{
    public class ConsumerMiddlewareTests : IDisposable
    {
        private readonly CancellationTokenSource _started;
        private readonly CancellationTokenSource _stopping;
        private readonly Mock<IConsumer> _consumer;
        private readonly Mock<IApplicationBuilder> _application;

        public ConsumerMiddlewareTests()
        {
            _started = new CancellationTokenSource();
            _stopping = new CancellationTokenSource();
            _consumer = new Mock<IConsumer>();

            var lifetime = new Mock<IHostApplicationLifetime>();
            lifetime.SetupGet(item => item.ApplicationStarted).Returns(_started.Token);
            lifetime.SetupGet(item => item.ApplicationStopping).Returns(_stopping.Token);

            var services = new ServiceCollection().AddLogging().AddSingleton(lifetime.Object).BuildServiceProvider();
            _application = new Mock<IApplicationBuilder>();
            _application.SetupGet(item => item.ApplicationServices).Returns(services);
        }

        [Fact]
        public void UseRabbitConsumerShouldSubscribeWhenApplicationStarts()
        {
            // Arrange
            _application.Object.UseRabbitConsumer(_consumer.Object);

            // Act
            _started.Cancel();

            // Assert
            _consumer.Verify(consumer => consumer.SubscribeAsync(), Times.Once);
        }

        [Fact]
        public void UseRabbitConsumerShouldUnsubscribeWhenApplicationStops()
        {
            // Arrange
            _application.Object.UseRabbitConsumer(_consumer.Object);

            // Act
            _stopping.Cancel();

            // Assert
            _consumer.Verify(consumer => consumer.UnsubscribeAsync(), Times.Once);
        }

        public void Dispose()
        {
            _started.Dispose();
            _stopping.Dispose();
        }
    }
}
