# [001] Shared build configuration for the whole solution — Implementation Checklist

> Last reviewed: 2026-10-30
> Overall: ✅ Complete. All 15 acceptance criteria met (AC-06 reworded, §6).

## 1. Acceptance criteria
"Snapshot" = evaluated properties and items of the 31 `.csproj` + `docker-compose.dcproj` (`dotnet msbuild -getProperty/-getItem`), baseline vs after (D-02).

| AC | Flow | Status | Proof | Implementation |
|----|------|--------|-------|----------------|
| AC-01 | UC-01 main | ✅ | Build 0 errors; snapshot: TargetFramework, ImplicitUsings, AssemblyName, RootNamespace, Compile items identical | `Directory.Build.props`, 31 `.csproj` |
| AC-02 | UC-01 main | ✅ | `dotnet test` 175 passed / 13 projects = baseline | — |
| AC-03 | UC-01 A2 | ✅ | Snapshot: `Nullable` identical for all 31 (11 `disable`) | 11 `.csproj` keep `<Nullable>disable</Nullable>` |
| AC-04 | UC-01 A1 | ✅ | `dotnet test ms.users.api.test.csproj` alone → 11 passed | — |
| AC-05 | UC-01 A3 | ✅ | `check.sh`: no `.csproj` declares `TargetFramework`/`ImplicitUsings`; snapshot still `net10.0` | `Directory.Build.props` |
| AC-06 | UC-01 E1 | ✅ | Broken props → build fails (`NETSDK1013` with plain `dotnet build`); `--no-restore` / `dotnet msbuild` → `MSB4024` naming `Directory.Build.props` | MSBuild restore behavior, see §6 |
| AC-07 | UC-01 E2 | ✅ | Snapshot: `docker-compose.dcproj` gets no TargetFramework, test or Docker props | `.csproj` guard condition |
| AC-08 | UC-02 main, E1 | ✅ | Red: gateway image failed at `restore` before the Dockerfile fix. Green: 5 images built, 4 APIs up; assistant stops on missing `ApiKey` (no local `.env`, pre-existing) | 5 `Dockerfile` |
| AC-09 | UC-03 main | ✅ | Snapshot: 13 test projects with IsPackable=false, 5 packages, `Using Xunit`; extra packages unchanged | test block |
| AC-10 | UC-03 main | ✅ | Snapshot: 5 APIs with Linux / `..\..` / `..\..\docker-compose.dcproj`; only `ms.users.api` differs from baseline (the fix) | web block |
| AC-11 | UC-03 main | ✅ | Snapshot: libraries have no test packages and no Docker props | conditions |
| AC-12 | UC-03 A1 | ✅ | Same as AC-03 (override wins) | 11 `.csproj` |
| AC-13 | UC-03 E1 | ✅ | Review: `CLAUDE.md` "Dependencies & config" states the `.test` rule and what is shared; root `README.md` section | `CLAUDE.md`, `README.md` |
| AC-14 | BR-07 | ✅ | `check.sh`: no `Version=` in `Directory.Build.props` | — |
| AC-15 | BR-09 | ✅ | `check.sh`: no shared setting repeated, no duplicate `Dapper`, no `<Folder Include>`, no `LangVersion`, no empty group; `UserConsumerTests.cs` still `None` | 31 `.csproj` |

## 2. Tasks
| Task | Status | Evidence / note |
|------|--------|-----------------|
| T-01 | ✅ | baseline snapshot of 31 projects (1043 lines), 175 tests |
| T-02 | ✅ | `check.sh` failed before the change (196 findings) |
| T-03 | ✅ | `UsingMicrosoftNETSdkWeb` visible in the props; D-03 confirmed |
| T-04 | ✅ | `Directory.Build.props` |
| T-05 | ✅ | build OK, only expected `NU1504` duplicates, no property changes |
| T-06 | ✅ | dcproj unaffected |
| T-07 | ✅ | 31 `.csproj` trimmed (−318 lines), BOM/indentation kept |
| T-08 | ✅ | `Dapper` duplicate, 5 `<Folder>`, `LangVersion` removed |
| T-09 | ✅ | `check.sh` passes; warnings 21 → 20 unique, none new; snapshot diff = 3 lines (§6) |
| T-10 | ✅ | 11 passed |
| T-11 | ✅ | see AC-06 |
| T-12 | ✅ | red: `NETSDK1013` at `dotnet restore` in the gateway image |
| T-13 | ✅ | `COPY ["Directory.Build.props", "./"]` in 5 Dockerfiles |
| T-14 | ✅ | `docker compose up -d --build` |
| T-15 | ✅ | `CLAUDE.md` root `features/` rule |
| T-16 | ✅ | `CLAUDE.md` Dependencies & config |
| T-17 | ✅ | `README.md` "Shared build configuration" |
| T-18 | ✅ | §4 |

## 3. Convention compliance
- [x] Central package management: no versions in `Directory.Build.props` or `.csproj`.
- [x] No new packages.
- [x] Existing spellings and names untouched (`infraestucture`, `ms-attendances.test`, `RootNamespace ms_attendances.api.test`).
- [x] Test layout unchanged: xUnit + Moq + coverlet, one test project per layer.
- [x] Docs: root `README.md` updated. No service README needed, because no service behavior, API or config changed.
- [x] Feature folder in root `features/` (new rule added to `CLAUDE.md`); branch `feature/001-directory-build-props`.
- [x] No source or test code changed.

## 4. Build & tests
```
dotnet build EmployeeAttendanceControl.sln   → 0 Errores
dotnet test EmployeeAttendanceControl.sln    → 13 test projects, 175 passed, 0 failed, 0 skipped (baseline: 175)
docker compose up -d --build                 → 5 API images built; users/employees/attendances/gateway running
```

## 5. Missing / pending
- None.

## 6. Deviations from spec
- **AC-06 / T-11:** the implicit restore of `dotnet build` hides `MSB4024`, so the first error is `NETSDK1013`. AC-06 and UC-01 E1 reworded in the business spec (decision 2026-10-30); tech spec §8 and T-11 updated.
- **`ms.employees.infraestucture.test`** now gets the global `using Xunit` like the other 12 test projects; before, it was the only one without it. Its files keep their own `using Xunit;`. No warnings, same 9 tests.
- **`LangVersion`** of `ms.employees.infraestucture.test` evaluates to `14.0` instead of `latest`, which is the same C# version on .NET 10.
- **`xunit.runner.visualstudio`** in `ms.users.api.test` loses `PrivateAssets/IncludeAssets`. No project references a test project, so there is no effect. As planned in the tech spec.

## 7. Risks & follow-ups
- Visual Studio may need a solution reload after pulling the change.
- `ms.assistant.api` needs a local `.env` with `ANTHROPIC_API_KEY` to start in Docker. This is pre-existing and not caused by this feature.
- Follow-up (optional): commit the snapshot/check scripts under `build/` so later build changes can be verified the same way (D-02).
- Follow-up (Q-04): `ms.users.api.test` still excludes `Consumers/UserConsumerTests.cs` from compilation.
