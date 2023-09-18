﻿using ms.rabbitmq.Events;

namespace ms.rabbitmq.Producers
{
    public interface IProducer
    {
        void Produce(IRabbitMqEvent rabbitMqEvent);
    }
}
