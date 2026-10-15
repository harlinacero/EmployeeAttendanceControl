# ms.communications

Shared communication library between microservices. It currently contains the RabbitMQ events and transport implementation.

## Structure

The main project is `ms.rabbitmq` and is organized into:

- `Events`: `IRabbitMqEvent` and `EventBase` contracts for serializable events.
- `Producers`: `IProducer` and `EventProducer` for publishing messages.
- `Consumers`: `IConsumer`, subscription and cancellation contract.
- `Middlewares`: `UseRabbitConsumer`, which connects the ASP.NET Core lifecycle to `Subscribe` and `Unsubscribe`.

## Communication model

The implementation uses RabbitMQ queues directly, without custom exchanges:

1. The producer gets the event type's name, e.g. `EmployeeCreateEvent`.
2. It declares a durable queue with that name.
3. It serializes the event as JSON.
4. It publishes to the default exchange (`""`) using the queue as the routing key.
5. The consumer declares the same queue and consumes its messages.

RabbitMQ runs in Docker as `ms.rabbitmq.bus`, with AMQP on port `5672` and the management UI at `http://localhost:15672`.

## Events used

### `EmployeeCreateEvent`

Published by `ms.employees` and consumed by `ms.users`. Used to create the Cassandra account associated with the employee.

### `AttendanceStateChangedEvent`

Published by `ms.employees` and consumed by `ms.attendances`. Used to record the attendance change in MongoDB.

## Dependencies

The `ms.rabbitmq` project uses:

- .NET `10.0`.
- `RabbitMQ.Client`.
- `Microsoft.Extensions.Configuration` to obtain the hostname.
- `Microsoft.Extensions.Hosting` to integrate with the API lifecycle.
- `Microsoft.AspNetCore.Http.Abstractions` for the middleware.

Package versions are managed centrally via `Directory.Packages.props` at the repository root (NuGet Central Package Management).

It is not a standalone API and does not have its own container. It is referenced as a project from `ms.users`, `ms.employees`, and `ms.attendances`.

## Configuration

Each consuming or producing service must define:

```json
{
  "Communication": {
    "EventBus": {
      "HostName": "ms.rabbitmq.bus"
    }
  }
}
```

This setting is bound via the `IOptions` pattern (`CommunicationConfigExtension`) in the consuming services. When running locally outside of Docker, the hostname can be `localhost`, as long as RabbitMQ is published on the machine.
