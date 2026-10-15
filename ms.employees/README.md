# ms.employees

Employee service. Keeps employment information in SQL Server, exposes administrative operations, and coordinates attendance state with the users and attendances services.

## Responsibility

The solution is split into four projects:

- `ms.employees.api`: HTTP API, JWT, Swagger, and Refit/RabbitMQ configuration.
- `ms.employees.application`: commands, queries, MediatR handlers, events, and HTTP communication.
- `ms.employees.domain`: `Employee` entity and repository contracts.
- `ms.employees.infraestucture`: Dapper context and SQL Server queries.

## API

In development it is exposed at `http://localhost:9010`.

| Method | Route                                            | Authorization | Function                                                   |
| ------ | ----------------------------------------------- | -------------- | ------------------------------------------------------------ |
| `GET`  | `/Employees/GetAllEmployees`                    | `admin`        | Returns all employees.                                       |
| `POST` | `/Employees/CreateEmployee`                     | `admin`        | Creates an employee and publishes its creation event.        |
| `PUT`  | `/Employees/UpdateAttendanceState/{attendance}` | JWT            | Updates the attendance state of the authenticated user.      |

`CreateEmployee` receives `UserName`, `FirstName`, `LastName`, `Password`, and `Role`. `UpdateAttendanceState` receives the notes in the body and obtains the user from the `NameIdentifier` claim of the JWT.

## SQL Server

The API uses Dapper and `System.Data.SqlClient`. In Docker, the connection points to `ms.sql.employees.db:1433`, with the `EmployeesAttendance` database and the `sa` user. The connection string is bound via the `IOptions` pattern (`EmployeesDapperContext`).

The `initsqldatabase.sql` file, run by the custom SQL Server image, creates the database, the `dbo.Employee` table, and the initial record. The files are kept in the `sqlserver_data` Docker volume.

## Communication with other services

### Refit towards ms.attendances

When updating attendance, the handler calls:

```text
GET http://ms.attendances.api:80/Attendances/GetAllAttendances?userName={userName}
```

It uses the `Authorization` header received in the request. The number of attendances is included in the notes saved in SQL Server.

### RabbitMQ events

- Publishes `EmployeeCreateEvent` when an employee is created. `ms.users.api` consumes this event to create the user account.
- Publishes `AttendanceStateChangedEvent` when the attendance state is updated. `ms.attendances.api` consumes this event to record the attendance in MongoDB.

The RabbitMQ host is configured via `Communication:EventBus:HostName`; in Docker it is `ms.rabbitmq.bus`.

## Dependencies

- .NET `10.0` and ASP.NET Core.
- MediatR, AutoMapper, and JWT Bearer.
- Dapper and `System.Data.SqlClient` for SQL Server.
- Refit for the HTTP call to `ms.attendances.api`.
- RabbitMQ.Client via `ms.communications/ms.rabbitmq`.
- Swashbuckle for Swagger.

Package versions are managed centrally via `Directory.Packages.props` at the repository root (NuGet Central Package Management).

## Tests

The `ms.employees.test` folder contains xUnit test projects, one per layer:

- `ms.employees.api.test`
- `ms.employees.application.test`
- `ms.employees.infraestucture.test`

They use `xunit`, `Moq`, and `coverlet.collector`. Run them with:

```powershell
dotnet test ms.employees
```

## Running

```powershell
docker compose up -d --build ms.sql.employees.db ms.rabbitmq.bus ms.attendances.api ms.employees.api
docker compose logs -f ms.employees.api
```
