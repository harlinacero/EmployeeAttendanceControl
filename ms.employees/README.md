# ms.employees

Servicio de empleados. Mantiene la informacion laboral en SQL Server, expone operaciones administrativas y coordina el estado de asistencia con los servicios de usuarios y asistencias.

## Responsabilidad

La solucion esta dividida en cuatro proyectos:

- `ms.employees.api`: API HTTP, JWT, Swagger y configuracion de Refit/RabbitMQ.
- `ms.employees.application`: comandos, consultas, handlers MediatR, eventos y comunicacion HTTP.
- `ms.employees.domain`: entidad `Employee` y contratos de repositorio.
- `ms.employees.infraestucture`: contexto Dapper y consultas SQL Server.

## API

En desarrollo se expone por `http://localhost:9010`.

| Metodo | Ruta                                            | Autorizacion | Funcion                                                    |
| ------ | ----------------------------------------------- | ------------ | ---------------------------------------------------------- |
| `GET`  | `/Employees/GetAllEmployees`                    | `admin`      | Devuelve todos los empleados.                              |
| `POST` | `/Employees/CreateEmployee`                     | `admin`      | Crea un empleado y publica su evento de alta.              |
| `PUT`  | `/Employees/UpdateAttendanceState/{attendance}` | JWT          | Actualiza el estado de asistencia del usuario autenticado. |

`CreateEmployee` recibe `UserName`, `FirstName`, `LastName`, `Password` y `Role`. `UpdateAttendanceState` recibe las notas en el cuerpo y obtiene el usuario desde el claim `NameIdentifier` del JWT.

## SQL Server

La API usa Dapper y `System.Data.SqlClient`. En Docker la conexion apunta a `ms.sql.employees.db:1433`, con la base `EmployeesAttendance` y el usuario `sa`.

El archivo `initsqldatabase.sql`, ejecutado por la imagen personalizada de SQL Server, crea la base, la tabla `dbo.Employee` y el registro inicial. Los archivos se conservan en el volumen Docker `sqlserver_data`.

## Comunicacion con otros servicios

### Refit hacia ms.attendances

Al actualizar la asistencia, el handler llama a:

```text
GET http://ms.attendances.api:80/Attendances/GetAllAttendances?userName={userName}
```

Usa el header `Authorization` recibido en la peticion. El numero de asistencias se incorpora a las notas guardadas en SQL Server.

### Eventos RabbitMQ

- Publica `EmployeeCreateEvent` al crear un empleado. `ms.users.api` consume este evento para crear la cuenta de usuario.
- Publica `AttendanceStateChangedEvent` al actualizar el estado de asistencia. `ms.attendances.api` consume este evento para registrar la asistencia en MongoDB.

El host de RabbitMQ se configura con `Communication:EventBus:HostName`; en Docker es `ms.rabbitmq.bus`.

## Dependencias

- .NET `6.0` y ASP.NET Core.
- MediatR, AutoMapper y JWT Bearer.
- Dapper y `System.Data.SqlClient` para SQL Server.
- Refit para la llamada HTTP a `ms.attendances.api`.
- RabbitMQ.Client mediante `ms.communications/ms.rabbitmq`.
- Swashbuckle para Swagger.

## Ejecucion

```powershell
docker compose up -d --build ms.sql.employees.db ms.rabbitmq.bus ms.attendances.api ms.employees.api
docker compose logs -f ms.employees.api
```
