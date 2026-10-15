# ms.users

User and authentication service for the attendance control system.

## Responsibility

`ms.users` manages the accounts that can authenticate and issues JWT tokens. It also allows querying users and creating new accounts for administrative operations.

The solution is split into four projects:

- `ms.users.api`: HTTP API, JWT authentication, Swagger, and RabbitMQ consumer.
- `ms.users.application`: commands, queries, MediatR handlers, DTOs, and mappings.
- `ms.users.domain`: `User` entity and `IUserRepository` interface.
- `ms.users.infraestructure`: Cassandra connection and repository.

## API

The API is exposed in the container on port `80` and, in development, at `http://localhost:9009`.

| Method | Route             | Authorization | Function                                               |
| ------ | ----------------- | -------------- | ------------------------------------------------------- |
| `POST` | `/Authentication` | Public         | Validates the username and password and returns a JWT.  |
| `GET`  | `/User`           | `admin`        | Returns all users.                                       |
| `POST` | `/User`           | `admin`        | Creates an account with `UserName`, `Password`, and `Role`. |

The token includes the username and role. The employees and attendances APIs validate tokens signed with the same JWT key.

## Cassandra

The configuration is found under `DatabaseSettings` and bound via the `IOptions` pattern (`CassandraCluster`, `UsersContext`):

```json
{
  "Hostname": "ms.cassandra.db",
  "Keyspace": "kusers",
  "Port": "9042"
}
```

The custom Cassandra image runs `cassandra/init.cql` on startup. The script idempotently creates the `kusers` keyspace, the `user` table, and the initial `admin` user with password `1234` and role `admin`.

The service uses `CassandraCSharpDriver` and `Cassandra.Mapping`. The `User` entity is stored in `kusers.user` with the columns `user_username`, `user_pasword`, and `user_role`.

## RabbitMQ

`ms.users.api` consumes the `EmployeeCreateEvent` queue from the host configured in `Communication:EventBus:HostName`.

When `ms.employees` publishes an employee creation event, `UserConsumer` receives it and executes `CreateUserAccountCommand` to create the corresponding user in Cassandra.

## Dependencies

- .NET `10.0` and ASP.NET Core.
- MediatR for commands and queries.
- AutoMapper to convert events and commands.
- JWT Bearer for authentication.
- `CassandraCSharpDriver` and `Cassandra.Mapping` for persistence.
- RabbitMQ.Client for the event consumer.
- Swashbuckle for Swagger.
- `ms.users.application`, `ms.users.infraestructure`, `ms.users.domain`, and `ms.communications/ms.rabbitmq`.

Package versions are managed centrally via `Directory.Packages.props` at the repository root (NuGet Central Package Management).

## Tests

The `ms.users.test` folder contains xUnit test projects, one per layer:

- `ms.users.api.test`
- `ms.users.application.test`
- `ms.users.infraestructure.test`

They use `xunit`, `Moq`, and `coverlet.collector`. Run them with:

```powershell
dotnet test ms.users
```

## Running

```powershell
docker compose up -d --build ms.cassandra.db ms.rabbitmq.bus ms.users.api
docker compose logs -f ms.users.api
```
