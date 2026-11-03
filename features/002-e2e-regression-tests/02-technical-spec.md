# [002] End-to-end regression tests — Technical Specification

> Status: Approved (2026-11-03), amended with §11 the same day
> Based on: `01-business-spec.md` (Approved 2026-10-30)
> Reference implementation followed: `ms.gateway/ms.gateway.test/ms.gateway.api.test/Configuration/OcelotRoutesTests.cs` (route catalog over `ocelot.json`); `features/001-directory-build-props` (shared build settings)

## 1. Overview
A black-box test project, `e2e/EmployeeAttendanceControl.e2e.test`, calls the gateway over HTTP with `HttpClient`. It has no project references to any service, and the DTOs it needs are its own small copies, so a contract change shows up as a failing check and not as a compile error. A PowerShell script, `e2e/run-e2e.ps1`, is the single entry point for running it. The same script runs locally, in GitHub Actions and in the ship-feature skill. It:

1. destroys leftovers of the `eac-e2e` compose project;
2. starts an isolated stack: `docker compose -p eac-e2e` with an extra override file, `docker-compose.e2e.yml`;
3. waits until the gateway, and the services behind it, answer;
4. runs `dotnet test` on the e2e project;
5. always destroys the stack with `down -v`.

The e2e project is part of the solution, so `dotnet build` compiles it. An MSBuild property keeps it out of the plain `dotnet test EmployeeAttendanceControl.sln` run. The isolation and solution-exclusion mechanisms were verified with a spike before writing this spec (D-02, D-03).

## 2. Components by layer
No service code changes, except the startup fixes in §11. All the other changes are in tests, build configuration, compose, CI and docs.

### Build configuration
- (~) `Directory.Build.props` — split "gets the shared test defaults" from "is run by `dotnet test`":
  - new `UsesTestDefaults` = the name ends in `.test`. It conditions `IsPackable` and the shared test packages, plus `using Xunit`;
  - `IsTestProject` defaults to `UsesTestDefaults`, and a project can still override it.
  
  Existing projects behave exactly as before (same evaluated properties and items).

### E2E test project — `e2e/EmployeeAttendanceControl.e2e.test/`
- (+) `EmployeeAttendanceControl.e2e.test.csproj`:
  - `<IsTestProject Condition="'$(RunE2E)' != 'true'">false</IsTestProject>`;
  - links `ms.gateway/ms.gateway.api/ocelot.json` as content (copied to output), for the catalog check;
  - no package references beyond the shared ones;
  - no project references.
- (+) `Support/E2ESettings.cs` — reads the run settings from environment variables, once (§6).
- (+) `Support/Eventually.cs` — `static Task<T> UntilAsync<T>(Func<Task<T>> probe, Func<T,bool> done, string what, TimeSpan timeout, TimeSpan interval)`. It polls until `done` or the timeout, then throws `TimeoutException($"{what} did not happen within {timeout}")` (BR-07, AC-16).
- (+) `Support/GatewayClient.cs` — a thin wrapper over one `HttpClient` whose base address is the gateway. It has:
  - `LoginAsync(user, password)`: returns the token, or the `HttpResponseMessage` for negative checks;
  - `SendAsync(method, path, token?, body?)`;
  - typed helpers: `CreateEmployeeAsync`, `GetEmployeesAsync`, `GetUsersAsync`, `CreateAccountAsync`, `MarkAttendanceAsync`, `GetAttendancesAsync`, `GetAttendancesByDateRangeAsync`, `AddAttendanceAsync`, `ChatAsync`.
- (+) `Support/TestData.cs` — run id `yyyyMMddHHmmss-<6 hex>` and `NewUserName(string tag)` → `e2e-<runid>-<tag>` (BR-03).
- (+) `Support/E2EEnvironmentFixture.cs` + `Support/E2ECollection.cs` — `[CollectionDefinition("E2E")]` with an `ICollectionFixture`. Once per run it:
  - builds the client;
  - signs in the seeded admin, failing every test with `"Seeded admin could not sign in: <status>"` if that fails (AC-17);
  - creates one shared **regular test employee** and waits until it can sign in.
  
  All test classes join the `E2E` collection, so they run sequentially against the one stack.
- (+) `Support/AssistantFactAttribute.cs` — `FactAttribute` subclass. It sets `Skip` with a reason when `E2E_ASSISTANT` isn't `true` ("assistant check not requested"), or when it's `true` but `E2E_ASSISTANT_KEY_PROVIDED` isn't ("ANTHROPIC_API_KEY not provided") (AC-09, BR-08). It uses no new packages.
- (+) `Support/Dtos/` — `EmployeeDto`, `UserDto`, `AttendanceDto`, `CreateEmployeeDto`, `AccountDto`, `CreateAttendanceDto`, `ChatDto`, `ChatAnswerDto`: only the fields the checks read or send (§3).
- (+) `Catalog/GatewayRoutes.cs` — the operation catalog, a single source for the access checks (AC-24):

  | Route | Access group |
  |-------|--------------|
  | `POST /Authentication` | `Anonymous` |
  | `GET /User`, `POST /User`, `GET /Employees`, `POST /Employees`, `GET /Attendances/ByDateRange` | `Admin` |
  | `PUT /Employees/AttendanceState/{attendance}`, `GET /Attendances`, `POST /Attendances`, `POST /Assistant/Chat` | `Identified` |

  Each entry carries a minimal valid request (path with sample values, body) for that route.
- (+) `Catalog/GatewayRoutesTests.cs` — `CatalogShouldMatchGatewayRoutes`: the method + path set in `ocelot.json` equals `GatewayRoutes`. An operation without a check fails the run (AC-24). It needs no environment.
- (+) `Security/AuthenticationTests.cs` — AC-21.
- (+) `Security/AccessTests.cs` — `[Theory]` over `GatewayRoutes`:
  - every non-anonymous route without a token returns 401 (AC-18);
  - every `Admin` route with the regular token returns 403 (AC-19);
  - every `Identified` route except the assistant, with the regular token, returns 2xx (AC-20);
  - another user's history is readable (AC-23).
- (+) `Flows/EmployeeOnboardingTests.cs` — AC-10, AC-15.
- (+) `Flows/AttendanceFlowTests.cs` — AC-11, AC-12, AC-13, AC-14, AC-22. Each test creates its **own** employee, so tests don't depend on each other's order or records.
- (+) `Flows/AssistantTests.cs` — `[AssistantFact]` AC-09.
- (+) `Support/EventuallyTests.cs` — unit tests of the waiting helper (AC-16). Run without the stack: `--filter FullyQualifiedName~Support`.

### Runner and environment
- (+) `docker-compose.e2e.yml` (repo root) — the override applied after `docker-compose.override.yml` (D-02):
  - every service: `container_name: !reset null` and `ports: !reset []`;
  - the gateway: `ports: !override ["${E2E_GATEWAY_PORT:-19000}:80"]`;
  - every built image is renamed `eac-e2e/<name>` (including `cassandra` and `mssql/server`, which the main compose also builds), so the dev image tags are never overwritten;
  - the assistant: `Anthropic:ApiKey=${ANTHROPIC_API_KEY:-e2e-assistant-disabled}`, so it passes `ValidateOnStart` without a real key;
  - Cassandra: `MAX_HEAP_SIZE=1G`, `HEAP_NEWSIZE=256M` and 30 healthcheck retries; SQL Server: `MSSQL_MEMORY_LIMIT_MB=2048`. By default Cassandra reserves a quarter of the host's memory, and two stacks exhaust it.
  
  Volumes keep their names; the `eac-e2e` project prefixes them (`eac-e2e_mongodb_data`…).
- (+) `e2e/run-e2e.ps1`:
  - compatible with Windows PowerShell 5.1 and `pwsh` 7, which GitHub's Ubuntu runners have;
  - parameters: `-Assistant` (switch; the key comes from `ANTHROPIC_API_KEY` or the root `.env`), `-KeepEnvironment` (switch), `-ReadyTimeoutSeconds` (default 300), `-GatewayPort` (default 19000);
  - steps: §8;
  - exit code 0 only if the stack was ready and every check passed; otherwise 1.
- (+) `e2e/README.md` — what the suite covers, how to run it, its parameters, and how to add a check for a new operation.

### CI
- (+) `.github/workflows/e2e.yml` — `on: pull_request` to `master`:
  - one `ubuntu-latest` job: `actions/checkout`, `actions/setup-dotnet` (10.0.x), `pwsh e2e/run-e2e.ps1`;
  - `actions/upload-artifact` uploads `e2e/TestResults/*.trx` with `if: always()`;
  - no secrets, and no `-Assistant` (BR-08);
  - timeout of 30 min.

### Outside the repository
- (~) `~/.claude/skills/ship-feature/SKILL.md` — new step 1b, right after the full suite: if `CLAUDE.md` lists an "E2E" command, run it.
  - On failure, stop and show the failed checks (AC-28).
  - If Docker isn't available, ask whether to continue without it, and never report it as passed.
  - Put the regression result in the PR's "Testing" section (AC-27).
  
  Kept generic: it reads the command from `CLAUDE.md`, so it doesn't hard-code this repo (Q-04).

### Docs
- (~) `CLAUDE.md`:
  - Structure: `e2e/` and its project;
  - Testing: replace "no integration tests" with the unit/E2E split and the E2E naming and placement;
  - Commands: `E2E: powershell -File e2e/run-e2e.ps1 [-Assistant] [-KeepEnvironment]` (CI uses `pwsh`; decision 2026-11-03);
  - Dependencies & config: `UsesTestDefaults` vs `IsTestProject`;
  - Inter-service communication: a new or changed gateway route also updates `e2e/.../Catalog/GatewayRoutes.cs`.
- (~) `README.md` (root) — "End-to-end regression tests" section: what, how to run, and the CI check.
- (~) `EmployeeAttendanceControl.sln` — add the project under a new root solution folder `e2e`.

## 3. API contract
No contract changes. The suite checks the current gateway contract:

| Method | Route | Auth | Request used by the suite | Expected |
|--------|-------|------|---------------------------|----------|
| POST | `/Authentication` | anonymous | `{ "userName", "password" }` | 200 + JWT as the body (text), or 401 |
| GET | `/User` | admin | — | 200 `[{ userName, password, role }]` |
| POST | `/User` | admin | `{ userName, password, role: "user" }` | 200 |
| GET | `/Employees` | admin | — | 200 `[{ userName, firstName, lastName, lastAttendance, lastAttendanceState, lastAttendanceNotes }]` |
| POST | `/Employees` | admin | `{ userName, firstName, lastName, password, role: "user" }` | 200 |
| PUT | `/Employees/AttendanceState/{true\|false}` | identified | JSON string body: `"notes"` | 200 |
| GET | `/Attendances?userName=` | identified | — | 200 `[{ id, userName, fullName, date, attendance, notes }]` |
| GET | `/Attendances/ByDateRange?userName=&from=&to=` | admin | dates `yyyy-MM-dd` | 200 list; 400 if from > to or range > 31 days |
| POST | `/Attendances` | identified | `{ fullName, date, attendance, notes }` | 200 |
| POST | `/Assistant/Chat` | identified | `{ question }` | 200 `{ answer }` (non-empty) |

Missing token → 401 at the gateway. Valid token with the wrong role → 403 at the gateway (`RouteClaimsRequirement`).

Expected history record after marking attendance (AC-11): `userName` = the employee, `fullName` = `"<FirstName> <LastName>"` (employees' `AttendanceStateChangedEvent` mapping), `attendance` = the value sent, `date` = today (UTC date), `notes` = `"<notes> [<n>] Asistencias"`, where `n` is the number of records before the change (OBS-02).

## 4. Data model & persistence
No changes. Test data lives only in the `eac-e2e_*` volumes and is removed by `down -v` (BR-02). User names have the form `e2e-<runid>-<tag>` (BR-03).

## 5. Inter-service impact
| Service | Change | Mechanism | Contract |
|---------|--------|-----------|----------|
| all | none; exercised as black box | HTTP through the gateway; `EmployeeCreateEvent` and `AttendanceStateChangedEvent` observed through their effects | unchanged |

## 6. Configuration & dependencies
Environment variables read by `E2ESettings` (set by `run-e2e.ps1`; defaults allow running the tests against an already running e2e stack):

| Variable | Default | Use |
|----------|---------|-----|
| `E2E_BASE_URL` | `http://localhost:19000` | gateway |
| `E2E_ADMIN_USER` / `E2E_ADMIN_PASSWORD` | `admin` / `1234` | seeded admin (OBS-03) |
| `E2E_EFFECT_TIMEOUT_SECONDS` | `60` | delayed effects (Q-02) |
| `E2E_ASSISTANT` | unset | `true` enables the real chat check |
| `E2E_ASSISTANT_KEY_PROVIDED` | unset | set by the script when `ANTHROPIC_API_KEY` is set; the key itself never reaches the tests |

- **New packages: none.** The project uses `HttpClient` and `System.Net.Http.Json` from the framework. The CI uses only official GitHub actions (`actions/checkout`, `actions/setup-dotnet`, `actions/upload-artifact`).
- **`Directory.Build.props` change** (UsesTestDefaults): verified in a spike. A project with `IsTestProject=false` and the split keeps its packages, the solution builds it, the solution `dotnet test` skips it, and `-p:RunE2E=true` runs it. Without the split, the packages disappear and the project doesn't compile.
- `docker-compose.e2e.yml` relies on the `!reset` / `!override` tags (Compose ≥ 2.24; local v5.5.1). `docker compose config` with the three files was verified: no container names, only port 19000 published, `eac-e2e_*` volumes and network, `eac-e2e/*` images.

## 7. Security
- The checks use the seeded admin password, which is fixed in every fresh stack (OBS-03). The stack is throwaway and only the gateway port is published.
- `ANTHROPIC_API_KEY` reaches only the assistant container, through the compose environment. The script passes the tests a "provided" flag, never the key. CI has no secrets.
- The suite checks the access rules of every route (AC-18…AC-20). These are the same rules `OcelotRoutesTests` checks statically; the suite checks them at runtime.

## 8. Error handling & logging
`run-e2e.ps1` flow and failure behavior:
1. `docker compose -p eac-e2e … down -v --remove-orphans` — clean leftovers (AC-07). Only the `eac-e2e` project is ever addressed, so the dev project `employeeattendancecontrol` is never touched (AC-03).
2. `up -d --build`. If the build or start fails, go to step 6 with failure (AC-05).
3. Readiness: poll until all of these answer or `-ReadyTimeoutSeconds` passes:
   - `POST /Authentication` admin → 200 (gateway + users + Cassandra);
   - `GET /Employees` → 200 (employees + SQL Server);
   - `GET /Attendances?userName=admin` → 200 (attendances + Mongo).
   
   On timeout, print which probes never succeeded, plus `docker compose ps` and the last 50 log lines of each failing service. Don't run tests (AC-05).
4. `dotnet test e2e/EmployeeAttendanceControl.e2e.test -p:RunE2E=true --logger "trx;LogFileName=e2e.trx" --logger "console;verbosity=normal"`, with the environment variables of §6. xUnit runs all tests even when some fail (AC-06).
5. Print the summary: passed / failed / skipped, and the failed test names with their messages (from the console logger).
6. `finally`: unless `-KeepEnvironment`, `down -v --remove-orphans`. With `-KeepEnvironment`, print the exact `down` command (AC-04). The `finally` block also runs on Ctrl+C. A killed process leaves leftovers that step 1 of the next run removes (AC-07).

In the tests, every assertion message names the operation, the caller and the actual status or body. For example: `"GET /Employees as regular user: expected 403, got 200"` (UC-03 E1).

## 9. Traceability
| AC | Components | Test level |
|----|------------|------------|
| AC-01 | `run-e2e.ps1`, all e2e tests | E2E run (manual verification of the script) |
| AC-02 | `run-e2e.ps1` step 6 | manual: `docker ps -a`, `docker volume ls`, `docker network ls` filtered by `eac-e2e` after a run. The `eac-e2e/*` images are build output and are kept to speed up the next run (decision 2026-11-03) |
| AC-03 | `docker-compose.e2e.yml`, script `-p eac-e2e` | manual: dev stack up with a marker record → run → dev containers up and the marker still there |
| AC-04 | script `-KeepEnvironment` | manual |
| AC-05 | script step 3 | manual: run with `-ReadyTimeoutSeconds 5` |
| AC-06 | xUnit + script exit code | manual: temporarily break one expectation → others run, exit 1, failure listed |
| AC-07 | script step 1 | manual: start, kill, rerun |
| AC-08 | `Directory.Build.props`, e2e `.csproj` | `dotnet test EmployeeAttendanceControl.sln` → the 13 projects / 175 tests of the baseline, plus only the `ms.rabbitmq.test` project of §11; no e2e test |
| AC-09 | `AssistantFactAttribute`, `AssistantTests`, script `-Assistant` | E2E + manual run without a key (skipped with reason) |
| AC-10 | `EmployeeOnboardingTests.CreateEmployeeShouldMakeAccountUsable` | E2E |
| AC-11 | `AttendanceFlowTests.MarkPresentShouldAddRecordToHistory` | E2E |
| AC-12 | `AttendanceFlowTests.MarkAbsentShouldAppendWithoutChangingPreviousRecord` | E2E |
| AC-13 | `AttendanceFlowTests.ByDateRangeShouldReturnRecordsOnlyWhenRangeIncludesToday` | E2E |
| AC-14 | `AttendanceFlowTests.AddAttendanceShouldAppearInOwnHistory` | E2E |
| AC-15 | `EmployeeOnboardingTests.CreateAccountShouldAllowSignIn` | E2E |
| AC-16 | `Eventually`, `EventuallyTests` | unit (no stack) |
| AC-17 | `E2EEnvironmentFixture` | manual: run with a wrong `E2E_ADMIN_PASSWORD` against a kept stack |
| AC-18 | `AccessTests.ProtectedRouteShouldRejectAnonymousCaller` | E2E |
| AC-19 | `AccessTests.AdminRouteShouldRejectRegularUser` | E2E |
| AC-20 | `AccessTests.IdentifiedRouteShouldAcceptRegularUser` | E2E |
| AC-21 | `AuthenticationTests.LoginShouldRejectWrongPassword`, `LoginShouldRejectUnknownUser`, `LoginShouldReturnTokenForSeededAdmin` | E2E |
| AC-22 | `AttendanceFlowTests.ByDateRangeShouldRejectInvalidRange` (theory: from > to; 32 days) | E2E |
| AC-23 | `AccessTests.HistoryShouldBeReadableByAnotherUser` | E2E |
| AC-24 | `GatewayRoutesTests.CatalogShouldMatchGatewayRoutes` | unit (no stack) |
| AC-25 | `.github/workflows/e2e.yml` | CI run on this feature's PR |
| AC-26 | workflow + trx artifact | manual: push a temporary failing expectation to the PR, then revert |
| AC-27 | ship-feature skill | manual: run the skill's step on this branch |
| AC-28 | ship-feature skill | review of the skill text + manual run with Docker stopped |

## 10. Risks, trade-offs & decisions
- **D-01 — Black-box project with its own DTOs, no project references.** It tests the contract that clients see. A rename inside a service doesn't break the e2e build, but a contract change fails a check. Rejected: referencing the application projects to reuse their DTOs, which would hide contract changes.
- **D-02 — Isolation with a third compose file and project `eac-e2e`.** The existing compose files stay untouched, and dev and e2e stacks coexist. Rejected:
  - parameterizing `container_name` and ports in the main override, which changes the dev setup;
  - Testcontainers, a new package that would also duplicate the compose topology.
- **D-03 — `UsesTestDefaults` / `IsTestProject` split in `Directory.Build.props`, with the e2e project opted out unless `RunE2E=true`.** The solution keeps compiling it, and the regular test run is unchanged. Rejected:
  - leaving it out of the solution, so compile breaks go unnoticed until a run;
  - skipping tests at runtime with an env var, which changes the regular run's skipped count (AC-08);
  - test-filter conventions, which `dotnet test EmployeeAttendanceControl.sln` without arguments wouldn't apply.
- **D-04 — Readiness probes through the gateway instead of compose health checks.** The probes check exactly what the tests need, and the compose files don't get new health checks. `--wait` isn't used because the APIs have no health checks.
- **D-05 — Each flow test creates its own employee.** This costs one extra wait per test (a few seconds), but the tests are order-independent and failures are isolated. The shared regular employee in the fixture is used only by the access checks.
- **D-06 — The catalog lives in the e2e project and is checked against `ocelot.json`.** This duplicates part of `OcelotRoutesTests`' catalog; accepted, so the e2e project stays self-contained (D-01).
- **D-07 — A PowerShell runner.** It works the same on the Windows dev machines and on GitHub's Ubuntu runners (`pwsh`). A bash twin would duplicate logic.
- **Risk — CI time and memory:** Cassandra and SQL Server on a GitHub runner (7 GB RAM) take several minutes to start; the readiness timeout is 300 s, configurable. Accepted in Q-03.
- **Risk — date edge:** a run near midnight UTC could mark attendance on one day and query the next. `date` is checked as today ± 1 day, and the "range includes today" query uses yesterday…tomorrow.
- **Risk — the first `pull_request` run needs the workflow on the PR branch.** GitHub runs `pull_request` workflows from the PR's head, so this feature's own PR runs it (AC-25).

## 11. Amendment: startup fixes (business spec §11)
- (~) `cassandra/init.cql` — the seed inserts into `kusers.user`, the table it creates. `kusers."User"` is a different, case-sensitive name that doesn't exist (AC-29).
- (~) `docker-compose.override.yml` — the broker's healthcheck becomes `rabbitmq-diagnostics -q check_port_connectivity`. `ping` passes about 40 s before port 5672 opens, so `service_healthy` didn't mean "accepts connections" (AC-30).
- (+) `ms.communications/ms.rabbitmq/Consumers/ConsumerSubscriber.cs` — `SubscribeAsync(CancellationToken)` calls `IConsumer.SubscribeAsync()` until it succeeds or the token is cancelled, waiting a fixed delay between attempts. Each failure is logged with `LogWarning`, the exception, the consumer type and the attempt number (AC-30, AC-31, AC-32).
- (~) `ms.communications/ms.rabbitmq/Middlewares/ConsumerMiddleware.cs` — `ApplicationStarted` starts the subscriber with `ApplicationStopping` as its token, with a 5 s delay. `ms.users` and `ms.attendances` get it through `UseRabbitConsumer`, with no change to their code.
- (+) `ms.communications/ms.communications.test/ms.rabbitmq.test` — the library's first test project, in solution folder `ms.communications` → `ms.communications.test`. No new packages.

| AC | Components | Test level |
|----|------------|------------|
| AC-29 | `init.cql` | E2E: every run starts from empty volumes, and the runner's readiness needs the admin's sign-in |
| AC-30 | healthcheck, `ConsumerSubscriber`, `ConsumerMiddleware` | unit: `SubscribeAsyncShouldRetryUntilBrokerIsAvailable`, `UseRabbitConsumerShouldSubscribeWhenApplicationStarts`; E2E: the attendance flow; manual: start a service with the broker stopped |
| AC-31 | `ConsumerSubscriber` | unit: `SubscribeAsyncShouldLogEachFailedAttempt` |
| AC-32 | `ConsumerSubscriber` | unit: `SubscribeAsyncShouldStopRetryingWhenApplicationStops` |

- **D-08 — Fix both the healthcheck and the subscription.** The healthcheck makes dependants wait for the broker on a normal start. The retry covers what a healthcheck can't: a broker that isn't up when a service starts on its own, or a service started outside compose.
- **D-09 — Retry in the shared library, not in each consumer.** The defect is in how the library starts consumers, and it fixes both services at once. Retries have no limit: a service that gives up would go back to silently not receiving messages.
- **Reconnection after the first connection** is left to `RabbitMQ.Client`, whose automatic recovery is on by default. It's verified by hand (T-60).
- **Known limit:** if a connection opens but a later step of `SubscribeAsync` fails, the next attempt replaces that connection without closing it. It's rare and bounded by the number of failed attempts, and it's left as it is to keep `IConsumer` implementations unchanged.
