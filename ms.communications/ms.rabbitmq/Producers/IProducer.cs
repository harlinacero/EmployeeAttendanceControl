using ms.rabbitmq.Events;

namespace ms.rabbitmq.Producers
{
    public interface IProducer
    {
        Task Produce(IRabbitMqEvent rabbitMqEvent);
    }
}
