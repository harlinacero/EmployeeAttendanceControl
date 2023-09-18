using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ms.rabbitmq.Consumers;

namespace ms.rabbitmq.Middlewares
{
    public static class ConsumerMiddleware
    {

        public static IApplicationBuilder UseRabbitConsumer(this IApplicationBuilder app)
        {
            IConsumer consumer = app.ApplicationServices.GetRequiredService<IConsumer>();
            IHostApplicationLifetime lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() => OnStarted(consumer));
            lifetime.ApplicationStopping.Register(() => OnStopping(consumer));

            return app;
        }


        private static void OnStarted(IConsumer consumer) => consumer.Subscribe();
        private static void OnStopping(IConsumer consumer) => consumer.Unsubscribe();
    }
}
