using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ms.rabbitmq.Settings
{
    public class EventBus
    {
        [Required]
        public string HostName { get; set; }
    }
}