namespace ms.rabbitmq.Consumers
{
    public interface IConsumer
    {
        Task SubscribeAsync();
        Task UnsubscribeAsync();
    }
}
