# ms.communications

Biblioteca compartida de comunicacion entre microservicios. Actualmente contiene la implementacion de eventos y transporte RabbitMQ.

## Estructura

El proyecto principal es `ms.rabbitmq` y se organiza en:

- `Events`: contratos `IRabbitMqEvent` y `EventBase` para eventos serializables.
- `Producers`: `IProducer` y `EventProducer` para publicar mensajes.
- `Consumers`: `IConsumer`, contrato de suscripcion y cancelacion.
- `Middlewares`: `UseRabbitConsumer`, que conecta el ciclo de vida de ASP.NET Core con `Subscribe` y `Unsubscribe`.

## Modelo de comunicacion

La implementacion usa colas RabbitMQ directamente, sin exchanges personalizados:

1. El producer obtiene el nombre del tipo del evento, por ejemplo `EmployeeCreateEvent`.
2. Declara una cola durable con ese nombre.
3. Serializa el evento como JSON.
4. Publica en el exchange por defecto (`""`) usando la cola como routing key.
5. El consumer declara la misma cola y consume sus mensajes.

RabbitMQ se ejecuta en Docker como `ms.rabbitmq.bus`, con AMQP en el puerto `5672` y la interfaz de administracion en `http://localhost:15672`.

## Eventos usados

### `EmployeeCreateEvent`

Publicado por `ms.employees` y consumido por `ms.users`. Se utiliza para crear la cuenta Cassandra asociada al empleado.

### `AttendanceStateChangedEvent`

Publicado por `ms.employees` y consumido por `ms.attendances`. Se utiliza para registrar el cambio de asistencia en MongoDB.

## Dependencias

El proyecto `ms.rabbitmq` usa:

- .NET `6.0`.
- `RabbitMQ.Client` `6.5.0`.
- `Microsoft.Extensions.Configuration` para obtener el hostname.
- `Microsoft.Extensions.Hosting` para integrarse con el ciclo de vida de la API.
- `Microsoft.AspNetCore.Http.Abstractions` para el middleware.

No es una API independiente ni contiene un contenedor propio. Se referencia como proyecto desde `ms.users`, `ms.employees` y `ms.attendances`.

## Configuracion

Cada servicio consumidor o productor debe definir:

```json
{
  "Communication": {
    "EventBus": {
      "HostName": "ms.rabbitmq.bus"
    }
  }
}
```

En ejecucion local fuera de Docker el hostname puede ser `localhost`, siempre que RabbitMQ este publicado en el equipo.
