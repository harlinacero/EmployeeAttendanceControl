# EmployeeAttendanceControl

Employee attendance control system built as .NET 10 microservices (ms.users, ms.employees, ms.attendances) orchestrated with docker-compose. See README.md for running the stack.

## SDD Conventions

### Structure
- Service folders: `ms.*` (`ms.users`, `ms.employees`, `ms.attendances`, `ms.gateway`); `ms.communications/ms.rabbitmq` is a shared RabbitMQ library, not a service
- `ms.gateway` is the API gateway (Ocelot): api layer only (`ms.gateway.api`: `Program.cs`, `Extensions/`, `ocelot.json` + `ocelot.Local.json`), no application/domain/infrastructure and no persistence; tests in `ms.gateway/ms.gateway.test/ms.gateway.api.test`
- Features folder: `<service>/features/NNN-feature-name/`
- Layers per service: `<svc>.api` (controllers, consumers, events, DI extensions, `SettingsOptions`), `<svc>.application` (commands/queries + `Handlers/`, `Mappers/`, `Request/`/`Requests/`, `Responses/`), `<svc>.domain` (`Entities/`, `Repositories/` interfaces), `<svc>.infraestucture` (data context, persistence entities, repositories)
- Keep the existing spellings: `infraestucture` (attendances, employees), `infraestructure` (users), test folder `ms-attendances.test`, test project `ms-attendances.api.test` (namespace `ms_attendances.api.test`)
- Persistence: ms.users → Cassandra, ms.employees → SQL Server via Dapper, ms.attendances → MongoDB (`MongoDB.Driver`)

### Code style
- .NET 10, file-scoped classes in block namespaces (`namespace x { ... }`), one class per file, namespace = folder path
- CQRS with MediatR: queries/commands are `record`s implementing `IRequest<T>` (e.g. `GetAllAttendancesQuery(string UserName)`); handlers in `Queries/Handlers` / `Commands/Handlers` named `<Name>Handler`
- Controllers are thin: `[ApiController]`, `[Route("[controller]")]`, inject only `IMediator`, return `Ok(await _mediator.Send(...))`; action methods named as nouns (`Attendances`, `Attendance`, `Employees`)
- Authorization: JWT bearer; `[Authorize]` for any authenticated user, `[Authorize(Roles = "admin")]` for admin-only endpoints; current user from the `ClaimTypes.NameIdentifier` claim
- Constructor injection into `private readonly _camelCase` fields; expression-bodied ctor allowed for a single dependency
- Mapping with AutoMapper `Profile`s per layer (`AttendanceProfile` in application, `AttendanceMongoProfile` in infrastructure, `EventMapperProfile` in api)
- Domain entities are plain POCOs; persistence-specific types live in infrastructure (`MongoEntities/AttendanceMongo`)
- Configuration via the `IOptions<T>` pattern (`ConnectionStrings`, `SettingsOptions` bound in `Program.cs`); no raw `IConfiguration` reads in classes
- DI registration in `Extensions` static classes (`AddAttendanceDbContext`, `AddConsummer`, `AddAuthenticationSettings`)
- Structured logging with `ILogger<T>` and message templates
- All I/O is async (`Task<T>`, `...Async` driver calls)

### Testing
- Framework / mocks: xUnit + Moq (+ coverlet.collector); no integration tests
- Layout: one test project per layer under `<svc>/<svc>.test` (or `ms-attendances.test`), folders mirror source (`Controllers/`, `Queries/`, `Commands/`, `Repositories/`, `Mappers/`, `Consumers/`)
- Test class: `<ClassUnderTest>Tests`; mocks created in the constructor as `Mock<T>` fields
- Naming: `MethodShouldExpectedBehavior` in PascalCase without underscores (e.g. `HandleShouldReturnMappedAttendances`). A few older tests use `Method_Should_When`; don't copy that style
- Structure: `// Arrange // Act // Assert` comments
- Per layer: controller → asserts `OkObjectResult` and value type with mocked `IMediator`; handler → mocked repository + `IMapper`; repository → mocked `IAttendanceContext` / `IMongoCollection<T>`

### Dependencies & config
- NuGet Central Package Management: versions only in root `Directory.Packages.props`; `.csproj` uses `<PackageReference Include="X" />` without version
- Any new package is a team decision — flag it
- Settings in `appsettings.json`, overridden by env vars in `docker-compose.override.yml`

### Inter-service communication
- Async: RabbitMQ via `ms.communications/ms.rabbitmq`; queue name = event class name (`nameof(AttendanceStateChangedEvent)`), durable queues; consumers in `<svc>.api/Consumers` implementing `IConsumer`
- Events: `EmployeeCreateEvent` (employees → users), `AttendanceStateChangedEvent` (employees → attendances)
- Sync: Refit clients in `<svc>.application/HttpComunications` (e.g. `ms.employees` → `GET /Attendances?userName=` forwarding the caller's `Authorization` header). Changing an existing route/contract breaks these clients.
- Clients go through `ms.gateway.api` (container `:9000`, local `:9001`), which exposes the same routes as the services. A new or changed endpoint must be added to **both** `ocelot.json` and `ocelot.Local.json` (same order), with `AuthenticationOptions` `Bearer` unless anonymous and `RouteClaimsRequirement { "role": "admin" }` for admin-only; `OcelotRoutesTests` keeps the catalog in sync. Service-to-service calls stay direct (not through the gateway). Services remain reachable directly on `:9009`/`:9010`/`:9020`

### Commands
- Build: `dotnet build EmployeeAttendanceControl.sln`
- Test (all): `dotnet test EmployeeAttendanceControl.sln`
- Test (one service): `dotnet test <path-to-test-csproj>` for each project in `<svc>/<svc>.test` (note: `dotnet test ms.attendances` from the README fails — there is no project/solution in that folder)
- Run: `docker compose up -d --build`

### Git
- Main branch: `master` (the branch features are cut from and PRs target)
- Feature branches: `feature/NNN-kebab-case-name` (same as the feature folder name)

### Business glossary & rules
- **Employee**: a person managed by ms.employees (SQL `dbo.Employee`), identified by `UserName`; holds the current attendance state (`LastAttendanceState/Date/Notes`)
- **User / account**: login credentials + `Role` in ms.users (Cassandra), created automatically from `EmployeeCreateEvent`; issues the JWT
- **Role**: `admin` or regular user (claim `ClaimTypes.Role`); admin manages employees and users
- **Attendance (record)**: one historical entry in ms.attendances (`AttendanceRecord`: `UserName`, `FullName`, `Date`, `Attendance` true=present/false=absent, `Notes`), stored in Mongo `DbHistoricalAttendance.Attendances`
- **Attendance state change**: employee marks attendance in ms.employees → `AttendanceStateChangedEvent` → a new record in the history
- Rule: `UserName` is the identifier shared across all services
- Rule: attendance history is append-only (create and read only; no update/delete endpoints exist)
