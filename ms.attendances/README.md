# ms.attendances

Servicio de historico de asistencias. Expone consultas y altas de asistencias y persiste los registros en MongoDB.

## Responsabilidad

La solucion esta dividida en cuatro proyectos:

- `ms.attendances.api`: API HTTP, JWT, Swagger y consumidor RabbitMQ.
- `ms.attendances.application`: comandos, consultas, handlers y DTOs.
- `ms.attendances.domain`: entidad `AttendanceRecord` y contrato de repositorio.
- `ms.attendances.infraestucture`: contexto, mapeos y repositorio MongoDB.

## API

En desarrollo se expone por `http://localhost:9020`.

| Metodo | Ruta                                                 | Autorizacion | Funcion                                              |
| ------ | ---------------------------------------------------- | ------------ | ---------------------------------------------------- |
| `GET`  | `/Attendances/GetAllAttendances?userName={userName}` | JWT          | Devuelve el historico del usuario.                   |
| `POST` | `/Attendances/CreateAttendance`                      | JWT          | Registra una asistencia para el usuario autenticado. |

`CreateAttendance` obtiene el nombre de usuario del claim `NameIdentifier` y recibe un `CreateAttendanceRequest` con los datos de la asistencia.

## MongoDB

La conexion en Docker se configura asi:

```text
MongoDB: mongodb://ms.mongo.attendances.db:27017
Database: DbHistoricalAttendance
Collection: Attendances
```

El servicio usa `MongoDB.Driver`. La coleccion se obtiene desde la configuracion y los datos se conservan en el volumen Docker `mongodb_data`.

## RabbitMQ

`AttendancesConsumer` consume la cola `AttendanceStateChangedEvent` desde `ms.rabbitmq.bus`.

Cuando `ms.employees.api` cambia el estado de asistencia, publica ese evento. El consumidor lo transforma en `CreateAttendanceCommand` y guarda el registro en MongoDB.

## Seguridad

Todos los endpoints de asistencias requieren un JWT valido. La clave y la duracion del token se configuran en `Authentication:JWT` y deben coincidir con las APIs que emiten y validan los tokens.

## Dependencias

- .NET `6.0` y ASP.NET Core.
- MediatR y AutoMapper.
- JWT Bearer para proteger los endpoints.
- MongoDB.Driver para persistencia.
- RabbitMQ.Client mediante `ms.communications/ms.rabbitmq`.
- Swashbuckle para Swagger.

## Ejecucion

```powershell
docker compose up -d --build ms.mongo.attendances.db ms.rabbitmq.bus ms.attendances.api
docker compose logs -f ms.attendances.api
```
