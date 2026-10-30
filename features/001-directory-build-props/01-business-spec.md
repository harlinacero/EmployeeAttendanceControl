# [001] Shared build configuration for the whole solution — Business Specification

> Status: Approved (2026-10-30)
> Owning service: none — solution-wide (root `features/`)
> Author / requested by: harlinacero

## 1. Summary
All projects in the solution take their common build settings from one shared place instead of each project repeating them. Building, testing and packaging the system works exactly as it does today; the change only affects where the settings are written.

## 2. Business goal / problem
Today each of the 31 projects repeats the same settings: the .NET version, implicit usings, nullable checks, and for test projects the test packages and the global xUnit using. Container settings are repeated in the 5 web APIs. This causes two problems:

- **Drift.** Copies have already diverged. One API points to a wrong container-orchestration path. One project lists the same package twice. One test project sets a language version no other project sets. Nullable checks are on in some layers and off in others, and nothing records that this is intentional.
- **Cost of change.** Upgrading the .NET version, or adding a test package, means editing every project and risks missing one. Every new project (the last two services added 11) copies the boilerplate again.

With one shared place, a change is made once and applies everywhere, and new projects start correctly configured.

## 3. Actors
| Actor | Description | Role / permission |
|-------|-------------|-------------------|
| Developer | Builds, tests and adds projects to the solution, locally or in containers | write access to the repository |
| Build environment | Container image build (`docker compose up --build`) and any command-line build/test run | — |

## 4. User stories
- **US-01** — As a developer, I want every project to inherit the common build settings from one shared place, so that the solution behaves the same as today but the settings are maintained once.
- **US-02** — As a developer, I want a new project to get the right settings for its kind (library, web API, test project) just by being created in the solution with the usual naming, so that I don't copy boilerplate.
- **US-03** — As a developer, I want to change a common setting (e.g. the .NET version) in one place, so that every project picks it up and none is missed.

## 5. Business rules
- **BR-01** — One shared configuration at the solution root applies to every .NET project of the solution, whatever folder it lives in.
- **BR-02** — **No behavior change.** After the change, every project keeps the same target framework, implicit usings, nullable setting, package references, assembly name and root namespace. The solution builds, and the same tests run and pass (same count).
- **BR-03** — Nullable checks keep their current per-project values: on for web APIs, test projects, the shared RabbitMQ library and `ms.users` infrastructure; off for the application, domain and infrastructure layers of the other services. The shared default is "on". Projects that differ declare it themselves. Unifying nullable is out of scope.
- **BR-04** — A setting declared in a project overrides the shared default. Project-specific needs stay in the project: its own packages, project references, user-secrets id, extra framework references, linked/copied content files.
- **BR-05** — **Test projects** are recognized by the solution's naming convention (project name ends in `.test`). They automatically get: not packable, the common test packages (xUnit, xUnit runner, test SDK, Moq, coverage collector), and xUnit as a global using. Packages a single test project needs on top of those (e.g. AutoMapper, MediatR) stay in that project.
- **BR-06** — **Web API projects** automatically get the container settings: Linux target OS, the build context at the repository root, and the path to the container-orchestration project. The wrong path in the users API is corrected by this.
- **BR-07** — Package versions stay where they are today (central package management). The shared configuration never sets a version.
- **BR-08** — Container images of every service still build, since they compile with the shared configuration too.
- **BR-09** — Clean-up while moving the settings: remove the duplicated package reference, the empty folder declarations and the isolated language-version setting. Nothing that changes what gets compiled is removed (e.g. the `ms.users` API test file that is intentionally excluded from compilation stays excluded).
- **BR-10** — The shared configuration does not affect non-.NET projects in the solution (the container-orchestration project).

## 6. Use cases

### UC-01 — Build and test the solution with the shared configuration (US-01, US-03)
- **Actor:** Developer
- **Preconditions:** The shared configuration is in place; the projects only declare what is specific to them.
- **Main flow:**
  1. The developer builds the whole solution.
  2. The system applies the shared settings to every project, then each project's own settings on top.
  3. Every project builds with the same settings it had before (BR-02, BR-03).
  4. The developer runs the test suite.
  5. The same tests are discovered and pass as before.
- **Alternative flows:**
  - **A1** (at step 1) — The developer builds or tests a single project (not the solution) → the shared settings apply too → same result as building it inside the solution.
  - **A2** (at step 2) — A project declares a value that differs from the shared default (e.g. nullable off) → the project's value wins (BR-04).
  - **A3** (at step 1) — The developer changes a shared setting → every project picks up the new value on the next build, with no project file edited (US-03).
- **Exception flows:**
  - **E1** (at step 2) — The shared configuration has an error → every project fails to build (no project silently builds without it). The error naming the shared file is shown when building without the implicit restore (`dotnet build --no-restore` / `dotnet msbuild`); a plain `dotnet build` fails earlier, at restore.
  - **E2** (at step 2) — The build reaches the container-orchestration project → it is not affected by the shared .NET settings and keeps working (BR-10).
- **Postconditions:** Success: same build output and test results as before. Failure: the build stops and points to the cause.

### UC-02 — Build the container images (US-01)
- **Actor:** Build environment
- **Preconditions:** The developer starts the containers with a rebuild.
- **Main flow:**
  1. The build environment restores and builds each service image.
  2. The shared configuration is available inside the image build, so each service compiles with the same settings as locally (BR-08).
  3. All service images build and the services start.
- **Exception flows:**
  - **E1** (at step 2) — The shared configuration is not available inside the image build → the service would compile with different or missing settings. This must not happen: each image build includes the shared configuration before restoring.
- **Postconditions:** Every service image builds as before.

### UC-03 — Add a new project (US-02)
- **Actor:** Developer
- **Preconditions:** The shared configuration is in place.
- **Main flow:**
  1. The developer creates a new project in the solution following the naming conventions and keeps only what is specific to it.
  2. The system gives it the common settings (.NET version, implicit usings, nullable on).
  3. If it is a test project (name ends in `.test`), the system adds the test defaults (BR-05).
  4. If it is a web API, the system adds the container settings (BR-06).
- **Alternative flows:**
  - **A1** (at step 1) — The new project needs a different value (e.g. nullable off, like the other layers of its service) → it declares that value itself (BR-04).
- **Exception flows:**
  - **E1** (at step 3) — A test project is named outside the convention → it does not get the test defaults and its tests don't build or aren't discovered. The developer renames it or declares the settings itself. The naming rule is documented.
- **Postconditions:** The new project builds and, if it is a test project, its tests run, with a minimal project file.

## 7. Acceptance criteria

- **AC-01** (UC-01 main, BR-01, BR-02) — *Given* the shared configuration, *When* the solution is built, *Then* the build succeeds and every project has the same target framework, implicit usings, assembly name and root namespace as before the change.
- **AC-02** (UC-01 main, BR-02) — *Given* the shared configuration, *When* the whole test suite runs, *Then* all tests pass and the total number of tests equals the number before the change.
- **AC-03** (UC-01 A2, BR-03, BR-04) — *Given* a project that declares nullable off, *When* it is built, *Then* nullable is off for it; *and* every other project keeps its current nullable value.
- **AC-04** (UC-01 A1) — *Given* a single test project, *When* only that project is tested, *Then* it builds with the shared settings and its tests pass.
- **AC-05** (UC-01 A3, US-03) — *Given* the shared configuration, *When* a common value (e.g. the .NET version) is read for any project, *Then* it comes from the shared file and no project file declares it.
- **AC-06** (UC-01 E1) — *Given* a broken shared configuration, *When* the solution is built, *Then* the build fails, and building without the implicit restore (`dotnet build --no-restore` / `dotnet msbuild`) shows an error referring to the shared file.
- **AC-07** (UC-01 E2, BR-10) — *Given* the shared configuration, *When* the container-orchestration project is evaluated, *Then* it does not receive the .NET target framework or the test/container defaults.
- **AC-08** (UC-02 main, E1, BR-08) — *Given* the shared configuration, *When* the containers are rebuilt, *Then* every service image builds and its service starts.
- **AC-09** (UC-03 main, BR-05) — *Given* a project whose name ends in `.test` and that does not declare them, *When* it is built, *Then* it is not packable, references the five common test packages and has xUnit as a global using; *and* a project-specific test package (e.g. AutoMapper) is still referenced only by the projects that declare it.
- **AC-10** (UC-03 main, BR-06) — *Given* a web API project, *When* it is evaluated, *Then* it has the Linux container target, the repository root as build context and the correct path to the container-orchestration project — including the users API, which today has a wrong path.
- **AC-11** (UC-03 main) — *Given* a non-test, non-web project (e.g. a domain library), *When* it is evaluated, *Then* it gets neither test packages nor container settings.
- **AC-12** (UC-03 A1, BR-04) — *Given* a project that declares a value different from the shared one, *When* it is built, *Then* its own value is used.
- **AC-13** (UC-03 E1) — *Given* the conventions documentation, *When* a developer reads it, *Then* it states that test projects must end in `.test` to get the test defaults, and what the shared configuration provides.
- **AC-14** (BR-07) — *Given* the shared configuration, *When* it is inspected, *Then* it sets no package version; versions stay in central package management.
- **AC-15** (BR-09) — *Given* the project files after the change, *When* they are inspected, *Then* none repeats a setting the shared configuration provides, there is no duplicated package reference, no empty folder declaration and no isolated language-version setting; *and* the excluded `ms.users` test file is still excluded.

### Flow coverage
| Flow | AC |
|------|----|
| UC-01 main | AC-01, AC-02 |
| UC-01 A1 | AC-04 |
| UC-01 A2 | AC-03 |
| UC-01 A3 | AC-05 |
| UC-01 E1 | AC-06 |
| UC-01 E2 | AC-07 |
| UC-02 main | AC-08 |
| UC-02 E1 | AC-08 |
| UC-03 main | AC-09, AC-10, AC-11 |
| UC-03 A1 | AC-12 |
| UC-03 E1 | AC-13 |
| BR-07, BR-09 | AC-14, AC-15 |

## 8. Out of scope
- Unifying nullable across the solution (would surface warnings to fix in several layers).
- New build-quality rules: warnings as errors, analyzers, code-style enforcement.
- Assembly metadata (authors, company, version numbers).
- Changing package versions or adding packages.
- Changing source code, tests, routes or runtime behavior of any service.

## 9. Glossary (new terms only)
- **Shared build configuration** — the single file at the solution root whose settings every .NET project inherits before its own settings are applied.
- **Test project** — a project whose name ends in `.test` (e.g. `ms.users.api.test`, `ms-attendances.api.test`).
- **Web API project** — a project built as an ASP.NET Core web application (one per service: users, employees, attendances, gateway, assistant).

## 10. Assumptions & open questions
- [x] Q-01 — Where does a solution-wide feature live? (answer: root `features/`, numbered on its own sequence starting at `001`; the conventions in CLAUDE.md will be updated.)
- [x] Q-02 — Unify nullable? (answer: no, keep current values; BR-03.)
- [x] Q-03 — What to centralize besides the base settings? (answer: test defaults, container settings for APIs, and clean-up; no new quality rules.)
- [x] Q-04 — The `ms.users` API test project excludes `Consumers/UserConsumerTests.cs` from compilation. Assumed intentional and kept as is; confirm if it should be re-enabled in a separate change. (answer: confirmed, kept excluded.)
