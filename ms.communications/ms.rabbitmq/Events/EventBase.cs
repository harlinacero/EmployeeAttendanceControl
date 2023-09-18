using System.Text.Json;

namespace ms.rabbitmq.Events
{
    public abstract class EventBase
    {
        public Guid EventId => Guid.NewGuid();
    }

}
