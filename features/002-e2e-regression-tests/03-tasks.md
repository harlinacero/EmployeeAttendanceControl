# [002] End-to-end regression tests — Tasks (TDD)

> Status: Approved (2026-11-03)
> Based on: `02-technical-spec.md` (Approved 2026-11-03, amended with §11)

Legend: `[TEST]` write a failing test (red) · `[IMPL]` make it pass (green) · `[REFACTOR]` · `[SETUP]` config/infra/docs · `[TEST:manual]` scripted or by-hand verification whose evidence is recorded in `04-checklist.md`

**What "red" means here.** The system's behavior doesn't change (BR-05), so a check of an existing operation can't fail because the behavior is missing. Red is therefore one of two things:
- the support code doesn't exist yet: a compile failure, as for any new type;
- the check is shown to be able to fail: T-37 breaks expectations on purpose (AC-06).

Stack checks run against a kept environment while developing: `e2e/run-e2e.ps1 -KeepEnvironment` once, then `dotnet test e2e/EmployeeAttendanceControl.e2e.test -p:RunE2E=true --filter FullyQualifiedName~<Class>`.

## Group 0 — Baseline
- [x] **T-01** `[SETUP]` Run `dotnet test EmployeeAttendanceControl.sln` before any change and record the projects and tests count. Spec §9 expects 13 / 175; the measured value is the one AC-08 compares against.

## Group 1 — AC-08: the e2e project builds with the solution but stays out of the regular run
- [x] **T-02** `[TEST:manual]` Save the evaluated `IsTestProject`, `IsPackable` and `PackageReference` of `ms.users/ms.users.test/ms.users.api.test` and `ms.users/ms.users.application` (`dotnet msbuild -getProperty:… -getItem:PackageReference`) to the scratchpad, as the "before" snapshot (AC-08)
- [x] **T-03** `[TEST]` `e2e/EmployeeAttendanceControl.e2e.test/EmployeeAttendanceControl.e2e.test.csproj` with `<IsTestProject Condition="'$(RunE2E)' != 'true'">false</IsTestProject>`, plus `Support/PlaceholderTests.cs` with one `[Fact]` → the project doesn't compile, because `Xunit` is missing (AC-08)
- [x] **T-04** `[IMPL]` `Directory.Build.props` — add `UsesTestDefaults`; `IsTestProject` defaults to it; `IsPackable`, the shared packages and `using Xunit` depend on it. The project compiles, and the snapshot of T-02 is unchanged (makes T-03 pass)
- [x] **T-05** `[SETUP]` `EmployeeAttendanceControl.sln` — add the project under a new root solution folder `e2e`
- [x] **T-06** `[TEST:manual]` `dotnet test EmployeeAttendanceControl.sln` gives the same counts as T-01, and `Placeholder` isn't among them; `dotnet test e2e/EmployeeAttendanceControl.e2e.test -p:RunE2E=true` runs it (AC-08). Then delete `PlaceholderTests.cs`

## Group 2 — AC-16: bounded waiting
- [x] **T-07** `[TEST]` `e2e/.../Support/EventuallyTests.cs` (AC-16):
  - `UntilAsyncShouldReturnValueWhenProbeIsDone` — asserts the probe's value is returned;
  - `UntilAsyncShouldKeepPollingUntilDone` — done on the third call; asserts 3 probe calls;
  - `UntilAsyncShouldThrowNamingTheEffectWhenTimeoutPasses` — never done; asserts `TimeoutException` whose message contains `what`, and that it returns close to the timeout.
- [x] **T-08** `[IMPL]` `e2e/.../Support/Eventually.cs` — `UntilAsync<T>` (makes T-07 pass)

## Group 3 — BR-03: unique test data
- [x] **T-09** `[TEST]` `e2e/.../Support/TestDataTests.cs`:
  - `NewUserNameShouldContainRunIdAndTag` — asserts the form `e2e-<runid>-<tag>`;
  - `NewUserNameShouldBeLowerCase` — asserts no upper-case characters, because sign-in lowercases the user name before looking it up (`UserRepository.GetUser`).
- [x] **T-10** `[IMPL]` `e2e/.../Support/TestData.cs` — run id `yyyyMMddHHmmss-<6 hex>`, `NewUserName(tag)` (makes T-09 pass)

## Group 4 — AC-24: every operation is in the catalog
- [x] **T-11** `[TEST]` `e2e/.../Catalog/GatewayRoutesTests.cs` → `CatalogShouldMatchGatewayRoutes` — asserts the method + path set in the linked `ocelot.json` equals `GatewayRoutes`; on failure, the message lists what's missing on each side (AC-24)
- [x] **T-12** `[IMPL]` `e2e/.../Catalog/GatewayRoutes.cs` — the 10 routes, each with its access group and minimal valid request. In the `.csproj`, link `ms.gateway/ms.gateway.api/ocelot.json` as content copied to output (makes T-11 pass)

## Group 5 — AC-09: assistant check is opt-in
- [x] **T-13** `[TEST]` `e2e/.../Support/AssistantFactAttributeTests.cs` (AC-09):
  - `SkipShouldNameMissingRequestWhenAssistantNotRequested`;
  - `SkipShouldNameMissingKeyWhenKeyNotProvided`;
  - `SkipShouldBeEmptyWhenRequestedWithKey`.
- [x] **T-14** `[IMPL]` `e2e/.../Support/AssistantFactAttribute.cs` (makes T-13 pass)

## Group 6 — Run environment and runner
- [x] **T-15** `[TEST:manual]` `docker compose -p eac-e2e -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.e2e.yml config` fails, because the file doesn't exist. Expected once it does: no `container_name`, only port 19000 published, `eac-e2e_*` volumes and network, `eac-e2e/*` images (AC-03)
- [x] **T-16** `[IMPL]` `docker-compose.e2e.yml` — per spec §2 (makes T-15 pass)
- [x] **T-17** `[IMPL]` `e2e/run-e2e.ps1` — parameters and steps of spec §2 and §8; `.gitignore` covers `e2e/TestResults/`. It works on Windows PowerShell 5.1 and `pwsh` 7

## Group 7 — AC-21, AC-17: sign-in and the seeded admin
- [x] **T-18** `[TEST]` `e2e/.../Security/AuthenticationTests.cs` (AC-21):
  - `LoginShouldReturnTokenForSeededAdmin` — asserts 200 and a three-part JWT as text;
  - `LoginShouldRejectWrongPassword` — asserts 401;
  - `LoginShouldRejectUnknownUser` — asserts 401.
- [x] **T-19** `[IMPL]` `e2e/.../Support/E2ESettings.cs`, `Support/GatewayClient.cs` (`LoginAsync`, `SendAsync`), `Support/E2EEnvironmentFixture.cs` and `Support/E2ECollection.cs`. The fixture signs in the admin and keeps the failure reason `"Seeded admin could not sign in: <status>"` (AC-17) (makes T-18 pass)

## Group 8 — AC-10, AC-15: onboarding
- [x] **T-20** `[TEST]` `e2e/.../Flows/EmployeeOnboardingTests.cs` → `CreateEmployeeShouldMakeAccountUsable` — asserts the employee is in `GET /Employees`; within the time limit, they can sign in with their password and appear in `GET /User` with the role `user` (AC-10)
- [x] **T-21** `[IMPL]` `Support/GatewayClient.cs` (`CreateEmployeeAsync`, `GetEmployeesAsync`, `GetUsersAsync`), `Support/Dtos/` (`EmployeeDto`, `CreateEmployeeDto`, `UserDto`). The fixture creates the shared regular employee (makes T-20 pass)
- [x] **T-22** `[TEST]` `Flows/EmployeeOnboardingTests.cs` → `CreateAccountShouldAllowSignIn` — asserts an account created directly with the role `user` can sign in (AC-15)
- [x] **T-23** `[IMPL]` `Support/GatewayClient.cs` (`CreateAccountAsync`), `Support/Dtos/AccountDto.cs` (makes T-22 pass)

## Group 9 — AC-11…AC-14, AC-22: attendance flow
- [x] **T-24** `[TEST]` `e2e/.../Flows/AttendanceFlowTests.cs` → `MarkPresentShouldAddRecordToHistory` — within the time limit there's one record: `attendance` true, date today ± 1 day, `fullName` `"<FirstName> <LastName>"`, notes `"<notes> [0] Asistencias"` (AC-11)
- [x] **T-25** `[IMPL]` `Support/GatewayClient.cs` (`MarkAttendanceAsync`, `GetAttendancesAsync`), `Support/Dtos/AttendanceDto.cs` (makes T-24 pass)
- [x] **T-26** `[TEST]` `Flows/AttendanceFlowTests.cs` → `MarkAbsentShouldAppendWithoutChangingPreviousRecord` — asserts two records, and that the first equals the one read before the change (AC-12). It passes with the T-25 helpers, so T-37 proves it can fail
- [x] **T-27** `[TEST]` `Flows/AttendanceFlowTests.cs` (AC-13, AC-22):
  - `ByDateRangeShouldReturnRecordsOnlyWhenRangeIncludesToday` — yesterday…tomorrow returns both records; a range that ended last month returns none;
  - `ByDateRangeShouldRejectInvalidRange` (theory: from > to; 32 days) — asserts 400.
- [x] **T-28** `[IMPL]` `Support/GatewayClient.cs` (`GetAttendancesByDateRangeAsync`) (makes T-27 pass)
- [x] **T-29** `[TEST]` `Flows/AttendanceFlowTests.cs` → `AddAttendanceShouldAppearInOwnHistory` — asserts the added record is in the caller's own history (AC-14)
- [x] **T-30** `[IMPL]` `Support/GatewayClient.cs` (`AddAttendanceAsync`), `Support/Dtos/CreateAttendanceDto.cs` (makes T-29 pass)

## Group 10 — AC-18, AC-19, AC-20, AC-23: access to every operation
- [x] **T-31** `[TEST]` `e2e/.../Security/AccessTests.cs`, theories over `GatewayRoutes`; each failure names the operation, the caller and the actual status (UC-03 E1):
  - `ProtectedRouteShouldRejectAnonymousCaller` — every non-anonymous route, assistant included, returns 401 (AC-18);
  - `AdminRouteShouldRejectRegularUser` — every `Admin` route returns 403 (AC-19);
  - `IdentifiedRouteShouldAcceptRegularUser` — every `Identified` route except the assistant returns 2xx (AC-20);
  - `HistoryShouldBeReadableByAnotherUser` — one test employee reads another's history and gets 200 (AC-23, OBS-01).
- [x] **T-32** `[IMPL]` Expose the routes as `TheoryData` from `Catalog/GatewayRoutes.cs`; add whatever the checks are missing (makes T-31 compile and pass)

## Group 11 — AC-09: real assistant check
- [x] **T-33** `[TEST]` `e2e/.../Flows/AssistantTests.cs` → `[AssistantFact] ChatShouldReturnAnAnswer` — asserts 200 and a non-empty `answer` (AC-09)
- [x] **T-34** `[IMPL]` `Support/GatewayClient.cs` (`ChatAsync`), `Support/Dtos/ChatDto.cs`, `ChatAnswerDto.cs`; `-Assistant` handling in `run-e2e.ps1` (makes T-33 compile)
- [ ] **T-35** `[TEST:manual]` `run-e2e.ps1` without `-Assistant`: the check is skipped with "not requested". With `-Assistant` and no key: skipped with "key not provided", and the run doesn't fail. With `-Assistant` and a key: it passes. **This last case calls the paid API, so it runs only if the developer provides the key** (AC-09)

## Group 12 — AC-01…AC-07, AC-17: the runner end to end
- [x] **T-36** `[TEST:manual]` Full `run-e2e.ps1`: exit 0, counts reported; afterwards `docker ps -a`, `docker volume ls` and `docker images` show no containers, volumes or network of `eac-e2e`; the `eac-e2e/*` images remain on purpose (AC-01, AC-02)
- [x] **T-37** `[TEST:manual]` Temporarily break one expectation per check family (flow, access, authentication): the other checks still run, exit 1, the failed check is listed with its reason, and the environment is destroyed (AC-06). Revert
- [x] **T-38** `[TEST:manual]` Dev stack up with a marker record → `run-e2e.ps1` → dev containers still up and the marker still there (AC-03)
- [x] **T-39** `[TEST:manual]` `-KeepEnvironment`: the stack stays up and the exact `down` command is printed (AC-04)
- [x] **T-40** `[TEST:manual]` `-ReadyTimeoutSeconds 5`: no checks run, the run fails naming the probes that never succeeded, and the environment is destroyed (AC-05)
- [x] **T-41** `[TEST:manual]` Kill a run halfway, then rerun: the leftovers are removed first, and the dev stack is untouched (AC-07)
- [x] **T-42** `[TEST:manual]` Against a kept stack, run with a wrong `E2E_ADMIN_PASSWORD`: the checks that need the admin fail with "Seeded admin could not sign in", and none passes (AC-17)

## Group 13 — AC-25, AC-26: pull request check
- [ ] **T-43** `[IMPL]` `.github/workflows/e2e.yml` — per spec §2
- [ ] **T-44** `[TEST:manual]` This feature's PR runs the workflow and shows it as passed (AC-25). Push a temporary failing expectation: the PR shows it as failed, with the trx artifact available; then revert (AC-26). **Done only once the PR exists, and only after you agree to the push**

## Group 14 — AC-27, AC-28: shipping workflow
- [x] **T-45** `[IMPL]` `~/.claude/skills/ship-feature/SKILL.md` — step 1b per spec §2 (outside the repository, Q-04)
- [ ] **T-46** `[TEST:manual]` Run the step on this branch: the result reaches the PR's "Testing" section (AC-27). With a failing check, and with Docker stopped: it stops before committing and asks, and a skipped run is never reported as passed (AC-28)

## Group 15 — Docs
- [x] **T-47** `[SETUP]` `e2e/README.md` — coverage, how to run, parameters, how to add a check for a new operation
- [x] **T-48** `[SETUP]` `README.md` (root) — "End-to-end regression tests" section
- [x] **T-49** `[SETUP]` `CLAUDE.md` — the Structure, Testing, Commands, Dependencies & config and Inter-service communication changes of spec §2

## Group 16 — AC-29…AC-32: startup fixes (spec §11)
- [x] **T-51** `[TEST]` `ms.communications/ms.communications.test/ms.rabbitmq.test/Consumers/ConsumerSubscriberTests.cs` (AC-30, AC-31, AC-32):
  - `SubscribeAsyncShouldSubscribeOnceWhenBrokerIsAvailable`;
  - `SubscribeAsyncShouldRetryUntilBrokerIsAvailable` — fails twice; asserts 3 calls;
  - `SubscribeAsyncShouldLogEachFailedAttempt` — asserts 2 warnings, with the exception;
  - `SubscribeAsyncShouldStopRetryingWhenApplicationStops` — always fails; asserts it ends without an exception;
  - `SubscribeAsyncShouldNotSubscribeWhenAlreadyStopping`.
- [x] **T-52** `[IMPL]` `ms.communications/ms.rabbitmq/Consumers/ConsumerSubscriber.cs` (makes T-51 pass)
- [x] **T-53** `[TEST]` `.../ms.rabbitmq.test/Middlewares/ConsumerMiddlewareTests.cs` (AC-30): `UseRabbitConsumerShouldSubscribeWhenApplicationStarts`, `UseRabbitConsumerShouldUnsubscribeWhenApplicationStops`
- [x] **T-54** `[IMPL]` `ms.communications/ms.rabbitmq/Middlewares/ConsumerMiddleware.cs` — starts `ConsumerSubscriber` (makes T-53 pass)
- [x] **T-55** `[SETUP]` `EmployeeAttendanceControl.sln` — add `ms.rabbitmq.test` under `ms.communications` → `ms.communications.test`
- [x] **T-56** `[IMPL]` `cassandra/init.cql` — the seed uses `kusers.user`. T-36 is its test: red was recorded on the first run (AC-29)
- [x] **T-57** `[IMPL]` `docker-compose.override.yml` — the broker's healthcheck uses `check_port_connectivity` (AC-30)
- [x] **T-58** `[SETUP]` `ms.communications/README.md`, `ms.users/README.md`, `ms.attendances/README.md`, root `README.md` — retry behavior and fresh-start fix; `CLAUDE.md` — test project location
- [x] **T-59** `[TEST:manual]` With the broker stopped, start `ms.attendances` in a kept stack: failed attempts are logged; start the broker: it subscribes without a restart and receives what was waiting (AC-30, AC-31)
- [x] **T-60** `[TEST:manual]` Restart the broker in a kept stack with `ms.attendances` subscribed: the consumer comes back without a restart, and an attendance change reaches the history (spec §11)

## Final verification
- [x] **T-50** `dotnet build EmployeeAttendanceControl.sln`, `dotnet test EmployeeAttendanceControl.sln` (T-01's counts plus only `ms.rabbitmq.test`), and `e2e/run-e2e.ps1` (exit 0)

## AC coverage
| AC | Test tasks |
|----|------------|
| AC-01 | T-36 |
| AC-02 | T-36 |
| AC-03 | T-15, T-38 |
| AC-04 | T-39 |
| AC-05 | T-40 |
| AC-06 | T-37 |
| AC-07 | T-41 |
| AC-08 | T-02, T-03, T-06 |
| AC-09 | T-13, T-33, T-35 |
| AC-10 | T-20 |
| AC-11 | T-24 |
| AC-12 | T-26 |
| AC-13 | T-27 |
| AC-14 | T-29 |
| AC-15 | T-22 |
| AC-16 | T-07 |
| AC-17 | T-42 |
| AC-18 | T-31 |
| AC-19 | T-31 |
| AC-20 | T-31 |
| AC-21 | T-18 |
| AC-22 | T-27 |
| AC-23 | T-31 |
| AC-24 | T-11 |
| AC-25 | T-44 |
| AC-26 | T-44 |
| AC-27 | T-46 |
| AC-28 | T-46 |
| AC-29 | T-36 |
| AC-30 | T-51, T-53, T-59 |
| AC-31 | T-51, T-59 |
| AC-32 | T-51 |

## Implementation notes
(Deviations from the plan discovered while implementing.)

- **T-01 baseline:** 13 projects, 175 tests, all passing, as spec §9 expected.
- **T-13 names:** the tests call `AssistantFactAttribute.SkipReason(requested, keyProvided)`, so they don't depend on environment variables.
- **`dotnet sln add` regenerated the GUIDs of two unrelated solution folders**, so the project was added by hand. The solution diff is 17 added lines and nothing else.
- **Not ticked:** T-35 (the real key case is paid), T-43 and T-44 (the workflow has never run), T-46 (the skill's reaction is exercised when shipping). See `04-checklist.md`.
- **T-38** compared counts, start times and restart counts instead of inserting a marker record, to avoid writing to the development data.
- **T-40** first passed by accident: the stack was already ready, so a short limit proved nothing. It was repeated with a service that can't start.
- **Memory (found by T-38).** Cassandra reserves a quarter of the host's memory by default (7.9 GB here), so two stacks exhausted it and containers of both were killed (`exit 137`), including the development Cassandra. `docker-compose.e2e.yml` now limits Cassandra's heap and SQL Server's memory, and gives Cassandra's healthcheck more retries. With the limits, both stacks ran with no restart.
- **Two defects found on the first run, both blocking and both older than this feature.** Decision 2026-11-03: fix them here, plus the consumer's subscription (Group 16):
  - **`cassandra/init.cql`** — the table is created as `kusers.user`, but the seed inserts into `kusers."User"`. Quoted names are case-sensitive, so on any fresh volume the script fails, `set -e` ends the entrypoint, and the container restarts forever. The development volume hides it because it still has an old table with that quoted name.
  - **`ms.attendances` never subscribes** — the broker's healthcheck (`rabbitmq-diagnostics -q ping`) passes before port 5672 opens (seen: API started 15:28:36, listener 15:29:16). `ConsumerMiddleware.OnStarted` calls `SubscribeAsync()` without awaiting it, with no retry, and the exception is lost. The service looks healthy while `AttendanceStateChangedEvent` piles up with 0 consumers.
  - **Evidence:** with a corrected `init.cql` mounted from outside the repository, 36 of 40 passed, 1 skipped (assistant), and the 3 failures were all "attendance change didn't reach the history". After restarting only `ms.attendances` in that stack, it subscribed, processed the queued messages, and the 30 flow and security checks gave 29 passed, 1 skipped.

- **Stack-free test classes stay out of the `E2E` collection.** Spec §2 says all test classes join it, but joining would start the fixture, which needs the stack. `EventuallyTests`, `TestDataTests`, `AssistantFactAttributeTests` and `GatewayRoutesTests` don't join, and run with `--filter "FullyQualifiedName~Support|FullyQualifiedName~Catalog"`.
- **Runner command.** `CLAUDE.md` lists `powershell -File e2e/run-e2e.ps1`, because this dev machine has only Windows PowerShell 5.1; CI uses `pwsh`. The script supports both (decision 2026-11-03).
- **Built images and AC-02.** `down -v` removes containers, volumes and the network. The `eac-e2e/*` images are kept on purpose, to speed up the next run (decision 2026-11-03).
