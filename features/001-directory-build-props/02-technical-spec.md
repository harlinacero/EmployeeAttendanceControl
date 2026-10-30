# [001] Shared build configuration for the whole solution — Technical Specification

> Status: Approved (2026-10-30)
> Based on: `01-business-spec.md` (Approved 2026-10-30)
> Reference implementation followed: root `Directory.Packages.props` (central package management, the existing solution-wide MSBuild file); MSBuild docs "Customize your build by directory" (`Directory.Build.props`)

## 1. Overview
Add a root `Directory.Build.props`. MSBuild imports it automatically, early in `Microsoft.Common.props`, into every project below the repository root, before the project body is evaluated. A value set in the `.csproj` therefore overrides the shared default (BR-04).

The file has three conditional blocks:
1. **Common** — every `.csproj`: `TargetFramework`, `ImplicitUsings`, `Nullable=enable`.
2. **Test projects** — `$(MSBuildProjectName)` ends in `.test`: `IsPackable=false`, the five common test packages (no versions, CPM), and `<Using Include="Xunit" />`.
3. **Web API projects** — `$(UsingMicrosoftNETSdkWeb)` is `true`: container settings.

The whole file is guarded by `'$(MSBuildProjectExtension)' == '.csproj'`, so `docker-compose.dcproj` (Microsoft.Docker.Sdk, also at the root) is untouched (BR-10).

Each `.csproj` is then trimmed to what is specific to it. The 5 Dockerfiles copy the new file before `dotnet restore`.

No source code, test code, route, contract or runtime config changes.

## 2. Components

### Root
- (+) `Directory.Build.props`:
  ```xml
  <Project>
  	<!-- Shared build settings for every .csproj in the solution.
  	https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory
  	A value set in a project file overrides the default here. Package versions live in Directory.Packages.props. -->
  	<PropertyGroup Condition="'$(MSBuildProjectExtension)' == '.csproj'">
  		<TargetFramework>net10.0</TargetFramework>
  		<ImplicitUsings>enable</ImplicitUsings>
  		<Nullable>enable</Nullable>
  		<IsTestProject Condition="$(MSBuildProjectName.EndsWith('.test'))">true</IsTestProject>
  	</PropertyGroup>

  	<!-- Test projects: name ends in .test -->
  	<PropertyGroup Condition="'$(IsTestProject)' == 'true'">
  		<IsPackable>false</IsPackable>
  	</PropertyGroup>
  	<ItemGroup Condition="'$(IsTestProject)' == 'true'">
  		<PackageReference Include="coverlet.collector" />
  		<PackageReference Include="Microsoft.NET.Test.Sdk" />
  		<PackageReference Include="Moq" />
  		<PackageReference Include="xunit" />
  		<PackageReference Include="xunit.runner.visualstudio" />
  		<Using Include="Xunit" />
  	</ItemGroup>

  	<!-- Web API projects (Microsoft.NET.Sdk.Web): container tooling, APIs live at <svc>/<svc>.api -->
  	<PropertyGroup Condition="'$(MSBuildProjectExtension)' == '.csproj' And '$(UsingMicrosoftNETSdkWeb)' == 'true'">
  		<DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
  		<DockerfileContext>..\..</DockerfileContext>
  		<DockerComposeProjectPath>..\..\docker-compose.dcproj</DockerComposeProjectPath>
  	</PropertyGroup>
  </Project>
  ```
  (Final shape may move a condition if verification (T-03) shows `UsingMicrosoftNETSdkWeb` isn't set yet at props time; see D-03.)
- (~) `CLAUDE.md` — "Dependencies & config": shared build settings live in `Directory.Build.props`; a `.csproj` only declares what differs (e.g. `Nullable=disable` in application/domain/infra) plus its own packages/references; test projects must end in `.test`; new projects follow this. Features-folder rule for solution-wide features (done at stage 1 approval).
- (~) `README.md` — short "Build configuration" note: `Directory.Build.props` + `Directory.Packages.props`, what each holds.

### Project files (31 `.csproj`) — remove what the shared file now provides
| Group | Remove | Keep |
|-------|--------|------|
| 5 web APIs (`ms.*.api`) | `TargetFramework`, `Nullable`, `ImplicitUsings`, `DockerDefaultTargetOS`, `DockerfileContext`, `DockerComposeProjectPath` | `UserSecretsId`, packages, project refs, gateway `Content Update="ocelot*.json"` |
| 11 layers with nullable off (application/domain/infra of assistant, attendances, employees; application/domain of users) | `TargetFramework`, `ImplicitUsings` | `<Nullable>disable</Nullable>` (BR-03), packages, refs |
| `ms.users.infraestructure`, `ms.rabbitmq` | `TargetFramework`, `ImplicitUsings`, `Nullable` | packages, refs |
| 13 test projects | `TargetFramework`, `ImplicitUsings`, `Nullable`, `IsPackable`, the 5 common test packages, `<Using Include="Xunit" />` | extra packages (AutoMapper, MediatR, Microsoft.Extensions.*, RabbitMQ.Client), project refs, `RootNamespace` (ms-attendances.api.test), `FrameworkReference` (ms.assistant.api.test), gateway `Content Include ocelot*.json`, `<None Include="Consumers\UserConsumerTests.cs" />` (ms.users.api.test, Q-04) |

Clean-up (BR-09):
- `ms.employees.infraestucture.csproj`: second `<PackageReference Include="Dapper" />` (inside the project-ref `ItemGroup`).
- Empty `<Folder Include>`: `ms.attendances.application` (`Extensions\`), `ms.attendances.infraestucture.test` (`Mappers\`), `ms.employees.infraestucture.test` (`SqlData\`), `ms.users.domain` (`Settings\`), `ms.users.api.test` (`Events\`). They only affect the VS tree view.
- `ms.employees.infraestucture.test`: `<LangVersion>latest</LangVersion>` (net10 default is the latest C#, so the effective version is the same).
- `ms.users.api.test`: `PrivateAssets`/`IncludeAssets` on `xunit.runner.visualstudio` disappear with the reference; they only affect projects that reference a test project, and none does.
- Empty `PropertyGroup`/`ItemGroup` left behind are removed. The UTF-8 BOM / indentation of each file is kept as is.

### Dockerfiles (5)
- (~) `ms.{users,employees,attendances,gateway,assistant}/ms.*.api/Dockerfile`: add `COPY ["Directory.Build.props", "./"]` next to the existing `COPY ["Directory.Packages.props", "./"]`. Without it, `dotnet restore` (which runs before `COPY . .`) would see projects with no `TargetFramework` and fail (UC-02 E1). `.dockerignore` doesn't exclude the file.

### API / Application / Domain / Infrastructure
No changes.

## 3. API contract
No changes.

## 4. Data model & persistence
No changes.

## 5. Inter-service impact
None. Build-time only; Refit clients, events and gateway routes are untouched.

## 6. Configuration & dependencies
- No new packages. No versions in `Directory.Build.props` (BR-07, AC-14).
- `docker-compose*.yml`: no changes (build context is already the repo root).
- The `ms.users.api` path to `docker-compose.dcproj` becomes correct (`..\..\`, was `..\`) (AC-10).

## 7. Security
No impact. `UserSecretsId` stays in each API project.

## 8. Error handling & logging
- A malformed `Directory.Build.props` fails every project's evaluation with `MSB4024` naming `Directory.Build.props` (AC-06). Verified manually, then reverted. *Found while implementing (T-11):* a plain `dotnet build` runs an implicit restore that ignores invalid imports, so the first error shown is `NETSDK1013` (empty `TargetFramework`); the `MSB4024` message appears with `dotnet build --no-restore` or `dotnet msbuild`. The build still never succeeds without the shared file.
- A test project not ending in `.test` gets no test packages → compile errors on `Xunit`/`Fact` (UC-03 E1). Documented in CLAUDE.md (AC-13).
- A project that declares one of the five test packages itself would duplicate the item → NuGet warning `NU1504`. The build is checked for it.

## 9. Traceability
Verification is by **MSBuild evaluation snapshots** plus build and test runs (D-02).

`snapshot` = for each `.csproj`, `dotnet msbuild <proj> -getProperty:TargetFramework,ImplicitUsings,Nullable,IsPackable,IsTestProject,AssemblyName,RootNamespace,DockerDefaultTargetOS,DockerfileContext,DockerComposeProjectPath -getItem:PackageReference -getItem:Using`, normalized and sorted, taken **before** (baseline, now) and **after**.

| AC | Components | Verification |
|----|------------|--------------|
| AC-01 | `Directory.Build.props`, 31 csproj | `dotnet build` OK; snapshot diff shows no change in TargetFramework/ImplicitUsings/AssemblyName/RootNamespace |
| AC-02 | all | `dotnet test` → 175 passed, 13 projects (baseline: 175 / 13) |
| AC-03, AC-12 | 11 csproj keep `Nullable=disable` | snapshot diff: `Nullable` unchanged for all 31 |
| AC-04 | test projects | `dotnet test ms.users/ms.users.test/ms.users.api.test/ms.users.api.test.csproj` → 11 passed |
| AC-05 | `Directory.Build.props` | `grep -L` shows no csproj declares `TargetFramework`/`ImplicitUsings`; snapshot still `net10.0` |
| AC-06 | `Directory.Build.props` | manual: break the XML, build → fails; `--no-restore` → error names the file; revert |
| AC-07 | guard condition | `dotnet msbuild docker-compose.dcproj -getProperty:TargetFramework,IsPackable` → empty |
| AC-08 | Dockerfiles | `docker compose build` → 5 images OK; `docker compose up -d` → services start |
| AC-09 | test block | snapshot: 13 test projects have IsPackable=false, the 5 packages, `Using Xunit`; extra packages (AutoMapper, MediatR…) unchanged and only where declared |
| AC-10 | web block | snapshot: 5 APIs have Linux, `..\..`, `..\..\docker-compose.dcproj`; only `ms.users.api` differs from baseline (the fix) |
| AC-11 | conditions | snapshot: libraries have no test packages and no Docker properties |
| AC-13 | `CLAUDE.md` | review the new convention text |
| AC-14 | `Directory.Build.props` | no `Version` attribute in the file |
| AC-15 | csproj | grep: no duplicated `Dapper`, no `<Folder Include`, no `LangVersion`, no setting provided by the shared file; `UserConsumerTests.cs` still `None` |

**Expected snapshot differences** (anything else is a defect): `ms.users.api` `DockerComposeProjectPath`; `ms.employees.infraestucture.test` `LangVersion` (not captured: same effective version); `IsTestProject` may become `true` on test projects where the test SDK didn't already set it (it does already via `Microsoft.NET.Test.Sdk`, so likely none); `xunit.runner.visualstudio` metadata in `ms.users.api.test`.

## 10. Risks, trade-offs & decisions
- **D-01 — Name-based test detection (`*.test`), not `IsTestProject`.** `IsTestProject` is set by the `Microsoft.NET.Test.Sdk` package's build props, i.e. only *after* the package is referenced — but here the shared file is what adds that package. So the name is the only signal available at props time. Matches the existing naming convention (all 13 test projects end in `.test`). Setting `IsTestProject=true` explicitly from the name keeps tooling consistent.
- **D-02 — Verification by evaluation snapshots, no new xUnit test.** The convention is xUnit + Moq unit tests of application code; MSBuild evaluation can't be unit-tested without a new package (`Microsoft.Build` / `Microsoft.Build.Locator`, a team decision) and a heavy test. The "tests first" step is therefore: take the baseline snapshot **before** any change (red/green = diff against baseline). The snapshot script lives in the session scratchpad and isn't committed. Alternative rejected: committing a verification script under `build/` — no other tooling scripts exist in the repo; can be a follow-up.
- **D-03 — Web detection via `UsingMicrosoftNETSdkWeb`.** The Web SDK sets it in its `Sdk.props` before importing the base SDK props (which import `Directory.Build.props`). Verified in T-03; if it turns out unset at that point, fall back to the project-name rule `$(MSBuildProjectName.EndsWith('.api'))` (all 5 APIs end in `.api`, no test project does since they end in `.test`).
- **D-04 — Relative Docker paths (`..\..`).** Same values the projects use today; all APIs live at `<svc>/<svc>.api`. A future API at another depth must override them (documented). Absolute paths from `$(MSBuildThisFileDirectory)` were rejected: VS container tooling expects paths relative to the project.
- **D-05 — Nullable default `enable`, 11 projects override with `disable`.** Matches BR-03 with the fewest overrides (20 projects use `enable`). A layer-based rule (by name suffix) was rejected: `ms.users.infraestructure` is the exception and would need its own override anyway, and the rule would hide an intentional per-project choice.
- **Risk — Visual Studio caches evaluation.** After pulling, VS may need a solution reload to pick up `Directory.Build.props`. Command-line builds are unaffected.
- **Risk — nested `Directory.Build.props`.** MSBuild stops at the first one found walking up. A future service-level `Directory.Build.props` must import the root one explicitly (`<Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />`). Documented in CLAUDE.md.
