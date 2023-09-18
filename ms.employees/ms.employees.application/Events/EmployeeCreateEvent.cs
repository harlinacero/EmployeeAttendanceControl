using ms.rabbitmq.Events;
using System.Text.Json;

namespace ms.employees.application.Events
{
    public class EmployeeCreateEvent : EventBase, IRabbitMqEvent
    {

        public string UserName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

        public string Serialize() => JsonSerializer.Serialize(this);
    }
}