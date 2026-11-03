# End-to-end regression tests

Checks that the services still work **together**. A run starts a throwaway copy of the whole system, calls every operation through the gateway like a client does, and destroys the copy and its data. The feature documents are in [`features/002-e2e-regression-tests`](../features/002-e2e-regression-tests).

## Running

From the repository root, with Docker running:

```powershell
powershell -File e2e/run-e2e.ps1     # Windows PowerShell 5.1
pwsh e2e/run-e2e.ps1                 # PowerShell 7, as in CI
```

| Parameter | Default | Use |
|-----------|---------|-----|
| `-Assistant` | off | Also runs the real assistant check, which **calls a paid service**. The key comes from `ANTHROPIC_API_KEY` or the root `.env` file. Without a key the check is reported as skipped. |
| `-KeepEnvironment` | off | Keeps the environment after the run to investigate a failure, and prints the command that destroys it. |
| `-ReadyTimeoutSeconds` | `300` | Maximum wait for the environment to become ready. |
| `-GatewayPort` | `19000` | Host port published for the gateway. |

The exit code is `0` only if the environment became ready and no check failed. A full run takes several minutes, mostly building and starting containers.

To iterate quickly, keep the environment once and run single checks against it:

```powershell
powershell -File e2e/run-e2e.ps1 -KeepEnvironment
dotnet test e2e/EmployeeAttendanceControl.e2e.test -p:RunE2E=true --filter FullyQualifiedName~AttendanceFlowTests
```

The checks that don't need an environment run on their own:

```powershell
dotnet test e2e/EmployeeAttendanceControl.e2e.test -p:RunE2E=true --filter "FullyQualifiedName~Support|FullyQualifiedName~Catalog"
```

## Isolation

- The run uses the compose project `eac-e2e` and the extra file `docker-compose.e2e.yml`. It publishes only the gateway port, so it can run while the development stack is up, and never touches it.
- `down -v` removes the containers, the volumes and the network, even when checks fail. Leftovers of a killed run are removed when the next run starts.
- The `eac-e2e/*` images stay on purpose, to speed up the next run. They hold no run data.
- `docker-compose.e2e.yml` limits the memory of Cassandra and SQL Server. Without it, Cassandra alone reserves a quarter of the host's memory, and two stacks together exhaust it.
- `dotnet test EmployeeAttendanceControl.sln` doesn't run these checks. The project compiles with the solution but has `IsTestProject=false` unless `-p:RunE2E=true` is passed.

## What it checks

| Area | Tests |
|------|-------|
| Sign-in | `Security/AuthenticationTests` |
| Who can call each operation | `Security/AccessTests`, driven by `Catalog/GatewayRoutes` |
| Employee → account | `Flows/EmployeeOnboardingTests` |
| Attendance → history, date range | `Flows/AttendanceFlowTests` |
| Real assistant (opt-in) | `Flows/AssistantTests` |
| Catalog matches `ocelot.json` | `Catalog/GatewayRoutesTests` |

The suite protects **current** behavior, including two behaviors recorded for review in a separate feature: any signed-in user can read any history (OBS-01), and marking attendance appends the previous count to the notes (OBS-02).

## Adding a check for a new operation

1. Add the route to `Catalog/GatewayRoutes.cs`, with its access group and a minimal valid request. `CatalogShouldMatchGatewayRoutes` fails until you do, and the access checks then cover it automatically.
2. Add its behavior to a class in `Flows/`. Each test creates its own employee with `CreateUsableEmployeeAsync`, so tests don't depend on each other.
3. For effects that arrive through messages, use `WaitUntilAsync` and name the effect. Don't use fixed pauses.
4. Copy only the fields you need into `Support/Dtos/`. Don't reference service projects: a contract change must fail a check, not the build.
5. A test class that calls the stack needs `[Collection(E2ECollection.Name)]`. One that doesn't must stay out of the collection, so it runs without an environment.
