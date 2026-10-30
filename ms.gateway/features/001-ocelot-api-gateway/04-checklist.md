# [001] API gateway (single entry point) — Implementation Checklist

> Last reviewed: 2026-10-22
> Overall: ✅ Complete (implementation and verification done; not committed yet)

Abbreviations:
- `gw.api` = `ms.gateway/ms.gateway.api`
- `gw.test` = `ms.gateway/ms.gateway.test/ms.gateway.api.test`
- `ORT` = `OcelotRoutesTests` (`gw.test/Configuration/OcelotRoutesTests.cs`)
- `ET` = `ExtensionsTests` (`gw.test/Extensions/ExtensionsTests.cs`)
- `T-21` = manual curl check, container mode (54/54 passed)
- `T-22` = manual check, local mode

## 1. Acceptance criteria
| AC | Flow | Status | Proving test(s) | Implementation |
|----|------|--------|-----------------|----------------|
| AC-01 | UC-01 main | ✅ | `ORT.RoutesShouldExposeExactlyTheCatalogOperations`, `ORT.RoutesShouldForwardToTheSameDownstreamPath`, `ORT.RoutesShouldTargetTheOwningServiceContainer` | `gw.api/ocelot.json` |
| AC-02 | UC-01 main | ✅ | T-21: `GET /Employees` body identical to direct :9010 | `gw.api/ocelot.json` (route 4) |
| AC-03 | UC-01 main, UC-02 A1 | ✅ | T-21: `POST /Attendances` as `sdd002user` saved under `sdd002user` | `gw.api/ocelot.json` (route 9), `Program.cs` |
| AC-04 | UC-01 main | ✅ | T-21: `ByDateRange` body identical to direct :9020 | `gw.api/ocelot.json` (route 8) |
| AC-05 | UC-01 A1 | ✅ | `ORT.LoginRouteShouldNotRequireAuthentication`, `ET.AddGatewayAuthenticationShouldRegisterBearerScheme`, `ET.AddGatewayAuthenticationShouldValidateSignatureWithConfiguredKey`, `ET.AddGatewayAuthenticationShouldNotValidateIssuerOrAudience`; T-21 | `gw.api/ocelot.json` (route 1), `Extensions/Extensions.cs`, `appsettings.json` |
| AC-06 | UC-01 A2 | ✅ | T-21: wrong password → 401 "Invalid username or password", identical to direct | `gw.api/ocelot.json` (route 1) |
| AC-07 | UC-01 A2 | ✅ | T-21: range > 31 days → 400, body identical except `traceId` | `gw.api/ocelot.json` (route 8) |
| AC-08 | UC-01 A3 | ✅ | T-21: direct :9009/:9010/:9020 still work | `docker-compose.override.yml` (service ports untouched) |
| AC-09 | UC-01 E1 | ✅ | `ORT.RoutesShouldDeclareExactlyOneMethodAndNoCatchAll`; T-21: `GET /Unknown`, `DELETE /Employees`, `GET /Authentication` → 404 | `gw.api/ocelot.json` |
| AC-10 | UC-01 E2 | ✅ | T-21: `ms.attendances.api` stopped → 502; `GET /Employees` still 200 | Ocelot default (no retries, D-01) |
| AC-11 | UC-02 E1 | ✅ | `ORT.ProtectedRoutesShouldRequireBearerAuthentication`, `ET.AddGatewayAuthenticationShouldRegisterBearerScheme`; T-21: routes 2–9 without token → 401 | `gw.api/ocelot.json`, `Extensions/Extensions.cs` |
| AC-12 | UC-02 E2 | ✅ | `ET.AddGatewayAuthenticationShouldValidateSignatureWithConfiguredKey`, `ET.AddGatewayAuthenticationShouldRejectExpiredTokens`; T-21: expired / foreign-key token → 401 | `Extensions/Extensions.cs` |
| AC-13 | UC-02 E3 | ✅ | `ORT.AdminRoutesShouldRequireAdminRole`, `ET.AddGatewayAuthenticationShouldKeepShortClaimNames`; T-21: routes 2, 3, 4, 5, 8 as non-admin → 403; forbidden POSTs created nothing | `gw.api/ocelot.json`, `Extensions/Extensions.cs` |
| AC-14 | UC-02 main, UC-02 A1 | ✅ | `ORT.IdentifiedUserRoutesShouldNotRequireAnyClaim`; T-21: routes 6, 7, 9 as non-admin → 200 | `gw.api/ocelot.json` |
| AC-15 | BR-09 | ✅ | T-21: gateway stopped → direct `PUT :9010 /Employees/AttendanceState/false` → 200 | no change to `ms.employees` (`Communication:External:AttendanceApiUrl` still direct) |
| AC-16 | BR-13a | ✅ | `ORT.RoutesShouldTargetTheOwningServiceContainer`, `ORT.RoutesShouldDeclareContainerModeBaseUrl`, `ET.AddOcelotRoutesShouldLoadDefaultFileWhenNoEnvironmentFileExists`; T-21 | `gw.api/ocelot.json`, `Extensions/Extensions.cs`, `Dockerfile`, `docker-compose*.yml` |
| AC-17 | BR-13b | ✅ | `ORT.LocalRoutesShouldMatchContainerRoutesExceptHosts`, `ORT.LocalRoutesShouldTargetPublishedPortsOnLocalhost`, `ET.AddOcelotRoutesShouldLoadLocalFileWhenEnvironmentIsLocal`, `ET.AddOcelotRoutesShouldNotMergeBothFiles`; T-22 | `gw.api/ocelot.Local.json`, `Extensions/Extensions.cs`, `Properties/launchSettings.json` |
| AC-18 | BR-11 | ✅ | T-21: all 401 and 403 bodies have 0 bytes | Ocelot default |

Every use-case flow (UC-01 main/A1/A2/A3/E1/E2, UC-02 main/A1/E1/E2/E3) and BR-09, BR-11 and BR-13 have at least one passing check.

## 2. Tasks
| Task | Status | Evidence / note |
|------|--------|-----------------|
| T-01 | ✅ | `Directory.Packages.props`: `<!-- API gateway (ms.gateway) -->` group, `Ocelot` 25.0.2 |
| T-02 | ✅ | `gw.api/ms.gateway.api.csproj`: versionless references, `ocelot*.json` `PreserveNewest` |
| T-03 | ✅ | `gw.test/ms.gateway.api.test.csproj` links `ocelot*.json`; `.sln` +18 lines (x64/x86 noise removed) |
| T-04 | ✅ | 5 tests in `ORT` (one extra: `RoutesShouldDeclareContainerModeBaseUrl`), pass |
| T-05 | ✅ | `ocelot.json`: 9 routes, `BaseUrl` `http://localhost:9000` |
| T-06 | ✅ | 4 tests in `ORT`, pass (2 were green before T-07 by design; kept as regression guards) |
| T-07 | ✅ | `AuthenticationOptions.AuthenticationProviderKeys ["Bearer"]` on routes 2–9; `RouteClaimsRequirement { "role": "admin" }` on 2, 3, 4, 5, 8 |
| T-08 | ✅ | 2 tests in `ORT`, pass |
| T-09 | ✅ | `ocelot.Local.json`: only hosts/ports and `BaseUrl` differ |
| T-10 | ✅ | 3 `AddOcelotRoutes…` tests in `ET`, pass |
| T-11 | ✅ | `Extensions.AddOcelotRoutes` |
| T-12 | ✅ | 5 `AddGatewayAuthentication…` tests in `ET`, pass |
| T-13 | ✅ | `Extensions/SettingsOptions.cs`, `Extensions.AddGatewayAuthentication`, `GatewayAuthenticationScheme` |
| T-14 | ✅ | `gw.api/Program.cs` |
| T-15 | ✅ | `appsettings.json` (JWT key identical to the services), `appsettings.Development.json`, `launchSettings.json` (`Local` on :9001, `Docker`) |
| T-16 | ✅ | `gw.api/Dockerfile` (multi-stage, EXPOSE 80) |
| T-17 | ✅ | `docker-compose.yml` + `docker-compose.override.yml`: `ms.gateway.api`, `9000:80`, `depends_on` the 3 APIs |
| T-18 | ✅ | `README.md`: components, diagram, "Gateway routes", "Running the API gateway" |
| T-19 | ✅ | `CLAUDE.md`: Service folders, `ms.gateway` structure bullet, inter-service gateway bullet |
| T-20 | ✅ | build 0 errors; 111/111 tests (see §4) |
| T-21 | ✅ | `scratchpad/t21.sh`: 54 passed, 0 failed |
| T-22 | ✅ | local mode on :9001: login 200, `GET /Employees` 200, no token 401, non-admin 403; test data deleted, stack stopped |

## 3. Convention compliance
- [x] Structure: `ms.gateway` follows `ms.*`; api layer only, with no application/domain/infrastructure and no persistence (documented in `CLAUDE.md`). Tests are in `ms.gateway/ms.gateway.test/ms.gateway.api.test`.
- [x] Code style: block namespaces in `Extensions.cs`, `SettingsOptions.cs` and both test files; namespace = folder path. `Program.cs` uses top-level statements, as the services do.
- [x] One class per file, with one accepted exception: `SettingsOptions.cs` holds `SettingsOptions`, `Authentication` and `JWT`. This mirrors `ms.attendances.api/Extensions/SettingsOptions.cs` (T-13 note).
- [x] Configuration via `IOptions<SettingsOptions>` bound in `Program.cs`; no raw `IConfiguration` reads for the JWT key.
- [x] DI registration in the static `Extensions` class (`AddOcelotRoutes`, `AddGatewayAuthentication`).
- [x] Authorization: same JWT (symmetric key, no issuer/audience), as in the services. Admin is enforced via `RouteClaimsRequirement`, the gateway equivalent of `[Authorize(Roles = "admin")]`.
- [x] Testing: xUnit + Moq, `<Class>Tests`, `MethodShouldExpectedBehavior` names, `// Arrange // Act // Assert`, mocks as fields, and no integration tests.
- [x] Dependencies: central package management (versions only in `Directory.Packages.props`). The new package `Ocelot` was flagged and approved as a team decision. Transitive dependencies are not pinned (user decision).
- [x] Settings in `appsettings.json`, with the container overrides in `docker-compose.override.yml`.
- [x] Inter-service: service-to-service calls stay direct (AC-15); service ports are unchanged.
- [x] Encoding: new `.cs` files have a UTF-8 BOM, like the existing ones.
- [x] Git: work done on branch `feature/002-ocelot-api-gateway`. The folder was later renumbered to `001` when feature numbers became per service.

No violations found.

## 4. Build & tests
```
dotnet test EmployeeAttendanceControl.sln
Passed: 111, Failed: 0, Skipped: 0   (10 test projects; ms.gateway.api.test: 19/19)
```
Re-run on 2026-10-22 for this checklist. Manual: T-21 `RESULT: 54 passed, 0 failed`; T-22 local mode OK.

## 5. Missing / pending
- ⏳ Nothing is committed yet (as the user asked). Pending: commit, push and PR to `master` (e.g. with `/ship-feature`).
- No functional gaps.

## 6. Deviations from spec
All of these are recorded in `03-tasks.md` → Implementation notes. None changes behavior, so the technical spec did not need updating.
- Extra test `RoutesShouldDeclareContainerModeBaseUrl` (T-04).
- Test alias `GatewayExtensions`, because the test namespace hides the class name `Extensions` (T-12).
- `SettingsOptions.cs` holds three classes, mirroring attendances (T-13).
- The gateway Dockerfile exposes only port 80 (T-16).
- The gateway test project path is documented in the Structure bullet of `CLAUDE.md`, not in Commands (T-19).

## 7. Risks & follow-ups
- **Two routes files must stay in sync.** A new or changed endpoint must be added to both `ocelot.json` and `ocelot.Local.json`, in the same order. `LocalRoutesShouldMatchContainerRoutesExceptHosts` and `RoutesShouldExposeExactlyTheCatalogOperations` fail on drift, and `CLAUDE.md` documents the rule.
- **Runtime behavior is checked only manually** (AC-02/03/04/06/07/08/10/15/18), because of the no-integration-tests convention (D-06). Follow-up for the team: in-process tests with `Microsoft.AspNetCore.Mvc.Testing` would need a new package and a convention change.
- **The JWT key is duplicated** in the `appsettings.json` of 4 projects, in plain text. Rotating it means changing all four. Follow-up: move it to a secret or environment variable shared through compose.
- **Ocelot is community-maintained** (ThreeMammals, MIT). Watch major upgrades: the singular `AuthenticationProviderKey` is already obsolete.
- **Direct ports stay open** (BR-08), so the gateway does not replace the services' own checks. Both layers authorize, by design.
- **No resilience features** (timeouts, retries, rate limiting): these are out of scope. When a service is down, the client gets 502/503.
