using ms.rabbitmq.Events;
using System.Text.Json;

namespace ms.employees.application.Events
{
    public class AttendanceStateChangedEvent : EventBase, IRabbitMqEvent
    {
        

        public string UserName { get; set; }
        public string FullName { get; set; }
        public DateTime Date { get; set; }
        public bool Attendance { get; set; }
        public string Notes { get; set; }

        public string Serialize() => JsonSerializer.Serialize(this);
    }
}