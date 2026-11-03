# [002] End-to-end regression tests — Implementation Checklist

> Last reviewed: 2026-11-03
> Overall: ⚠️ In progress. Everything that can run on this machine passes; CI and the shipping step are pending (§5). The paid assistant check is an accepted risk (§7).

## 1. Acceptance criteria
"E2E" means it passed in the final `run-e2e.ps1` run (§4). "Manual" means it was run on 2026-11-03, with the result noted.

| AC | Flow | Status | Proving test(s) | Implementation |
|----|------|--------|-----------------|----------------|
| AC-01 | UC-01 main | ✅ | Manual: full run, exit 0, counts reported, environment destroyed | `e2e/run-e2e.ps1` |
| AC-02 | UC-01 main | ✅ | Manual: 0 containers, 0 volumes, 0 networks after the run; 7 `eac-e2e/*` images kept by decision | `e2e/run-e2e.ps1` |
| AC-03 | UC-01 A1 | ✅ | Manual: run alongside the dev stack, exit 0; dev counts 26/23/60 unchanged; same start times, no restarts | `docker-compose.e2e.yml` |
| AC-04 | UC-01 A3 | ✅ | Manual: `-KeepEnvironment` kept the stack, and the printed command destroyed it | `e2e/run-e2e.ps1` |
| AC-05 | UC-01 E1 | ✅ | Manual: `ms.employees.api` unable to start: exit 1, no checks run, probe named, environment destroyed | `e2e/run-e2e.ps1` |
| AC-06 | UC-01 E2 | ✅ | Manual: expectations broken in 3 families: 7 failed, 32 passed, exit 1, reasons listed, environment destroyed | `e2e/run-e2e.ps1` |
| AC-07 | UC-01 E3 | ✅ | Manual: killed run left 9 containers and 3 volumes; the next run removed them and passed | `e2e/run-e2e.ps1` |
| AC-08 | UC-01 E4 | ✅ | Manual: solution run has no e2e test; 14 projects / 182 tests = baseline 13 / 175 + `ms.rabbitmq.test` (7) | `Directory.Build.props`, e2e `.csproj` |
| AC-09 | UC-01 A2, E5, UC-03 A1 | ⚠️ | `AssistantFactAttributeTests`; manual: not requested and no-key cases skip with the reason. **With a key: never run, accepted risk (§7)** | `AssistantFactAttribute`, `AssistantTests` |
| AC-10 | UC-02 main 1–5 | ✅ | E2E `CreateEmployeeShouldMakeAccountUsable` | `EmployeeOnboardingTests` |
| AC-11 | UC-02 main 6–7 | ✅ | E2E `MarkPresentShouldAddRecordToHistory`; failed when broken on purpose | `AttendanceFlowTests` |
| AC-12 | UC-02 main 8–9 | ✅ | E2E `MarkAbsentShouldAppendWithoutChangingPreviousRecord` | `AttendanceFlowTests` |
| AC-13 | UC-02 main 10, A1 | ✅ | E2E `ByDateRangeShouldReturnRecordsOnlyWhenRangeIncludesToday` | `AttendanceFlowTests` |
| AC-14 | UC-02 main 11 | ✅ | E2E `AddAttendanceShouldAppearInOwnHistory` | `AttendanceFlowTests` |
| AC-15 | UC-02 A2 | ✅ | E2E `CreateAccountShouldAllowSignIn` | `EmployeeOnboardingTests` |
| AC-16 | UC-02 E1, E2 | ✅ | `EventuallyTests`; the first real run failed with "Record 1 … did not happen within 00:01:00" | `Eventually` |
| AC-17 | UC-02 E3 | ✅ | Manual: wrong admin password: 9 of 9 selected checks failed with "Seeded admin could not sign in: 401" | `E2EEnvironmentFixture` |
| AC-18 | UC-03 main 1 | ✅ | E2E `ProtectedRouteShouldRejectAnonymousCaller` (9 routes) | `AccessTests`, `GatewayRoutes` |
| AC-19 | UC-03 main 2 | ✅ | E2E `AdminRouteShouldRejectRegularUser` (5 routes); failed when broken on purpose | `AccessTests` |
| AC-20 | UC-03 main 3 | ✅ | E2E `IdentifiedRouteShouldAcceptRegularUser` (3 routes) | `AccessTests` |
| AC-21 | UC-03 main 4 | ✅ | E2E `AuthenticationTests` (3); failed when broken on purpose | `AuthenticationTests` |
| AC-22 | UC-03 main 5 | ✅ | E2E `ByDateRangeShouldRejectInvalidRange` (2 cases) | `AttendanceFlowTests` |
| AC-23 | UC-03 A2 | ✅ | E2E `HistoryShouldBeReadableByAnotherUser` | `AccessTests` |
| AC-24 | UC-03 E1 | ✅ | `CatalogShouldMatchGatewayRoutes` | `GatewayRoutes` |
| AC-25 | UC-04 main | ❌ | Needs the PR. The workflow exists but has never run | `.github/workflows/e2e.yml` |
| AC-26 | UC-04 E1 | ❌ | Needs the PR | `.github/workflows/e2e.yml` |
| AC-27 | UC-05 main | ⚠️ | Skill step written; not yet exercised by shipping a feature | `~/.claude/skills/ship-feature/SKILL.md` |
| AC-28 | UC-05 E1, E2 | ⚠️ | Runner verified: failing checks and no Docker both give exit 1, with the reason. The skill's reaction is not yet exercised | `~/.claude/skills/ship-feature/SKILL.md` |
| AC-29 | UC-06 main 1–2 | ✅ | E2E: every run starts from empty volumes. Red on the first run, green after the fix | `cassandra/init.cql` |
| AC-30 | UC-06 main 3, A1 | ✅ | `SubscribeAsyncShouldRetryUntilBrokerIsAvailable`, `UseRabbitConsumerShouldSubscribeWhenApplicationStarts`; manual: broker down, then up: subscribed without a restart and received the waiting message | `ConsumerSubscriber`, `ConsumerMiddleware`, healthcheck |
| AC-31 | UC-06 E1 | ✅ | `SubscribeAsyncShouldLogEachFailedAttempt`; manual: "Could not subscribe … on attempt N" in the log | `ConsumerSubscriber` |
| AC-32 | UC-06 E2 | ✅ | `SubscribeAsyncShouldStopRetryingWhenApplicationStops`, `SubscribeAsyncShouldNotSubscribeWhenAlreadyStopping` | `ConsumerSubscriber` |

## 2. Tasks
| Task | Status | Evidence / note |
|------|--------|-----------------|
| T-01 | ✅ | 13 projects, 175 tests |
| T-02…T-06 | ✅ | Red: `Fact` not found. Green after the split; evaluated properties and packages identical; placeholder removed |
| T-07…T-14 | ✅ | Red: types missing. Green: 10 stack-free tests |
| T-15, T-16 | ✅ | `compose config`: only port 19000, no container names, own volumes, network and images |
| T-17 | ✅ | Exercised on Windows PowerShell 5.1. **Not run on `pwsh` 7** (§7) |
| T-18…T-34 | ✅ | Red: support types missing. Green in the final run |
| T-35 | ⚠️ | Not-requested and no-key cases done. Real key case not run: accepted risk, your decision of 2026-11-03 |
| T-36…T-42 | ✅ | See AC-01…AC-07, AC-17. T-38 compared counts instead of inserting a marker, to avoid writing to the dev data |
| T-43 | ⚠️ | Written, never run |
| T-44 | ❌ | Needs push and PR |
| T-45 | ✅ | Step 1b added to the skill |
| T-46 | ⚠️ | Only the runner's side verified |
| T-47…T-49 | ✅ | Docs updated |
| T-50 | ✅ | §4 |
| T-51…T-58 | ✅ | Red: `ConsumerSubscriber` missing. Green: 7 tests |
| T-59, T-60 | ✅ | Broker down at start, and broker restarted while subscribed: both recovered without restarting the service |

## 3. Convention compliance
- [x] Block namespaces, namespace = folder path, one type per file
- [x] Test names `MethodShouldExpectedBehavior`, `// Arrange // Act // Assert`, classes `<ClassUnderTest>Tests` (the flow classes are named after the flow, since there's no single class under test)
- [x] Test project name ends in `.test`; no repeated shared packages
- [x] Central package management; **no new packages**
- [x] Solution: `ms.rabbitmq.test` nested in `ms.communications` → `ms.communications.test`
- [x] Two levels of README updated; `e2e/README.md` added; `CLAUDE.md` updated
- [x] Gateway catalog unchanged; `ocelot.json` untouched
- [ ] ⚠ `E2ESettings` reads environment variables directly, not `IOptions<T>`. Deliberate: it's a test project with no host (spec §6)

## 4. Build & tests
```
dotnet build EmployeeAttendanceControl.sln
Compilación correcta. (0 errors)

dotnet test EmployeeAttendanceControl.sln
14 projects, 182 tests, 0 failed. No e2e test among them

powershell -File e2e/run-e2e.ps1
E2E result: total 40, passed 39, failed 0, skipped 1
SKIPPED ChatShouldReturnAnAnswer: assistant check not requested
```

## 5. Missing / pending
- ❌ **T-44 / AC-25, AC-26** — the workflow's first run. Needs your approval to commit, push and open the PR.
- ⚠️ **T-46 / AC-27, AC-28** — the shipping step, exercised when this feature ships.

## 6. Deviations from spec
All recorded in the specs or in the notes of `03-tasks.md`.
- **Scope:** two startup defects and the consumer's subscription fixed here (business spec §11, technical spec §11), by your decision.
- **Memory limits in `docker-compose.e2e.yml`** (spec §2 updated): without them, two stacks exhausted the memory and the system killed containers of both, including the dev Cassandra.
- **Longer Cassandra healthcheck in the e2e stack** (30 retries), for slower machines and CI.
- **`-Assistant` also reads the key from the root `.env`** (spec §2 updated), because that's where the project documents it.
- **`CLAUDE.md` lists `powershell -File`, not `pwsh`**, and **images are kept**: your decisions of 2026-11-03.
- **Stack-free test classes stay out of the `E2E` collection**, and `TestData` adds a sequence number to user names so one tag can be reused.

## 7. Risks & follow-ups
- **Accepted risk: the real assistant check has never passed (T-35 / AC-09).** It calls a paid service, and you decided on 2026-11-03 not to run it. The check itself is unproven, so a broken assistant flow wouldn't be detected by a normal run.
- **`pwsh` 7 and Linux are untested.** CI will be the script's first run there, so the first PR run may need adjustments. Installing `pwsh` locally covers only part of it.
- **CI memory and time are unproven.** The limits should fit a 7 GB runner, but that hasn't been measured.
- **Dev volume has a leftover table `kusers."User"`.** It's why the dev stack never showed defect 1. It's no longer used and could be dropped in a separate change.
- **`ms.sql.employees.db` showed `restarts=1` in the dev stack seconds after starting**, before the suite ran. Not investigated; the suite didn't affect it.
- **Messages can be lost:** consumers use `autoAck: true`, so a message whose processing fails after delivery is gone. Only `JsonException` is caught. Separate feature.
- **OBS-01, OBS-02, Q-01** (business spec §10) remain open for separate features.
- **Next:** the refresh token and asymmetric signing feature will change `POST /Authentication`, so `GatewayClient`, `AuthenticationTests` and the runner's readiness probe must change with it.
