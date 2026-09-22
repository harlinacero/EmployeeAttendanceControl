# ms.users

Servicio de usuarios y autenticacion del sistema de control de asistencia.

## Responsabilidad

`ms.users` administra las cuentas que pueden autenticarse y emite tokens JWT. Tambien permite consultar usuarios y crear cuentas nuevas para operaciones administrativas.

La solucion esta dividida en cuatro proyectos:

- `ms.users.api`: API HTTP, autenticacion JWT, Swagger y consumidor RabbitMQ.
- `ms.users.application`: comandos, consultas, handlers MediatR, DTOs y mapeos.
- `ms.users.domain`: entidad `User` e interfaz `IUserRepository`.
- `ms.users.infraestructure`: conexion y repositorio de Cassandra.

## API

La API se expone en el contenedor por el puerto `80` y, en desarrollo, por `http://localhost:9009`.

| Metodo | Ruta              | Autorizacion | Funcion                                              |
| ------ | ----------------- | ------------ | ---------------------------------------------------- |
| `POST` | `/Authentication` | Publica      | Valida usuario y password y devuelve un JWT.         |
| `GET`  | `/User`           | `admin`      | Devuelve todos los usuarios.                         |
| `POST` | `/User`           | `admin`      | Crea una cuenta con `UserName`, `Password` y `Role`. |

El token incluye el nombre de usuario y el rol. Las APIs de empleados y asistencias validan tokens firmados con la misma clave JWT.

## Cassandra

La configuracion se encuentra bajo `DatabaseSettings`:

```json
{
  "Hostname": "ms.cassandra.db",
  "Keyspace": "kusers",
  "Port": "9042"
}
```

La imagen personalizada de Cassandra ejecuta `cassandra/init.cql` al iniciar. El script crea de forma idempotente el keyspace `kusers`, la tabla `user` y el usuario inicial `admin` con password `1234` y rol `admin`.

El servicio usa `CassandraCSharpDriver` y `Cassandra.Mapping`. La entidad `User` se almacena en `kusers.user` con las columnas `user_username`, `user_pasword` y `user_role`.

## RabbitMQ

`ms.users.api` consume la cola `EmployeeCreateEvent` desde el host configurado en `Communication:EventBus:HostName`.

Cuando `ms.employees` publica un evento de alta de empleado, `UserConsumer` lo recibe y ejecuta `CreateUserAccountCommand` para crear el usuario correspondiente en Cassandra.

## Dependencias

- .NET `6.0` y ASP.NET Core.
- MediatR para comandos y consultas.
- AutoMapper para convertir eventos y comandos.
- JWT Bearer para autenticacion.
- `CassandraCSharpDriver` y `Cassandra.Mapping` para persistencia.
- RabbitMQ.Client para el consumidor de eventos.
- Swashbuckle para Swagger.
- `ms.users.application`, `ms.users.infraestructure`, `ms.users.domain` y `ms.communications/ms.rabbitmq`.

## Ejecucion

```powershell
docker compose up -d --build ms.cassandra.db ms.rabbitmq.bus ms.users.api
docker compose logs -f ms.users.api
```
