using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ms.rabbitmq.Consumers;

namespace ms.rabbitmq.Middlewares
{
    public static class ConsumerMiddleware
    {
        private static IConsumer Consumer { get; set; }
        public static IApplicationBuilder UseRabbitConsumer(this IApplicationBuilder app, IConsumer consumer)
        {
            Consumer = consumer;
            IHostApplicationLifetime lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(OnStarted);
            lifetime.ApplicationStopping.Register(OnStopping);

            return app;
        }


        private static void OnStarted() => Consumer.Subscribe();
        private static void OnStopping() => Consumer.Unsubscribe();
    }
}
