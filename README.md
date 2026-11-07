# EmployeeAttendanceControl

Distributed system for managing employees, users, and historical attendance records.

## General architecture

The project is organized as a .NET 10 microservices solution. Each service has its own responsibilities and storage, while RabbitMQ propagates events between them.

### Components

- **ms.gateway**: API gateway (Ocelot). The single entry point for clients: it forwards each request to the service that handles it, after checking the JWT with `ms.users`' public key and, for admin-only operations, the `admin` role. It has no persistence. See [`ms.gateway/README.md`](ms.gateway/README.md).
- **ms.users**: authentication, sessions, users, and token issuance. It is the only service that can issue tokens. Uses Cassandra.
- **ms.employees**: employee management and current attendance state. Uses SQL Server.
- **ms.attendances**: attendance history. Uses MongoDB.
- **ms.assistant**: AI assistant (Claude). Answers attendance questions in natural language by calling the other services with the caller's token. Read-only, no persistence.
- **ms.communications**: shared library for publishing and consuming RabbitMQ events.
- **RabbitMQ**: message bus for asynchronous communication.
- **SQL Server, Cassandra, and MongoDB**: storage specialized per service.

Each API is internally split into `api`, `application`, `domain`, and `infraestructure` layers when it needs persistence. The APIs expose HTTP and Swagger, and use JWT for authentication and authorization.

### Authentication

- **Sign-in** (`POST /Authentication`) returns an access token valid for 15 minutes and a single-use refresh token. `POST /Authentication/Refresh` exchanges the refresh token for a new pair; presenting one that was already used revokes that session. `POST /Authentication/Logout` ends the current session only. A person can have several sessions, which end after 7 days without renewal or 30 days after sign-in.
- **Only ms.users can issue tokens.** It signs them with RS256 using a private key kept in its `users_keys` volume, and publishes the public key at `/.well-known/openid-configuration` and `/.well-known/jwks.json`. The gateway and the other services download that key (`Authentication:JWT:Authority`) and can check tokens, but hold nothing that could create one.
- **Passwords** are stored only as PBKDF2 hashes, and are never returned, logged, or sent through RabbitMQ.

How a token is issued and checked:

```mermaid
sequenceDiagram
	autonumber
	actor Client as Client
	participant Gateway as ms.gateway.api
	participant Users as ms.users.api
	participant Service as Any other service

	Client->>Gateway: POST /Authentication
	Gateway->>Users: POST /Authentication
	Users->>Users: Check PBKDF2 hash, start session,<br/>sign access token with the private key (RS256)
	Users-->>Client: accessToken (15 min) + refreshToken

	Client->>Gateway: Request + access token
	opt First token seen, or unknown key id (at most every 30 s)
		Gateway->>Users: GET /.well-known/openid-configuration, /.well-known/jwks.json
		Users-->>Gateway: Issuer and public key
	end
	Gateway->>Gateway: Verify signature, issuer and expiry
	Gateway->>Service: Request + same token
	Service->>Service: Verify again with its own copy of the public key
	Service-->>Client: Response

	Client->>Gateway: POST /Authentication/Refresh (refreshToken)
	Gateway->>Users: POST /Authentication/Refresh
	Users-->>Client: New pair, the presented token is spent
```

Details in [`ms.users/README.md`](ms.users/README.md).

### Gateway routes

`ms.gateway.api` exposes the same routes as the services, so a client only changes the host and port. The services keep validating the token themselves, and remain reachable directly on their own ports.

| Method | Route | Service | Auth at gateway |
|--------|-------|---------|-----------------|
| POST | `/Authentication` | ms.users | none |
| POST | `/Authentication/Refresh` | ms.users | none |
| POST | `/Authentication/Logout` | ms.users | none |
| GET | `/User` | ms.users | `admin` |
| POST | `/User` | ms.users | `admin` |
| GET | `/Employees` | ms.employees | `admin` |
| POST | `/Employees` | ms.employees | `admin` |
| PUT | `/Employees/AttendanceState/{attendance}` | ms.employees | any authenticated user |
| GET | `/Attendances?userName={userName}` | ms.attendances | any authenticated user |
| GET | `/Attendances/ByDateRange?userName=&from=&to=` | ms.attendances | `admin` |
| POST | `/Attendances` | ms.attendances | any authenticated user |
| POST | `/Assistant/Chat` | ms.assistant | any authenticated user |

Gateway responses: no/invalid/expired token → `401`; valid token without the `admin` role on an admin route → `403`; any other route or method → `404`; service unreachable → `502` (timeout → `503`). Every other response comes from the service unchanged.

Routes are defined in `ms.gateway/ms.gateway.api/ocelot.json` (containers) and `ocelot.Local.json` (gateway running on the host). Both files must keep the same routes; a unit test checks it.

Each service has its own README with its API, configuration, dependencies and tests: `ms.users/README.md`, `ms.employees/README.md`, `ms.attendances/README.md`, `ms.assistant/README.md`, `ms.communications/README.md`.

### ms.attendances endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/Attendances?userName={userName}` | any authenticated user | Full attendance history of an employee (used by `ms.employees` via Refit). |
| GET | `/Attendances/ByDateRange?userName={userName}&from={yyyy-MM-dd}&to={yyyy-MM-dd}` | `admin` | Attendances of an employee between two dates, both days inclusive, at most 31 days, most recent first. Returns `[]` when there are none. Invalid input → `400`. The 31-day limit is duplicated in `ms.assistant` (`GetAttendancesByDateRangeTool.MaxRangeDays`); change both together. |
| POST | `/Attendances` | any authenticated user | Creates an attendance record for the current user. |

### Central package management

The solution uses NuGet Central Package Management (`Directory.Packages.props` at the repository root) to pin a single version per package across all projects. Each `.csproj` references packages without a version, and the actual version is resolved from this central file.

### Shared build configuration

`Directory.Build.props` at the repository root holds the build settings shared by every project, so each `.csproj` only declares what is specific to it ([MSBuild docs](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)):

| Applies to | Settings |
|------------|----------|
| Every `.csproj` | `TargetFramework` `net10.0`, `ImplicitUsings`, `Nullable` enabled (the application/domain/infrastructure layers turn it off in their own `.csproj`) |
| Test projects (name ends in `.test`) | `IsPackable=false`, xUnit, xUnit runner, test SDK, Moq, coverlet, global `using Xunit`. `dotnet test` runs them unless the project sets `IsTestProject=false`, as the end-to-end project does |
| Web APIs (`Microsoft.NET.Sdk.Web`) | Docker tooling: Linux target, repository root as build context, path to `docker-compose.dcproj` |

A value set in a `.csproj` overrides the shared one. The Dockerfiles copy `Directory.Build.props` and `Directory.Packages.props` before `dotnet restore`. After pulling these files, reload the solution in Visual Studio.

### Component diagram

```mermaid
flowchart TB
	Client[HTTP Client / Swagger]
	Gateway[ms.gateway.api\nAPI gateway - Ocelot\nvalidates tokens]

	subgraph Services[Microservices]
		Users[ms.users.api\nAuthentication, sessions and users\nonly issuer of tokens]
		Employees[ms.employees.api\nEmployees and current state]
		Attendances[ms.attendances.api\nAttendance history]
		Assistant[ms.assistant.api\nAI assistant]
		Communications[ms.communications\nRabbitMQ library]
	end

	subgraph Messaging[Messaging]
		Rabbit[RabbitMQ\nms.rabbitmq.bus]
	end

	subgraph Data[Persistence]
		Cassandra[(Cassandra kusers\nuser, user_session, refresh_token)]
		Keys[(users_keys volume\nRSA private key)]
		Sql[(SQL Server\nEmployeesAttendance.Employee)]
		Mongo[(MongoDB\nDbHistoricalAttendance.Attendances)]
	end

	Client -->|All requests :9000| Gateway
	Client -.->|Direct access :9009 / :9010 / :9020 / :9030| Services
	Gateway -->|/Authentication, /User| Users
	Gateway -->|/Employees| Employees
	Gateway -->|/Attendances| Attendances
	Gateway -->|/Assistant/Chat| Assistant

	Gateway -.->|Public key: /.well-known| Users
	Employees -.->|Public key| Users
	Attendances -.->|Public key| Users
	Assistant -.->|Public key| Users

	Employees -->|Refit POST /User with caller token| Users
	Employees -->|Refit GET /Attendances| Attendances
	Assistant -->|Refit GET with caller token| Attendances
	Assistant -->|Refit GET with caller token| Employees
	Assistant -->|Messages API| Claude[(Anthropic API\nClaude)]

	Users -->|Users, sessions, refresh token hashes| Cassandra
	Users -->|Signs tokens with| Keys
	Employees -->|Reads and updates employees| Sql
	Attendances -->|Reads and saves attendances| Mongo

	Employees -.-> Communications
	Attendances -.-> Communications
	Communications -.-> Rabbit
	Employees -->|AttendanceStateChangedEvent| Rabbit
	Rabbit -->|AttendanceStateChangedEvent| Attendances
```

Solid arrows are requests and messages; dotted arrows are the public key download (cached, on the first token) and the use of the shared library.

### Communication between services

Synchronous communication is done over HTTP. `ms.employees` creates the employee's account in `ms.users` (`POST /User`) before saving the employee, and queries `ms.attendances` with Refit before updating an employee's state. Every service downloads `ms.users`' public key to validate tokens.

Asynchronous communication uses RabbitMQ and durable queues:

| Event                          | Publishes       | Consumes          | Purpose                                            |
| ------------------------------- | -------------- | ---------------- | --------------------------------------------------- |
| `AttendanceStateChangedEvent`  | `ms.employees` | `ms.attendances`  | Save the attendance change in the history.           |

Services connect to RabbitMQ through the DNS name `ms.rabbitmq.bus` inside Docker. The `docker-compose.override.yml` file overrides `appsettings.json` configuration via environment variables.

### Sequence diagram

The following flow summarizes authentication, employee creation, and attendance update:

```mermaid
sequenceDiagram
	autonumber
	actor Client as Client
	participant Users as ms.users.api
	participant Cassandra as Cassandra
	participant Employees as ms.employees.api
	participant SQL as SQL Server
	participant Rabbit as RabbitMQ
	participant Attendances as ms.attendances.api
	participant Mongo as MongoDB

	Client->>Users: POST /Authentication\nusername and password
	Users->>Cassandra: Look up user in kusers.user
	Cassandra-->>Users: User and role
	Users->>Cassandra: Start session, store refresh token hash
	Users-->>Client: Access token (15 min) + refresh token

	Client->>Users: POST /Authentication/Refresh\nrefresh token
	Users->>Cassandra: Spend it, store the new one
	Users-->>Client: New access token + refresh token

	Client->>Employees: POST /Employees\naccess token + employee data
	Employees->>Users: POST /User\nAuthorization + account
	Users->>Cassandra: Create user, password hashed
	Cassandra-->>Users: Account created
	Users-->>Employees: Account created
	Employees->>SQL: Insert employee
	SQL-->>Employees: Employee created
	Employees-->>Client: Creation result

	Client->>Employees: PUT /Employees/AttendanceState/{attendance}\nJWT + notes
	Employees->>Attendances: GET /Attendances?userName=\nAuthorization
	Attendances->>Mongo: Query the user's history
	Mongo-->>Attendances: List of attendances
	Attendances-->>Employees: List of attendances
	Employees->>SQL: Update state and notes
	SQL-->>Employees: Update confirmed
	Employees->>Rabbit: Publish AttendanceStateChangedEvent
	Rabbit-->>Attendances: Deliver event
	Attendances->>Mongo: Insert new attendance
	Mongo-->>Attendances: Record saved
	Employees-->>Client: Update result
```

### Startup flow

1. Docker Compose creates the internal network and infrastructure services.
2. RabbitMQ and Cassandra run their healthchecks.
3. The APIs wait until their required dependencies are available.
4. SQL Server runs `initsqldatabase.sql` idempotently.
5. Cassandra runs `cassandra/init.cql` idempotently.
6. `ms.users` generates its signing key if it has none, protects any readable stored password, and only then starts serving requests.
7. The other APIs register their RabbitMQ consumers and start serving requests. They download `ms.users`' public key when they receive their first token.

### ms.assistant (AI assistant)

`POST /Assistant/Chat` answers attendance questions in natural language. It is an **agent**: Claude reads the question, decides which tools it needs, and `ms.assistant` runs those tools by calling the other services with the caller's own JWT. Then Claude writes the answer from the data it received.

| Tool | Offered to | Calls |
|------|-----------|-------|
| `get_my_attendances` | everyone | `GET /Attendances?userName={caller}` |
| `get_attendances_by_date_range` | admin | `GET /Attendances/ByDateRange` |
| `list_employees` | admin | `GET /Employees` |

Request and response:

```json
{ "question": "¿Y la semana pasada?",
  "history": [ { "role": "user", "content": "¿Cuántas veces falté este mes?" },
               { "role": "assistant", "content": "Dos veces: el 3 y el 17 de octubre." } ] }
```
```json
{ "answer": "La semana pasada no tienes ausencias registradas." }
```

Errors: `400` question empty or longer than 2,000 characters, or more than 20 previous messages · `401` no token · `503` AI service unavailable.

**Running it** requires an Anthropic API key, which is never committed:

```powershell
Copy-Item .env.example .env      # then set ANTHROPIC_API_KEY=... in .env
docker compose up -d --build
```

Settings (`ms.assistant/ms.assistant.api/appsettings.json`):
- `Anthropic:Model` (default `claude-opus-5-5`);
- `Anthropic:Effort` (default `low`);
- `Anthropic:MaxTokens`;
- `Anthropic:TimeoutSeconds`;
- `Assistant:MaxToolRounds` (default 5).

Every question costs tokens. Each answer logs its token usage (without the question or answer text). The design is in `ms.assistant/features/001-ai-attendance-assistant/`.

## End-to-end regression tests

Unit tests fake every collaborator, so nothing in them checks that the services still work together. The regression suite does: it starts a throwaway copy of the whole system, calls every operation through the gateway like a client does, follows the complete attendance flow, and destroys the copy and its data.

```powershell
powershell -File e2e/run-e2e.ps1     # or: pwsh e2e/run-e2e.ps1
```

- It runs alongside the development stack without touching it: compose project `eac-e2e`, with ports `19000` (gateway) and `19009`/`19010`/`19020`/`19030` (services, to check that each one validates tokens on its own).
- `dotnet test EmployeeAttendanceControl.sln` doesn't run it and doesn't need Docker.
- GitHub Actions runs it on every pull request to `master` (`.github/workflows/e2e.yml`), without the paid assistant check.
- A new or changed gateway route also updates `e2e/EmployeeAttendanceControl.e2e.test/Catalog/GatewayRoutes.cs`, or the suite fails.

Parameters, coverage and how to add a check: [`e2e/README.md`](e2e/README.md).

## Docker Compose

The project separates common configuration from local configuration:

- `docker-compose.yml`: base configuration. Defines the images and `Dockerfile`s for the services.
- `docker-compose.override.yml`: development configuration. Defines local ports, environment variables, data volumes, and dependencies. The broker counts as healthy only once port `5672` accepts connections, and consumers also keep trying until they can subscribe (see [`ms.communications`](ms.communications/README.md)).

When `docker compose` is run without specifying files, Compose automatically loads `docker-compose.yml` and `docker-compose.override.yml` and merges their configurations.

### Development

Use both files via Compose's automatic behavior:

```powershell
docker compose up --build
```

To run it in the background:

```powershell
docker compose up -d --build
```

To stop and remove the containers and the network:

```powershell
docker compose down
```

SQL Server uses the Docker volume `sqlserver_data` to store the `.mdf` and `.ldf` files. The entrypoint automatically fixes the volume's permissions for the `mssql` user, so there is no need to run `chown` manually.

```powershell
docker compose up -d --build
```

The volume keeps the data even if the containers are removed. The previous `data/sqlServer` directory is not deleted; it is kept as a backup and is no longer mounted into SQL Server.

MongoDB also uses the Docker volume `mongodb_data`. The previous `data/mongo` directory is kept as a backup and is no longer mounted, because the WiredTiger storage of the bind mount caused MongoDB restarts and HTTP 500 responses in `attendances.api`.

### Running the API gateway

The gateway can run in two ways. The routes file is chosen by `ASPNETCORE_ENVIRONMENT`, with no code change:

| Mode | How to start | Gateway URL | Routes file | Downstream services |
|------|--------------|-------------|-------------|---------------------|
| Containers | `docker compose up -d --build` (service `ms.gateway.api`, `ASPNETCORE_ENVIRONMENT=Development`) | `http://localhost:9000` | `ocelot.json` | `ms.users.api:80`, `ms.employees.api:80`, `ms.attendances.api:80`, `ms.assistant.api:80` |
| Local (host) | `dotnet run --project ms.gateway/ms.gateway.api --environment Local`, or the `ms.gateway.api` launch profile | `http://localhost:9001` | `ocelot.Local.json` | `localhost:9009`, `localhost:9010`, `localhost:9020`, `localhost:9030` |

Local mode needs the services running in Compose with their published ports. Example:

```powershell
$token = (Invoke-RestMethod -Method Post -Uri http://localhost:9000/Authentication -ContentType 'application/json' -Body '{"userName":"admin","password":"1234"}').accessToken
Invoke-RestMethod -Uri http://localhost:9000/Employees -Headers @{ Authorization = "Bearer $token" }
```

### Automatic SQL Server initialization

The `ms.sql.employees.db` service uses a custom image based on SQL Server. On startup, it waits until SQL Server accepts connections and runs `initsqldatabase.sql` automatically.

The script idempotently creates:

- the `EmployeesAttendance` database;
- the `dbo.Employee` table;
- the initial `hacero` user record.

Therefore, the environment can be started normally:

```powershell
docker compose up -d --build
```

On subsequent restarts, the table and the initial record are not duplicated. Changes made after startup are preserved in the `sqlserver_data` volume.

Do not run `docker-compose.override.yml` in isolation. It is a partial file and does not contain `image` or `build` for every service.

The files can also be specified explicitly:

```powershell
docker compose -f docker-compose.yml -f docker-compose.override.yml up --build
```

### Production

Do not use `docker-compose.override.yml` in production, because it contains local-environment-specific configuration, such as development ports, local volumes, and sample credentials.

The base run command is:

```powershell
docker compose -f docker-compose.yml up -d
```

However, before deploying to production it is recommended to create an additional file, for example `docker-compose.prod.yml`, to define:

- production environment variables and secrets;
- ports or a reverse proxy;
- restart policies;
- appropriate persistent volumes;
- labels, resource limits, and network configuration;
- versioned images, instead of ambiguous tags like `latest`.

With that file, the run command would be:

```powershell
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

To stop the production configuration:

```powershell
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
```

Real secrets should not be stored in the Compose files or in the repository. They should be provided via environment variables, a file managed outside version control, or a secrets manager.
