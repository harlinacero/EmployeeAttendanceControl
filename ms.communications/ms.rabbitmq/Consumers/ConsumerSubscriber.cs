using Microsoft.Extensions.Logging;

namespace ms.rabbitmq.Consumers
{
    /// <summary>
    /// Subscribes a consumer, trying again until the broker accepts connections or the application stops.
    /// There is no attempt limit: a service that gives up would silently stop receiving messages.
    /// </summary>
    public class ConsumerSubscriber
    {
        private readonly IConsumer _consumer;
        private readonly ILogger<ConsumerSubscriber> _logger;
        private readonly TimeSpan _retryDelay;

        public ConsumerSubscriber(IConsumer consumer, ILogger<ConsumerSubscriber> logger, TimeSpan retryDelay)
        {
            _consumer = consumer;
            _logger = logger;
            _retryDelay = retryDelay;
        }

        public async Task SubscribeAsync(CancellationToken stopping)
        {
            for (var attempt = 1; !stopping.IsCancellationRequested; attempt++)
            {
                try
                {
                    await _consumer.SubscribeAsync();
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not subscribe RabbitMQ consumer {Consumer} on attempt {Attempt}. Trying again in {RetryDelay}",
                        _consumer.GetType().Name, attempt, _retryDelay);
                }

                try
                {
                    await Task.Delay(_retryDelay, stopping);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
