# [002] API gateway (single entry point) — Tasks (TDD)

> Status: Approved (2026-10-22)
> Based on: `02-technical-spec.md` (Approved 2026-10-22)

Legend: `[TEST]` write a failing test (red) · `[IMPL]` make it pass (green) · `[REFACTOR]` · `[SETUP]` config/infra/docs

Abbreviations:
- `gw.api` = `ms.gateway/ms.gateway.api`
- `gw.test` = `ms.gateway/ms.gateway.test/ms.gateway.api.test` (namespace `ms.gateway.api.test`)

Tests: xUnit + Moq, `MethodShouldExpectedBehavior`, `// Arrange // Act // Assert`. `OcelotRoutesTests` reads the real `ocelot*.json` files, linked into the test output, and deserializes them into `Ocelot.Configuration.File.FileConfiguration` with Newtonsoft. A shared helper `LoadRoutes(fileName)` does the loading.

Operation catalog used by the tests (tech spec §3):

| # | Method | Path | Service | Auth |
|---|--------|------|---------|------|
| 1 | POST | `/Authentication` | users | none |
| 2 | GET | `/User` | users | admin |
| 3 | POST | `/User` | users | admin |
| 4 | GET | `/Employees` | employees | admin |
| 5 | POST | `/Employees` | employees | admin |
| 6 | PUT | `/Employees/AttendanceState/{attendance}` | employees | Bearer |
| 7 | GET | `/Attendances` | attendances | Bearer |
| 8 | GET | `/Attendances/ByDateRange` | attendances | admin |
| 9 | POST | `/Attendances` | attendances | Bearer |

## Group 0 — Project scaffolding (needed before any test compiles)
- [x] **T-01** `[SETUP]` `Directory.Packages.props` — add the `<!-- API gateway (ms.gateway) -->` `ItemGroup` with `<PackageVersion Include="Ocelot" Version="25.0.2" />`.
- [x] **T-02** `[SETUP]` `gw.api/ms.gateway.api.csproj` — Web SDK, net10.0, `Nullable`/`ImplicitUsings` enabled, Docker properties.
  - References: `Ocelot`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.VisualStudio.Azure.Containers.Tools.Targets`.
  - `ocelot*.json` set to `PreserveNewest`.
  - A minimal `Program.cs` (`WebApplication.CreateBuilder(args)` … `app.Run()`) so it builds.
- [x] **T-03** `[SETUP]` `gw.test/ms.gateway.api.test.csproj`:
  - Same packages as `ms-attendances.api.test`.
  - `ProjectReference` to `gw.api`.
  - `<Content Include="..\..\ms.gateway.api\ocelot*.json" Link="%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />`.
  - Add both projects to `EmployeeAttendanceControl.sln` under the `ms.gateway` and `ms.gateway.test` solution folders.
  - Build the solution.

## Group 1 — Operation catalog in container mode (AC-01, AC-09, AC-16)
- [x] **T-04** `[TEST]` `gw.test/Configuration/OcelotRoutesTests.cs`
  - `RoutesShouldExposeExactlyTheCatalogOperations` — `ocelot.json` has 9 routes; the set of (single upstream method, upstream path) equals the catalog, with no extras. (AC-01)
  - `RoutesShouldForwardToTheSameDownstreamPath` — for every route, `DownstreamPathTemplate == UpstreamPathTemplate` and `DownstreamScheme == "http"`. (AC-01)
  - `RoutesShouldTargetTheOwningServiceContainer` — users routes → `ms.users.api:80`, employees → `ms.employees.api:80`, attendances → `ms.attendances.api:80`; exactly one host per route. (AC-01, AC-16)
  - `RoutesShouldDeclareExactlyOneMethodAndNoCatchAll` — every route has one `UpstreamHttpMethod`, and no template contains `{everything}` or `/{` at the root. (AC-09)
- [x] **T-05** `[IMPL]` `gw.api/ocelot.json` — 9 routes (no auth yet) and `GlobalConfiguration.BaseUrl` `http://localhost:9000` (makes T-04 pass).

## Group 2 — Authentication and roles per route (AC-05, AC-11, AC-13, AC-14)
- [x] **T-06** `[TEST]` `gw.test/Configuration/OcelotRoutesTests.cs`
  - `LoginRouteShouldNotRequireAuthentication` — route 1 has no `AuthenticationProviderKey(s)` and no `RouteClaimsRequirement`. (AC-05)
  - `ProtectedRoutesShouldRequireBearerAuthentication` — routes 2–9 have `AuthenticationOptions.AuthenticationProviderKeys == ["Bearer"]`. (AC-11)
  - `AdminRoutesShouldRequireAdminRole` — routes 2, 3, 4, 5 and 8 have `RouteClaimsRequirement == { "role": "admin" }`. (AC-13)
  - `IdentifiedUserRoutesShouldNotRequireAnyClaim` — routes 6, 7 and 9 have an empty `RouteClaimsRequirement`. (AC-14)
- [x] **T-07** `[IMPL]` `gw.api/ocelot.json` — add `AuthenticationOptions` and `RouteClaimsRequirement` (makes T-06 pass).

## Group 3 — Local mode routes file (AC-17)
- [x] **T-08** `[TEST]` `gw.test/Configuration/OcelotRoutesTests.cs`
  - `LocalRoutesShouldMatchContainerRoutesExceptHosts` — `ocelot.Local.json` has the same routes in the same order as `ocelot.json`: upstream, downstream path, scheme, auth, claims. (AC-17, D-04)
  - `LocalRoutesShouldTargetPublishedPortsOnLocalhost` — users → `localhost:9009`, employees → `localhost:9010`, attendances → `localhost:9020`. `BaseUrl` is `http://localhost:9001`. (AC-17)
- [x] **T-09** `[IMPL]` `gw.api/ocelot.Local.json` (makes T-08 pass).

## Group 4 — Routes file selection per environment (AC-16, AC-17)
- [x] **T-10** `[TEST]` `gw.test/Extensions/ExtensionsTests.cs` (temp content root holding stub `ocelot.json` / `ocelot.Local.json` with distinct `GlobalConfiguration:BaseUrl`; `Mock<IHostEnvironment>` for `EnvironmentName` / `ContentRootPath`)
  - `AddOcelotRoutesShouldLoadLocalFileWhenEnvironmentIsLocal` — `GlobalConfiguration:BaseUrl` comes from `ocelot.Local.json`. (AC-17)
  - `AddOcelotRoutesShouldLoadDefaultFileWhenNoEnvironmentFileExists` — environment `Development` loads `ocelot.json`. (AC-16)
  - `AddOcelotRoutesShouldNotMergeBothFiles` — with `Local`, a key present only in `ocelot.json` is absent. (D-04)
- [x] **T-11** `[IMPL]` `gw.api/Extensions/Extensions.cs` → `AddOcelotRoutes(this IConfigurationManager, IHostEnvironment)` (makes T-10 pass).

## Group 5 — JWT validation at the gateway (AC-05, AC-12, AC-13)
- [x] **T-12** `[TEST]` `gw.test/Extensions/ExtensionsTests.cs` (a `ServiceCollection` with `AddOptions<SettingsOptions>` configured with a test key, then `AddGatewayAuthentication()`, then `IOptionsMonitor<JwtBearerOptions>.Get("Bearer")`)
  - `AddGatewayAuthenticationShouldRegisterBearerScheme` — `IAuthenticationSchemeProvider` has a scheme named `Bearer`. (AC-05, AC-11)
  - `AddGatewayAuthenticationShouldValidateSignatureWithConfiguredKey` — `ValidateIssuerSigningKey` is true and `IssuerSigningKey` is a `SymmetricSecurityKey` whose bytes equal the UTF-8 bytes of the test key. (AC-05, AC-12)
  - `AddGatewayAuthenticationShouldRejectExpiredTokens` — `ValidateLifetime` and `RequireExpirationTime` are true, and `ClockSkew == TimeSpan.Zero`. (AC-12)
  - `AddGatewayAuthenticationShouldKeepShortClaimNames` — `MapInboundClaims` is false. (AC-13, D-03)
  - `AddGatewayAuthenticationShouldNotValidateIssuerOrAudience` — as in the services. (AC-05)
- [x] **T-13** `[IMPL]` `gw.api/Extensions/SettingsOptions.cs`, plus `AddGatewayAuthentication` and the `GatewayAuthenticationScheme` constant in `Extensions.cs` (makes T-12 pass).

## Group 6 — Wiring & setup
- [x] **T-14** `[SETUP]` `gw.api/Program.cs` — `AddOcelotRoutes`, bind `SettingsOptions`, `AddGatewayAuthentication`, `AddOcelot`; `UseAuthentication`, `await UseOcelot()`.
- [x] **T-15** `[SETUP]` `gw.api/appsettings.json` (`Authentication:JWT:Key`, `Logging`, `AllowedHosts`), `appsettings.Development.json`, and `Properties/launchSettings.json`:
  - profile `ms.gateway.api`: `ASPNETCORE_ENVIRONMENT=Local`, `http://localhost:9001`;
  - profile `Docker`.
- [x] **T-16** `[SETUP]` `gw.api/Dockerfile` — multi-stage, as the services.
- [x] **T-17** `[SETUP]` `docker-compose.yml` and `docker-compose.override.yml` — service `ms.gateway.api`:
  - `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_HTTP_PORTS=80`;
  - `9000:80`;
  - `depends_on` the three APIs.

  The services' ports are left untouched.
- [x] **T-18** `[SETUP]` `README.md`:
  - gateway in Components and the component diagram;
  - "Gateway routes" table;
  - "Running the gateway" (container on :9000; local with `dotnet run --project ms.gateway/ms.gateway.api --environment Local` on :9001).
- [x] **T-19** `[SETUP]` `CLAUDE.md` `## SDD Conventions`:
  - add `ms.gateway` (api-only, Ocelot, no persistence) to Structure;
  - add the gateway to Inter-service communication;
  - add the test project path to Commands.

## Final verification
- [x] **T-20** `dotnet build EmployeeAttendanceControl.sln` and `dotnet test EmployeeAttendanceControl.sln`. All tests pass, existing ones included.
- [x] **T-21** Manual check, **container mode** (`docker compose up -d --build`, gateway on :9000), with a curl script:

  | AC | Request | Expected |
  |----|---------|----------|
  | AC-05 | log in admin and a non-admin test employee via gateway | tokens |
  | AC-06 | wrong password | the service's 401 "Invalid username or password" |
  | AC-02 | `GET /Employees` (admin) | same as direct :9010 |
  | AC-03 | `POST /Attendances` (employee) | record saved with the employee's `UserName` |
  | AC-04 | `GET /Attendances/ByDateRange` (admin) | same as direct :9020 |
  | AC-07 | `ByDateRange` over 31 days | service's 400 with message |
  | AC-14 | routes 6, 7, 9 as employee | 200 |
  | AC-13 | routes 2, 3, 4, 5, 8 as employee | 403, empty body |
  | AC-11 | no token on routes 2–9 | 401, empty body |
  | AC-12 | expired token / token signed with another key | 401 |
  | AC-18 | 401/403 bodies | empty |
  | AC-09 | unknown path and `DELETE /Employees` | 404 |
  | AC-08 | direct :9009/:9010/:9020 | still work |
  | AC-10 | stop `ms.attendances.api` → attendance route | 502/503; `GET /Employees` still 200; restart |
  | AC-15 | stop `ms.gateway.api` → mark attendance state directly on :9010 | still works |

- [x] **T-22** Manual check, **local mode**: `dotnet run --project ms.gateway/ms.gateway.api --environment Local` against the compose services. Log in and list employees as admin on :9001 (AC-17). Then stop the stack, and delete the test employee/user and any attendance records created.

## AC coverage
| AC | Test tasks |
|----|------------|
| AC-01 | T-04 |
| AC-02 | T-21 (manual) |
| AC-03 | T-21 (manual) |
| AC-04 | T-21 (manual) |
| AC-05 | T-06, T-12 (+ T-21) |
| AC-06 | T-21 (manual) |
| AC-07 | T-21 (manual) |
| AC-08 | T-21 (manual) |
| AC-09 | T-04 (+ T-21) |
| AC-10 | T-21 (manual) |
| AC-11 | T-06, T-12 (+ T-21) |
| AC-12 | T-12 (+ T-21) |
| AC-13 | T-06, T-12 (+ T-21) |
| AC-14 | T-06 (+ T-21) |
| AC-15 | T-21 (manual) |
| AC-16 | T-04, T-10 (+ T-21) |
| AC-17 | T-08, T-10 (+ T-22) |
| AC-18 | T-21 (manual) |

AC-02, 03, 04, 06, 07, 08, 10, 15 and 18 describe Ocelot's runtime forwarding and error behavior. With no integration tests in the project (tech spec D-06), they are verified manually against the running stack.

## Implementation notes
(Deviations from the plan discovered while implementing.)
- T-03: `dotnet sln add` also added `Debug/Release|x64|x86` platform entries for every project (≈200 lines of noise). They were removed so the `.sln` diff only adds the two new projects (+18 lines).
- T-04: added a fifth test, `RoutesShouldDeclareContainerModeBaseUrl` (`GlobalConfiguration.BaseUrl == http://localhost:9000`), to cover the `BaseUrl` that T-05 sets. Red reason: `ocelot.json` missing from the test output.
- T-06: `LoginRouteShouldNotRequireAuthentication` and `IdentifiedUserRoutesShouldNotRequireAnyClaim` were already green before T-07, because no route had auth yet. They are kept as regression guards; the two positive tests (`ProtectedRoutes…`, `AdminRoutes…`) were red as expected. The obsolete `AuthenticationProviderKey` (singular) is not used, only `AuthenticationProviderKeys`.
- T-09: `ocelot.Local.json` was generated from `ocelot.json` by replacing only the 9 `DownstreamHostAndPorts` and the `BaseUrl`; `diff` shows no other change. `LocalRoutesShouldMatchContainerRoutesExceptHosts` guards against future drift between the two files.
- T-10: `ExtensionsTests` implements `IDisposable` to delete the temp content root it creates with stub routes files.
- T-12: the test file aliases `GatewayExtensions = ms.gateway.api.Extensions.Extensions` because the test namespace `ms.gateway.api.test.Extensions` hides the class name `Extensions`. `Dispose` calls `GC.SuppressFinalize` (CA1816). Red reason: `SettingsOptions`, `AddGatewayAuthentication` and `GatewayAuthenticationScheme` did not exist.
- T-13: `SettingsOptions.cs` keeps `SettingsOptions`, `Authentication` and `JWT` in one file, mirroring `ms.attendances.api/Extensions/SettingsOptions.cs` (an existing exception to "one class per file"). The gateway only needs `JWT.Key`; `ExpirationHours` is not used because the gateway does not issue tokens.
- T-15: the gateway `appsettings.json` JWT key was checked to be identical to the three services' keys. The `Docker` launch profile sets `ASPNETCORE_ENVIRONMENT=Development`, so it uses `ocelot.json` like compose.
- T-16: the gateway Dockerfile only exposes port 80 (no 443), because the gateway is HTTP-only like the services in compose.
- T-18: README also got a PowerShell example (log in + list employees through `:9000`) and an updated component diagram (client → gateway → services; direct access shown dashed).
- T-19: the gateway test project path is documented in the Structure bullet for `ms.gateway`, rather than in Commands (the generic `dotnet test <path-to-test-csproj>` command already covers it).
- T-20: solution build 0 errors, no warnings in `ms.gateway`; full suite 111/111 passed (92 existing + 19 gateway).
- T-21: curl script (`scratchpad/t21.sh`) against the container stack, gateway on :9000, with admin and a non-admin test employee `sdd002user` created through the gateway. Result: **54 checks passed, 0 failed**.
  - AC-05: log in through the gateway; the token is accepted by the gateway.
  - AC-06: wrong password → 401 "Invalid username or password", identical to direct.
  - AC-02: `GET /Employees` body identical to direct.
  - AC-14: routes 6, 7, 9 as employee → 200.
  - AC-03: the record was saved under `sdd002user`.
  - AC-04: `ByDateRange` identical to direct.
  - AC-07: 400 body identical (except `traceId`).
  - AC-13/18: routes 2, 3, 4, 5, 8 as employee → 403 with empty body. The forbidden `POST /User` and `POST /Employees` never created their records.
  - AC-11/18: routes 2–9 without a token → 401 with empty body.
  - AC-12: expired token and token signed with another key → 401; a self-signed valid control token → 200.
  - AC-09: `GET /Unknown`, `DELETE /Employees` and `GET /Authentication` → 404.
  - AC-08: direct :9009/:9010/:9020 still work.
  - AC-10: `ms.attendances.api` stopped → 502 on the attendance route while `GET /Employees` stays 200.
  - AC-15: gateway stopped → direct `PUT :9010 /Employees/AttendanceState/false` → 200.
- T-22: `dotnet run --project ms.gateway/ms.gateway.api --environment Local` → "Hosting environment: Local", listening on :9001. Log in → 200, `GET /Employees` as admin → 200 (24 employees); no token → 401; non-admin → 403. Test data deleted afterwards (3 Mongo records, Cassandra user and SQL employee `sdd002user`; verified 0 remaining; no `t21forbidden*` records existed). Local process stopped and `docker compose down` run.
