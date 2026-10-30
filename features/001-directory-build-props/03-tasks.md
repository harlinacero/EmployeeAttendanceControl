# [001] Shared build configuration for the whole solution — Tasks (TDD)

> Status: Approved (2026-10-30)
> Based on: `02-technical-spec.md` (Approved 2026-10-30)

Legend: `[TEST]` write a failing check (red) · `[IMPL]` make it pass (green) · `[REFACTOR]` · `[SETUP]` config/infra/docs

Per D-02, "tests" are **evaluation snapshots and build/test runs**, not new xUnit tests. `snapshot.sh` (session scratchpad, not committed) writes, for each of the 31 `.csproj`, the evaluated properties `TargetFramework, ImplicitUsings, Nullable, IsPackable, IsTestProject, AssemblyName, RootNamespace, DockerDefaultTargetOS, DockerfileContext, DockerComposeProjectPath` and the items `PackageReference` (identity only) and `Using`, sorted, to one file. `check.sh` holds the static checks (grep) of AC-05, AC-14 and AC-15.

## Group 1 — Baseline (before any change)
- [x] **T-01** `[TEST]` Write `snapshot.sh` and take `baseline.txt` from the current `master` state. Record baseline test result: 175 passed / 13 test projects (already measured). (AC-01, AC-02, AC-03, AC-09, AC-10, AC-11)
- [x] **T-02** `[TEST]` Write `check.sh` (static checks) and run it: it must **fail** now — csproj files declare `TargetFramework`/`ImplicitUsings`, `Directory.Build.props` doesn't exist, duplicated `Dapper`, `<Folder Include` and `LangVersion` present. Also asserts `UserConsumerTests.cs` is still `None` in `ms.users.api.test` (passes now, must keep passing). (AC-05, AC-14, AC-15)

## Group 2 — Shared file
- [x] **T-03** `[TEST]` Check D-03: `dotnet msbuild ms.gateway/ms.gateway.api/ms.gateway.api.csproj -getProperty:UsingMicrosoftNETSdkWeb` after a probe props sets a marker property only when `UsingMicrosoftNETSdkWeb == true` → marker is set for the 5 APIs, empty for a library. If not, switch the web condition to the `.api` name rule and note it in the tech spec. (AC-10, AC-11)
- [x] **T-04** `[IMPL]` `Directory.Build.props` (root) per tech spec §2: common block, test block, web block, `.csproj` guard, no versions. (AC-01, AC-07, AC-09, AC-10, AC-11, AC-14)
- [x] **T-05** `[TEST]` With the props added but csproj files unchanged: `dotnet build EmployeeAttendanceControl.sln` succeeds and snapshot diff vs baseline shows only the expected differences (tech spec §9). Duplicated items from the props + csproj are expected to raise `NU1504` here; they disappear in T-07. (AC-01, AC-03, AC-09, AC-10)
- [x] **T-06** `[TEST]` `dotnet msbuild docker-compose.dcproj -getProperty:TargetFramework,IsPackable,DockerfileContext` → all empty. (AC-07)

## Group 3 — Trim the project files
- [x] **T-07** `[IMPL]` Trim the 31 `.csproj` per tech spec §2 table: 5 APIs, 11 nullable-off layers (keep `<Nullable>disable</Nullable>`), `ms.users.infraestructure`, `ms.rabbitmq`, 13 test projects (keep extra packages, `RootNamespace`, `FrameworkReference`, gateway `Content`, `UserConsumerTests.cs` `None`). Keep BOM/indentation. (makes T-02 pass; AC-05, AC-12, AC-15)
- [x] **T-08** `[IMPL]` Clean-up: remove second `Dapper` (`ms.employees.infraestucture`), 5 empty `<Folder Include>`, `LangVersion` (`ms.employees.infraestucture.test`), empty groups. (AC-15)
- [x] **T-09** `[TEST]` Re-run: `check.sh` passes; build has no `NU1504` / new warnings vs baseline; snapshot diff vs baseline = expected differences only. (AC-01, AC-03, AC-05, AC-09, AC-10, AC-11, AC-12, AC-14, AC-15)

## Group 4 — Failure and single-project paths
- [x] **T-10** `[TEST]` `dotnet test ms.users/ms.users.test/ms.users.api.test/ms.users.api.test.csproj` alone → 11 passed. (AC-04)
- [x] **T-11** `[TEST]` Temporarily break `Directory.Build.props` (unclosed element), `dotnet build` a library project → build fails; with `--no-restore` the error references `Directory.Build.props`; restore the file and `git diff` it clean. (AC-06)

## Group 5 — Containers
- [x] **T-12** `[TEST]` Before touching Dockerfiles: `docker compose build ms.gateway.api` → expected to fail at `dotnet restore` (no `TargetFramework`). Skipped if Docker isn't running (noted). (AC-08)
- [x] **T-13** `[IMPL]` Add `COPY ["Directory.Build.props", "./"]` after the `Directory.Packages.props` copy in the 5 `ms.*.api/Dockerfile`. (makes T-12 pass)
- [x] **T-14** `[TEST]` `docker compose up -d --build` → the 5 service images build and the containers start (`docker compose ps`). (AC-08)

## Group 6 — Documentation
- [x] **T-15** `[SETUP]` `CLAUDE.md` Structure: root `features/` for solution-wide features (done at stage 1 approval).
- [x] **T-16** `[SETUP]` `CLAUDE.md` "Dependencies & config": `Directory.Build.props` holds the shared settings; csproj only declares what differs; test projects must end in `.test` (they get the test packages + `using Xunit`); APIs at `<svc>/<svc>.api` get the Docker settings; a nested `Directory.Build.props` must import the root one. (AC-13)
- [x] **T-17** `[SETUP]` `README.md` (root): "Build configuration" note on `Directory.Build.props` and `Directory.Packages.props`. No service README changes (no service behavior changes).

## Final verification
- [x] **T-18** `dotnet build EmployeeAttendanceControl.sln` and `dotnet test EmployeeAttendanceControl.sln` → 175 passed, 0 failed, 13 test projects. (AC-01, AC-02)

## AC coverage
| AC | Test tasks |
|----|------------|
| AC-01 | T-01, T-05, T-09, T-18 |
| AC-02 | T-01, T-18 |
| AC-03 | T-05, T-09 |
| AC-04 | T-10 |
| AC-05 | T-02, T-09 |
| AC-06 | T-11 |
| AC-07 | T-06 |
| AC-08 | T-12, T-14 |
| AC-09 | T-05, T-09 |
| AC-10 | T-03, T-05, T-09 |
| AC-11 | T-03, T-09 |
| AC-12 | T-09 |
| AC-13 | T-16 (review) |
| AC-14 | T-02, T-09 |
| AC-15 | T-02, T-09 |

## Implementation notes
- **T-01:** `snapshot.py` also captures `LangVersion`, `UserSecretsId` and the `Compile`/`ProjectReference`/`FrameworkReference` items, and `docker-compose.dcproj`. Baseline: 31 projects, 1043 lines. Compared with `sort -u` (props + csproj duplicates in T-05 are the same item).
- **T-03:** D-03 confirmed: `UsingMicrosoftNETSdkWeb` is already `true` when `Directory.Build.props` is evaluated for the 5 APIs and empty for libraries/tests. No fallback needed.
- **T-05:** build OK with 56 `NU1504` (52 test-package duplicates + the pre-existing `Dapper` one); no property changes.
- **T-07:** first trimming pass lost the indentation of the first group after `<Project>`; files reverted and trimmed again with a line-based script. BOM and tabs/spaces of each file kept.
- **T-09:** snapshot diff vs baseline = 3 lines: `LangVersion latest → 14.0` (expected, same effective version), `ms.users.api` `DockerComposeProjectPath` fixed (expected), and **`ms.employees.infraestucture.test` gains `using Xunit` global** (not expected: it was the only test project without it; its files already have `using Xunit;`, no new warning). Unique warnings: baseline 21 → 20 (the `Dapper` `NU1504` is gone), none new.
- **T-11:** ⚠ with a plain `dotnet build` (implicit restore) a malformed `Directory.Build.props` fails with `NETSDK1013` (empty `TargetFramework`): restore evaluates ignoring invalid imports. The `MSB4024` error naming `Directory.Build.props` appears with `dotnet build --no-restore` and `dotnet msbuild`. The build never succeeds silently. AC-06 reworded accordingly (2026-10-30).
- **T-12:** red confirmed: `docker compose build ms.gateway.api` failed at `dotnet restore` with `NETSDK1013`.
- **T-14:** 5 API images built; users, employees, attendances and gateway running. `ms.assistant.api` exits at startup with `OptionsValidationException: ApiKey required` because there's no `.env` locally — pre-existing, expected behavior (ValidateOnStart), not related to the build.
- **T-18:** build 0 errors; `dotnet test` 175 passed / 0 failed in 13 test projects (= baseline).
