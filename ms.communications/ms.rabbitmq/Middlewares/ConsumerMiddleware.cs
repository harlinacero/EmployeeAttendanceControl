using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ms.rabbitmq.Consumers;

namespace ms.rabbitmq.Middlewares
{
    public static class ConsumerMiddleware
    {
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        public static IApplicationBuilder UseRabbitConsumer(this IApplicationBuilder app, IConsumer consumer)
        {
            IHostApplicationLifetime lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            var logger = app.ApplicationServices.GetRequiredService<ILogger<ConsumerSubscriber>>();
            var subscriber = new ConsumerSubscriber(consumer, logger, RetryDelay);

            lifetime.ApplicationStarted.Register(() => _ = subscriber.SubscribeAsync(lifetime.ApplicationStopping));
            lifetime.ApplicationStopping.Register(() => _ = consumer.UnsubscribeAsync());

            return app;
        }
    }
}
