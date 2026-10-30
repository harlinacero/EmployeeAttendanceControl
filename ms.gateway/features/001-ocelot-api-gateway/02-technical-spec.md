# [001] API gateway (single entry point) — Technical Specification

> Status: Approved (2026-10-22)
> Based on: `01-business-spec.md` (Approved 2026-10-22)
> Reference implementation followed: `ms.attendances.api` (`Program.cs`, `Extensions/Extensions.cs` → `AddAuthenticationSettings`, `Extensions/SettingsOptions.cs`, `Dockerfile`, `Properties/launchSettings.json`) and its test project `ms-attendances.api.test`

## 1. Overview
A new service folder `ms.gateway` holds one ASP.NET Core project, `ms.gateway.api`, running the **Ocelot** 25.0.2 middleware (MIT, ThreeMammals, targets net10.0).

Routing is declarative, in `ocelot.json`: 9 routes, one per operation of BR-01, with the same upstream and downstream paths and methods (BR-01, BR-06). The query string, body and `Authorization` header are forwarded as-is (BR-05, BR-10, BR-12).

The gateway validates the existing JWT with the same symmetric key as the services, read via `IOptions<SettingsOptions>`.
- Every route except log in declares `AuthenticationOptions` with the `Bearer` scheme (BR-03).
- Admin-only routes also declare `RouteClaimsRequirement { "role": "admin" }` (BR-04).
- Services keep their own `[Authorize]` checks unchanged.

Two run modes (BR-13) are selected by `ASPNETCORE_ENVIRONMENT`, with no code change:
- **container mode:** `ocelot.json`, downstream = compose service names on port 80;
- **local mode:** `ocelot.Local.json`, downstream = `localhost` and the ports compose publishes (9009/9010/9020).

The gateway has no application/domain/infrastructure layers: it owns no business logic or data (BR-10).

## 2. Components by layer

### API (`ms.gateway/ms.gateway.api`) — new project
- (+) `ms.gateway.api.csproj` — `Microsoft.NET.Sdk.Web`, `net10.0`, `Nullable` enabled, `ImplicitUsings` enabled, Docker properties as in the other APIs.
  - Package references (no versions): `Ocelot`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.VisualStudio.Azure.Containers.Tools.Targets`.
  - `ocelot.json` / `ocelot.Local.json` set to `CopyToOutputDirectory=PreserveNewest`.
- (+) `Program.cs`:
  ```csharp
  var builder = WebApplication.CreateBuilder(args);
  builder.Configuration.AddOcelotRoutes(builder.Environment);
  builder.Services.AddOptions<SettingsOptions>().Bind(builder.Configuration);
  builder.Services.AddGatewayAuthentication();
  builder.Services.AddOcelot(builder.Configuration);
  var app = builder.Build();
  app.UseAuthentication();
  await app.UseOcelot();
  app.Run();
  ```
- (+) `Extensions/SettingsOptions.cs` — `SettingsOptions { Authentication { JWT { Key } } }` with `[Required]`, same shape and keys as the services (`Authentication:JWT:Key`).
- (+) `Extensions/Extensions.cs` — `public static class Extensions`:
  - `AddGatewayAuthentication(this IServiceCollection)` — `AddAuthentication().AddJwtBearer(GatewayAuthenticationScheme, …)`, modeled on `AddAuthenticationSettings`. It sets:
    - `MapInboundClaims = false`, so the token's `role` claim keeps that short name (see D-03);
    - `RequireHttpsMetadata = false`;
    - `ValidateIssuer = false`, `ValidateAudience = false` (as the services);
    - `ValidateIssuerSigningKey = true`, with `IssuerSigningKey` = `SymmetricSecurityKey(UTF8(Key))`;
    - `ValidateLifetime = true`, `RequireExpirationTime = true`, `ClockSkew = TimeSpan.Zero`.

    The key comes from `IOptions<SettingsOptions>`, using the same `BuildServiceProvider` pattern as the services.
  - `public const string GatewayAuthenticationScheme = "Bearer";`
  - `AddOcelotRoutes(this IConfigurationManager, IHostEnvironment)` — adds `ocelot.{EnvironmentName}.json` **instead of** `ocelot.json` when that file exists in `ContentRootPath`; otherwise adds `ocelot.json`. Only one routes file is ever loaded, which avoids index-based merging of the `Routes` array.
- (+) `ocelot.json` — container mode (§3).
- (+) `ocelot.Local.json` — local mode: same routes, same order; only `DownstreamHostAndPorts` differ.
- (+) `appsettings.json` — `Authentication:JWT:Key` (same value as the services), `Logging`, `AllowedHosts`.
- (+) `appsettings.Development.json` — logging only.
- (+) `Properties/launchSettings.json`:
  - profile `ms.gateway.api` → `ASPNETCORE_ENVIRONMENT=Local`, `applicationUrl` `http://localhost:9001`;
  - profile `Docker`, as in the other APIs.
- (+) `Dockerfile` — copy of the services' multi-stage Dockerfile (`Directory.Packages.props` + csproj restore, publish, `ENTRYPOINT ["dotnet", "ms.gateway.api.dll"]`).

### Tests (`ms.gateway/ms.gateway.test/ms.gateway.api.test`) — new project
- (+) `ms.gateway.api.test.csproj` — same packages as `ms-attendances.api.test` (xUnit, Moq, coverlet, Test.Sdk), referencing `ms.gateway.api`. The routes files are linked as content (`<Content Include="..\..\ms.gateway.api\ocelot*.json" CopyToOutputDirectory="PreserveNewest" />`) so the tests read the real files.
- (+) `Configuration/OcelotRoutesTests.cs` — deserializes both files into Ocelot's own `Ocelot.Configuration.File.FileConfiguration` (Newtonsoft, as Ocelot does) and asserts the catalog, authentication and roles (§9).
- (+) `Extensions/ExtensionsTests.cs` — builds a `ServiceCollection` with `SettingsOptions`, calls `AddGatewayAuthentication`, resolves `IOptionsMonitor<JwtBearerOptions>.Get("Bearer")` and asserts the validation parameters. It also checks `AddOcelotRoutes` picks the right file per environment, using a temp content root.

### Solution / infrastructure
- (~) `EmployeeAttendanceControl.sln` — add the `ms.gateway` solution folder with `ms.gateway.api` and `ms.gateway.test` → `ms.gateway.api.test` (`dotnet sln add`).
- (~) `Directory.Packages.props` — new dedicated `<ItemGroup>` block, labeled with a comment, for the gateway's packages (§6):
  ```xml
  <!-- API gateway (ms.gateway) -->
  <ItemGroup>
    <PackageVersion Include="Ocelot" Version="25.0.2" />
  </ItemGroup>
  ```
- (~) `docker-compose.yml` — new service `ms.gateway.api` (image `${DOCKER_REGISTRY-}msgatewayapi`, build `ms.gateway/ms.gateway.api/Dockerfile`).
- (~) `docker-compose.override.yml` — `ms.gateway.api`:
  - `container_name`;
  - `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_HTTP_PORTS=80`;
  - ports `"9000:80"`;
  - `depends_on` the three APIs (`service_started`).

  The services' published ports 9009/9010/9020 are kept (BR-08, needed for local mode).
- (~) `README.md` — gateway in Components, the component diagram, a "Gateway routes" table, and how to run each mode.
- (~) `CLAUDE.md` `## SDD Conventions` — add `ms.gateway` to the service folders (api-only, Ocelot, no persistence) and add the gateway to Inter-service communication.

No service code changes (`ms.users`, `ms.employees`, `ms.attendances` untouched).

## 3. API contract
The gateway exposes the same contracts as the services (BR-01). Route table (`ocelot.json`; `ocelot.Local.json` differs only in the downstream host/port column):

| # | Upstream (method, path) | Downstream (container mode) | Local mode | Auth at gateway |
|---|-------------------------|------------------------------|------------|-----------------|
| 1 | POST `/Authentication` | `ms.users.api:80` `/Authentication` | `localhost:9009` | none |
| 2 | GET `/User` | `ms.users.api:80` `/User` | `localhost:9009` | Bearer + `role=admin` |
| 3 | POST `/User` | `ms.users.api:80` `/User` | `localhost:9009` | Bearer + `role=admin` |
| 4 | GET `/Employees` | `ms.employees.api:80` `/Employees` | `localhost:9010` | Bearer + `role=admin` |
| 5 | POST `/Employees` | `ms.employees.api:80` `/Employees` | `localhost:9010` | Bearer + `role=admin` |
| 6 | PUT `/Employees/AttendanceState/{attendance}` | `ms.employees.api:80` `/Employees/AttendanceState/{attendance}` | `localhost:9010` | Bearer |
| 7 | GET `/Attendances` | `ms.attendances.api:80` `/Attendances` | `localhost:9020` | Bearer |
| 8 | GET `/Attendances/ByDateRange` | `ms.attendances.api:80` `/Attendances/ByDateRange` | `localhost:9020` | Bearer + `role=admin` |
| 9 | POST `/Attendances` | `ms.attendances.api:80` `/Attendances` | `localhost:9020` | Bearer |

All routes use `DownstreamScheme: http`, a single `UpstreamHttpMethod` and the default (case-insensitive) matching. Query strings (`?userName=`, `?from=&to=`) are forwarded automatically.

Example route:
```json
{
  "UpstreamPathTemplate": "/Attendances/ByDateRange",
  "UpstreamHttpMethod": [ "Get" ],
  "DownstreamPathTemplate": "/Attendances/ByDateRange",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [ { "Host": "ms.attendances.api", "Port": 80 } ],
  "AuthenticationOptions": { "AuthenticationProviderKeys": [ "Bearer" ] },
  "RouteClaimsRequirement": { "role": "admin" }
}
```
`GlobalConfiguration.BaseUrl`: `http://localhost:9000` (`ocelot.json`) and `http://localhost:9001` (`ocelot.Local.json`).

Gateway-produced outcomes (from Ocelot's error mapping):

| Case | Status | Body |
|------|--------|------|
| No/invalid/expired token on a protected route | 401 | empty |
| Valid token without `role=admin` on an admin route | 403 | empty |
| Path/method not in the table | 404 | empty |
| Downstream unreachable (connection refused / DNS) | 502 | empty |
| Downstream timeout (Ocelot default 90 s) | 503 | empty |

Any other status comes from the service itself, unchanged.

## 4. Data model & persistence
None. The gateway is stateless (BR-10).

## 5. Inter-service impact
| Service | Change | Mechanism (event / HTTP) | Contract |
|---------|--------|--------------------------|----------|
| ms.users | None; reached through the gateway at the same routes | HTTP (gateway → service) | unchanged |
| ms.employees | None; its Refit call to ms.attendances keeps using `Communication:External:AttendanceApiUrl=http://ms.attendances.api:80` (direct, BR-09) | HTTP | unchanged |
| ms.attendances | None | HTTP (gateway → service) | unchanged |

## 6. Configuration & dependencies
- **New package: `Ocelot` 25.0.2** — requested and approved by the user (2026-10-22). It goes in its own `<!-- API gateway (ms.gateway) -->` `ItemGroup` in `Directory.Packages.props` and is referenced without a version.
  - Ocelot's transitive dependencies are resolved by NuGet from Ocelot's own package metadata. The repo doesn't use transitive pinning (`CentralPackageTransitivePinningEnabled`), so they aren't listed.
  - `Microsoft.AspNetCore.Authentication.JwtBearer` is already pinned in the shared block and is reused.
  - It brings these transitive dependencies: `Microsoft.AspNetCore.Mvc.NewtonsoftJson` 10.0.13, `Microsoft.AspNetCore.MiddlewareAnalysis` 10.0.13 and `Microsoft.Extensions.DiagnosticAdapter` 3.1.32.
  - The test project also uses Ocelot's `FileConfiguration` types through the project reference.
- Config keys (gateway):
  - `Authentication:JWT:Key` → `SettingsOptions.Authentication.JWT.Key`. It has the same value as the services today, since the secret is already in each service's `appsettings.json` (see §10 risk).
  - `ocelot*.json` is loaded by `AddOcelotRoutes` and consumed by Ocelot (`Routes`, `GlobalConfiguration`). It is the library's own configuration format, so it doesn't go through `IOptions`.
- Run-mode selection (BR-13):
  - **Container mode:** `ASPNETCORE_ENVIRONMENT=Development` (compose) → no `ocelot.Development.json` → `ocelot.json` (service names, port 80).
  - **Local mode:** `ASPNETCORE_ENVIRONMENT=Local` (launch profile `ms.gateway.api`, or `dotnet run --environment Local`) → `ocelot.Local.json` (`localhost:9009/9010/9020`). It requires the services to run in compose with their published ports.
- DI: `AddOptions<SettingsOptions>`, `AddGatewayAuthentication`, `AddOcelot`. No MediatR, AutoMapper or RabbitMQ.
- docker-compose: new `ms.gateway.api` service on `9000:80`.

## 7. Security
- Authentication at the gateway uses JWT bearer, validated with the same symmetric key, lifetime and clock-skew rules as the services (BR-03, BR-12).
- Authorization at the gateway is by route: `RouteClaimsRequirement { "role": "admin" }` on routes 2, 3, 4, 5 and 8 (BR-04). Routes 6, 7 and 9 only need authentication, and route 1 is anonymous (BR-02).
- Defense in depth: the `Authorization` header is forwarded unchanged and every service re-validates it with its own `[Authorize]` attributes (BR-05).
- Rejections carry no body (BR-11).
- Direct service ports remain open (BR-08). The gateway is not a security boundary for direct callers, which is accepted per the business spec.

## 8. Error handling & logging
- Errors produced by the gateway are listed in §3 (401/403/404/502/503). Ocelot logs them through `ILogger` (default ASP.NET Core logging, `Logging` section in `appsettings.json`).
- No custom error middleware; service errors pass through unchanged (BR-05, BR-10).
- No retries or QoS (out of scope).

## 9. Traceability
| AC | Components | Test level (unit / manual) |
|----|------------|----------------------------|
| AC-01 catalog = 9 operations | `ocelot.json` | unit: `OcelotRoutesTests` (exact set of upstream method+path, downstream path, host/port per service) |
| AC-02 list employees = same result | route 4 | manual (stack): compare gateway vs direct |
| AC-03 identity reaches service | route 9; `Authorization` header forwarded | manual: record via gateway, check `UserName` |
| AC-04 query inputs forwarded | route 8 | manual: same result via gateway and direct |
| AC-05 log in via gateway; token accepted | route 1 (no auth); `AddGatewayAuthentication` key | unit: route 1 has no `AuthenticationOptions`; `ExtensionsTests` (key, validation). Manual: log in, then call route 4 |
| AC-06 wrong credentials → service's rejection | route 1 | manual |
| AC-07 > 31 days → service's 400 | route 8 | manual |
| AC-08 direct access unchanged | compose keeps 9009/9010/9020 | manual |
| AC-09 unknown path / method → 404 | `ocelot.json` (single method per route, no catch-all) | unit: every route has exactly one method and no `{everything}` template. Manual: unknown path and `DELETE /Employees` → 404 |
| AC-10 service down → unavailable; others work | Ocelot error mapping | manual: stop `ms.attendances.api` → 502; `GET /Employees` still 200 |
| AC-11 anonymous → 401, not forwarded | `AuthenticationOptions` on routes 2–9 | unit: routes 2–9 declare `Bearer`. Manual: 401 |
| AC-12 expired / foreign token → 401 | `AddGatewayAuthentication` (`ValidateLifetime`, `ClockSkew=0`, signing key) | unit: `ExtensionsTests`. Manual: token signed with another key → 401 |
| AC-13 non-admin on admin routes → 403 | `RouteClaimsRequirement` on 2, 3, 4, 5, 8; `MapInboundClaims=false` | unit: exactly those routes require `role=admin`; `ExtensionsTests` asserts `MapInboundClaims == false`. Manual: 403 ×5 |
| AC-14 non-admin on 6, 7, 9 → forwarded | routes 6, 7, 9 without claims requirement | unit. Manual: 200 ×3 |
| AC-15 employees → attendances stays direct | compose env `AttendanceApiUrl` unchanged | unit: no service config touched (review). Manual: mark attendance works with the gateway stopped |
| AC-16 container mode | `ocelot.json`, compose service | unit: `ocelot.json` hosts = compose service names, port 80; `AddOcelotRoutes` picks `ocelot.json` for `Development`. Manual: login + list employees on :9000 |
| AC-17 local mode | `ocelot.Local.json`, launch profile | unit: Local file = same routes, hosts `localhost` with 9009/9010/9020; `AddOcelotRoutes` picks it for `Local`. Manual: `dotnet run --environment Local` on :9001 |
| AC-18 rejection without detail | Ocelot 401/403 | manual: empty body |

## 10. Risks, trade-offs & decisions
- **D-01 — Ocelot (requested) over YARP.** Declarative per-route auth and claims fit the BR-01 catalog directly. YARP would need code or policies per route. Ocelot is community-maintained (ThreeMammals), not Microsoft.
- **D-02 — One route per operation (9 routes), no catch-all `/{everything}`.** This makes the catalog explicit, gives "not found" for anything else (BR-06), and allows a different auth rule per operation even on the same path (`GET`/`POST /User`).
- **D-03 — `MapInboundClaims = false` and claim key `role`.** The users service writes `ClaimTypes.Role`, which serializes as `role` in the JWT. With inbound mapping on, it becomes `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`. A colon in a JSON config key is read by .NET configuration as a section separator, so `RouteClaimsRequirement` can't express it reliably. Turning mapping off at the gateway only keeps the short name `role`; services are unaffected.
- **D-04 — Two complete routes files, one loaded per environment, instead of overriding hosts.**
  - .NET configuration merges JSON arrays by index, so a partial `ocelot.Local.json` overlaid on `ocelot.json` would be fragile.
  - Overriding hosts with env vars (`Routes__3__DownstreamHostAndPorts__0__Host`) would be index-coupled and unreadable.
  - Cost: the 9 routes are duplicated. Mitigation: a unit test asserts both files have identical routes except hosts and ports.
- **D-05 — Container mode uses `ASPNETCORE_ENVIRONMENT=Development`**, like the other containers. Local mode uses a dedicated `Local` environment.
- **D-06 — No integration tests (project convention).** Ocelot's runtime behavior (401/403/404/502, forwarding) is library behavior. It is verified manually against the stack with a curl script. Unit tests cover our configuration and authentication setup. In-process tests with `Microsoft.AspNetCore.Mvc.Testing` would be another new package and a convention change; it is left as a team decision.
- **D-07 — Gateway port 9000 (container) and 9001 (local)**, so both can run at the same time without clashing.
- **Risk:** the JWT key is duplicated in a fourth `appsettings.json`. That matches the current pattern, but a key rotation must touch 4 files. Follow-up: move it to an env var or secret in compose.
- **Risk:** the 502 vs 503 distinction (unreachable vs timeout) is Ocelot's. Both satisfy "service unavailable" (BR-07).
- **Risk:** direct ports stay open (BR-08), so clients can bypass the gateway. Accepted; hiding them is out of scope and would break local mode.
