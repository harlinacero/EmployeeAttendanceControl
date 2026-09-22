# EmployeeAttendanceControl

## Docker Compose

El proyecto separa la configuración común de la configuración local:

- `docker-compose.yml`: configuración base. Define las imágenes y los `Dockerfile` de los servicios.
- `docker-compose.override.yml`: configuración de desarrollo. Define puertos locales, variables de entorno, volúmenes de datos y dependencias.

Cuando se ejecuta `docker compose` sin indicar archivos, Compose carga automáticamente `docker-compose.yml` y `docker-compose.override.yml` y combina sus configuraciones.

### Desarrollo

Usa ambos archivos mediante el comportamiento automático de Compose:

```powershell
docker compose up --build
```

Para ejecutarlo en segundo plano:

```powershell
docker compose up -d --build
```

Para detener y eliminar los contenedores y la red:

```powershell
docker compose down
```

SQL Server usa el volumen Docker `sqlserver_data` para almacenar los archivos `.mdf` y `.ldf`. En una instalación nueva, prepara los permisos del volumen antes del primer arranque:

```powershell
docker compose down
docker volume create employeeattendancecontrol_sqlserver_data
docker run --rm -v employeeattendancecontrol_sqlserver_data:/var/opt/mssql/data alpine:3.20 chown -R 10001:0 /var/opt/mssql/data
docker compose up -d
```

El volumen conserva los datos aunque se eliminen los contenedores. El directorio `data/sqlServer` anterior no se elimina; se mantiene como respaldo y ya no se monta en SQL Server.

### Inicialización automática de SQL Server

El servicio `ms.sql.employees.db` usa una imagen personalizada basada en SQL Server. Al iniciar, espera a que SQL Server acepte conexiones y ejecuta `initsqldatabase.sql` automáticamente.

El script crea de forma idempotente:

- la base de datos `EmployeesAttendance`;
- la tabla `dbo.Employee`;
- el registro inicial del usuario `hacero`.

Por tanto, se puede iniciar el entorno normalmente:

```powershell
docker compose up -d --build
```

En reinicios posteriores no se duplican la tabla ni el registro inicial. Los cambios realizados después del arranque se conservan en el volumen `sqlserver_data`.

No ejecutes `docker-compose.override.yml` de forma aislada. Es un archivo parcial y no contiene `image` ni `build` para todos los servicios.

También se pueden indicar los archivos explícitamente:

```powershell
docker compose -f docker-compose.yml -f docker-compose.override.yml up --build
```

### Producción

No uses `docker-compose.override.yml` en producción, porque contiene configuración específica del entorno local, como puertos de desarrollo, volúmenes locales y credenciales de ejemplo.

La ejecución base es:

```powershell
docker compose -f docker-compose.yml up -d
```

Sin embargo, antes de desplegar en producción se recomienda crear un archivo adicional, por ejemplo `docker-compose.prod.yml`, para definir:

- variables de entorno y secretos de producción;
- puertos o un proxy inverso;
- políticas de reinicio;
- volúmenes persistentes adecuados;
- etiquetas, límites de recursos y configuración de red;
- imágenes versionadas, en lugar de etiquetas ambiguas como `latest`.

Con ese archivo, la ejecución sería:

```powershell
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

Para detener la configuración de producción:

```powershell
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
```

Los secretos reales no deben guardarse en los archivos Compose ni en el repositorio. Deben proporcionarse mediante variables de entorno, un archivo gestionado fuera del control de versiones o un gestor de secretos.
