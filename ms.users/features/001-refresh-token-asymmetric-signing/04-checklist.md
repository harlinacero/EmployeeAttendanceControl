# [001] Session renewal, exclusive credential issuing and password protection — Implementation Checklist

> Last reviewed: 2026-11-07
> Overall: ✅ Complete. All 46 ACs pass, all 71 tasks are done, the build is clean and both suites are green. Nothing is committed yet (§5).

## 1. Acceptance criteria
"E2E" means it passed in the final `run-e2e.ps1` run (§4). "Manual" means a scripted check against a real stack on 2026-11-07 (T-57 to T-66), with the result noted. Where a manual check covered some verifiers only, the others are covered by their unit tests (`...Extensions*Tests`), which assert the same configuration.

| AC | Flow | Status | Proving test(s) | Implementation |
|----|------|--------|-----------------|----------------|
| AC-01 | UC-01 main | ✅ | `LoginCommandHandlerTests.HandleShouldReturnTokensAndCreateSession`, `JwtAccessTokenIssuerTests.IssueShouldExpireAfterConfiguredMinutes`; E2E `LoginShouldReturnTokenPairForSeededAdmin` | `LoginCommandHandler`, `JwtAccessTokenIssuer` |
| AC-02 | UC-01 A1 | ✅ | `HandleShouldCreateIndependentSessionEachTime`; E2E `SecondLoginShouldKeepFirstSessionUsable` | `LoginCommandHandler`, `SessionRepository` |
| AC-03 | UC-01 E1 | ✅ | `HandleShouldRejectWrongPassword`, `...UnknownUserAfterVerifyingDummyHash`, `...MissingData`; E2E `LoginShouldRejectMissingDataLikeWrongPassword` (same status and body) | `LoginCommandHandler`, `LoginCredentialsRequest` |
| AC-04 | UC-01 E2 | ✅ | `GetUserByUserNameShouldThrowDataUnavailable*`, `DataUnavailableExceptionHandlerTests`; manual T-57: Cassandra stopped, sign-in with the right password → 503 | `CassandraCall`, `DataUnavailableExceptionHandler` |
| AC-05 | UC-02 main | ✅ | `HandleShouldReturnNewPairAndSpendPresentedToken`; E2E `RefreshShouldRotateTokens` | `RefreshSessionCommandHandler` |
| AC-06 | UC-02 A1 | ✅ | Same as AC-05: renewal with an unexpired access token | `RefreshSessionCommandHandler` |
| AC-07 | UC-02 A2 | ✅ | `HandleShouldUseCurrentRole`. Unit only: no operation changes a role | `RefreshSessionCommandHandler` |
| AC-08 | UC-02 E1 | ✅ | `HandleShouldRejectUnknownTokenWithoutChanges`, `HandleShouldRejectMissingToken`; E2E `RefreshWithUnknownTokenShouldBeRejected` | `RefreshSessionCommandHandler` |
| AC-09 | UC-02 E2 | ✅ | `UserSessionTests` (4), `HandleShouldRejectExpiredSession`. Unit only, with fixed dates | `UserSession.IsActive` |
| AC-10 | UC-02 E3 | ✅ | `HandleShouldRevokeSessionWhenTokenIsSpent` (warning logged without the token); E2E `ReusedTokenShouldRevokeOnlyItsSession` | `RefreshSessionCommandHandler` |
| AC-11 | UC-02 E3 | ✅ | E2E `ReusedTokenShouldRevokeOnlyItsSession` (other session renews) | `RefreshSessionCommandHandler` |
| AC-12 | UC-02 E4 | ✅ | `HandleShouldRejectEndedSession`; E2E `ReusedTokenShouldRevokeOnlyItsSession`, `LogoutShouldEndOnlyCurrentSession` | `RefreshSessionCommandHandler` |
| AC-13 | UC-02 E5 | ✅ | `HandleShouldRevokeSessionWhenSpendIsNotApplied`, `TrySpendShouldUseConditionalUpdate`; E2E `ConcurrentRefreshShouldSucceedAtMostOnce` | LWT in `SessionRepository` |
| AC-14 | UC-02 E6 | ✅ | `HandleShouldEndSessionWhenUserIsMissing`. Unit only: no operation deletes an account | `RefreshSessionCommandHandler` |
| AC-15 | UC-02 E7 | ✅ | `HandleShouldNotSpendTokenWhenEarlierStepFails`; manual T-57: refresh → 503 with Cassandra down, the same token → 200 after recovery | Sequence in spec §4 |
| AC-16 | UC-02 main | ✅ | `HashShouldBeStableAndNotContainToken`, `HandleShouldStoreOnlyRefreshTokenHash`; manual T-58: the returned token isn't in `refresh_token`, its SHA-256 is | `RefreshTokenGenerator`, `SessionRepository` |
| AC-17 | UC-03 main | ✅ | `HandleShouldEndSessionOfPresentedToken`, `LogoutShouldReturnNoContent`; E2E `LogoutShouldEndOnlyCurrentSession` | `LogoutCommandHandler` |
| AC-18 | UC-03 A1 | ✅ | `HandleShouldDoNothingForUnknownToken`, `...ForEndedSession`; E2E `LogoutWithUnknownTokenShouldSucceed` | `LogoutCommandHandler` |
| AC-19 | UC-03 A2 | ✅ | `HandleShouldEndSessionWhenTokenIsSpent`; E2E `LogoutWithSpentTokenShouldEndSession` | `LogoutCommandHandler` |
| AC-20 | UC-03 E1 | ✅ | E2E `LogoutWithoutTokenShouldBeRejectedAsInvalid` (400) | `LogoutRequest` |
| AC-21 | UC-03 E2 | ✅ | `HandleShouldNotCatchDataUnavailable`; manual T-57: logout → 503, and the session renews afterwards | `LogoutCommandHandler` |
| AC-22 | UC-04 main | ✅ | `IssueShouldIncludeNameIdentifierRoleAndIssuer`; E2E `ValidTokenShouldWorkThroughGatewayAndDirectly` (gateway and the 4 services), plus every existing access check | Verifier extensions |
| AC-23 | UC-04 main | ✅ | Stack-free `NoServiceShouldHoldSigningSecret` (fails with the old gateway configuration); manual T-66 `git grep`: the old secret only in the 2 tests that forge tokens with it | `SettingsOptions`, `appsettings.json` |
| AC-24 | UC-04 A1 | ✅ | Manual T-61: `ms.users` stopped, issued token accepted by the gateway, `ms.employees` and `ms.attendances` | `Authority` |
| AC-25 | UC-04 A2 | ✅ | `RsaSigningKeyProviderTests.ShouldUseNewKeyWhenFileIsReplaced`, `...ShouldRejectTokensOfReplacedKeys` (4 verifiers); manual T-62: key deleted and `ms.users` restarted → new key accepted everywhere without restarts, old one rejected by the gateway and the 3 other services that were checked | `RsaSigningKeyProvider`, `RefreshInterval`, `UseLastKnownGoodConfiguration = false` |
| AC-26 | UC-04 E1 | ✅ | `...ShouldAllowOnlyRs256` (5); E2E `TokenForgedWithOldSecretShouldBeRejectedEverywhere` (5 entry points) | Validation parameters |
| AC-27 | UC-04 E2 | ✅ | `...ShouldValidateIssuer*`; E2E `AlteredTokenShouldBeRejectedEverywhere` (5 entry points) | Validation parameters |
| AC-28 | UC-04 E3 | ✅ | `...ShouldRejectExpiredTokens` / `...ValidateIssuerAndLifetime`; manual T-63: 1-minute token rejected after 65 s by the gateway, `ms.employees`, `ms.attendances` and `ms.users` | `ValidateLifetime`, `ClockSkew = 0` |
| AC-29 | UC-04 E4 | ✅ | E2E `AccessTokenShouldNotRenew`, `RefreshTokenShouldNotWorkAsBearer` | Handler, JwtBearer |
| AC-30 | UC-04 E5 | ✅ | `...ShouldFailInsteadOfThrowingWhenKeysAreUnavailable` (4); manual T-61: gateway restarted with `ms.users` down → 401 (not 500); accepted within 10 s once `ms.users` returned, without restarting | `OnAuthenticationFailed`, `RefreshInterval` |
| AC-31 | UC-05 main | ✅ | `Pbkdf2PasswordHasherTests`, `HandleShouldStoreOnlyHashedPassword`; manual T-58: no readable value in `user` | `Pbkdf2PasswordHasher` |
| AC-32 | UC-05 A1 | ✅ | `HashShouldDifferForSamePassword`; manual: two accounts with the same password store different values | `Pbkdf2PasswordHasher` |
| AC-33 | UC-05 A2 | ✅ | `UserResponseShouldNotExposePassword`; E2E `UserListShouldNotExposePasswords` | `UserResponse` |
| AC-34 | UC-05 E1 | ✅ | E2E `AccountWithoutPasswordShouldBeRejected` | `AccountRequest` (existing `[Required]`) |
| AC-35 | UC-05 E2 | ✅ | `VerifyShouldRejectStoredValueUsedAsPassword`, `VerifyShouldRejectReadableStoredValue`; manual T-58: admin's hash as password → 401 | `Pbkdf2PasswordHasher.Verify` |
| AC-36 | UC-06 main | ✅ | `HandleShouldProtectReadablePasswords`; manual: e2e stack (planted readable password converted, sign-in still works) and the development volume (26 of 26 converted, same accounts, admin signs in with `1234`) | `ProtectStoredPasswordsCommandHandler` |
| AC-37 | UC-06 A1 | ✅ | `HandleShouldSkipProtectedPasswords`, `ProtectPasswordShouldUpdateOnlyIfValueIsUnchanged`; manual T-59: restart → "Protected 0", stored values identical | Same |
| AC-38 | UC-06 A2 | ✅ | E2E every run starts empty and the admin signs in; manual T-58: seeded value is a PBKDF2 hash | `cassandra/init.cql` |
| AC-39 | UC-06 E1 | ✅ | `HandleShouldPropagateFailureWithoutLoggingValues`; manual T-60: Cassandra down → `ms.users` serves nothing, critical log without the planted password, conversion completed after Cassandra returned | `Program.cs` |
| AC-40 | UC-07 main | ✅ | `HandleShouldCreateAccountBeforeEmployee`, `...ForwardCallerToken`, `CreateEmployeeShouldPassAuthorizationHeader`; E2E `CreateEmployeeShouldMakeAccountUsable` (sign-in with no wait); manual T-64: no `EmployeeCreateEvent` queue | `CreateEmployeeCommandHandler` |
| AC-41 | UC-07 main | ✅ | Manual T-64: marker password found 0 times in the logs of all 9 containers | No logging of the value |
| AC-42 | UC-07 A1 | ✅ | `HandleShouldContinueWhenAccountAlreadyExists`, `AccountShouldReturnConflictWhenUserExists`, `CreateUserShouldThrowUserAlreadyExistsWhenNotApplied`; E2E `EmployeeForExistingAccountShouldKeepItsPassword` | Handler, `UserController` |
| AC-43 | UC-07 E1 | ✅ | `HandleShouldNotSaveEmployeeWhenUsersServiceFails` (3 failures), `UsersServiceUnavailableExceptionHandlerTests`; manual T-61: `ms.users` stopped → 503, no employee | Handler, exception handler |
| AC-44 | UC-07 E2 | ✅ | `HandleShouldPropagateEmployeeSaveFailureAfterAccountCreated`; the retry path is AC-42. Unit only | `CreateEmployeeCommandHandler` |
| AC-45 | UC-07 E3 | ✅ | E2E `RegularUserShouldNotCreateEmployeeOrAccount` | Existing `[Authorize(Roles = "admin")]` |
| AC-46 | UC-07 E4 | ✅ | E2E `EmployeeWithoutPasswordShouldBeRejected` | `CreateEmployeeRequest` |

## 2. Tasks
| Task | Status | Evidence / note |
|------|--------|-----------------|
| T-01 | ✅ | 14 projects, 182 tests |
| T-02…T-08 | ✅ | Red: `Security`, `PasswordHash`, `UserAlreadyExistsException` missing. Green |
| T-09…T-12 | ✅ | Red: `Exceptions`, `UsersContext` failures. First green attempt failed: the driver's `Mapper` needs the session's configuration and the global mapping can't be defined twice; fixed (notes in `03-tasks.md`) |
| T-13…T-27 | ✅ | Red: types missing. Green: 63 application, 21 infrastructure, 13 API tests in `ms.users` |
| T-28…T-32 | ✅ | Red → green |
| T-33…T-35 | ✅ | Red: `Authority` missing. Green. Plus `...ShouldRejectTokensOfReplacedKeys` (4), added after T-62, red (`Expected False, Actual True`) then green |
| T-36…T-39 | ✅ | Red → green; seed hash generated with PBKDF2-SHA256, 600,000 iterations |
| T-40…T-44 | ✅ | Red: `IUsersApiCommunication`, `Exceptions` missing. Green |
| T-45, T-46 | ✅ | `OcelotRoutesTests`: 2 failed, then 22 passed |
| T-47…T-54 | ✅ | First full run: 6 failed (503 on refresh, CQL defect); then 72 passed |
| T-55, T-56 | ✅ | `compose config` checked for both stacks |
| T-57…T-66 | ✅ | §1. Two checks were redone because the first attempt proved nothing (notes in `03-tasks.md`) |
| T-67…T-69 | ✅ | READMEs of `ms.users`, `ms.employees`, `ms.communications`, `ms.attendances`, `ms.assistant`, root, `e2e`; `CLAUDE.md`. On request (2026-11-07): an architecture diagram in every service README, the root component diagram redrawn, a token issuing and validation sequence in the root README, and a new `ms.gateway/README.md` (the gateway had none) |
| T-70, T-71 | ✅ | §4 |

## 3. Convention compliance
- [x] Block namespaces, namespace = folder path, test names `MethodShouldExpectedBehavior`, `// Arrange // Act // Assert`
- [x] CQRS: login, refresh, logout and conversion are commands; key publication is a query. Thin controllers with `IMediator`
- [x] `IOptions<T>` for configuration; message templates in logs (the interpolated log in `CreateUserAccountCommandHandler` was converted)
- [x] **No new packages**; central package management untouched
- [x] Gateway routes in `ocelot.json` and `ocelot.Local.json` in the same order; `OcelotRoutesTests` and e2e `GatewayRoutes` updated
- [x] Two levels of README updated; "Consumers of this API" added to `ms.users`
- [x] Sync call `ms.employees` → `ms.users` direct, not through the gateway, with a Refit client in `HttpComunications`
- [ ] ⚠ `UserSessionTests` tests a domain type but lives in `ms.users.application.test`, because `ms.users` has no domain test project. Creating one would be a solution-structure change for 4 tests
- [ ] ⚠ The token validation block is repeated in four services (D-04). A shared library would remove the copies, but it is a new project

## 4. Build & tests
```
dotnet build EmployeeAttendanceControl.sln
Compilación correcta. (0 errors)

dotnet test EmployeeAttendanceControl.sln
14 projects, 282 passed, 0 failed (baseline 182)

powershell -File e2e/run-e2e.ps1
E2E result: total 73, passed 72, failed 0, skipped 1
SKIPPED ChatShouldReturnAnAnswer: assistant check not requested
0 containers and 0 volumes left
```

## 5. Missing / pending
- **Commit, push and PR** — nothing is committed, as with feature 002.
- **Cassandra snapshot in the development volume** — dropping `kusers."User"` left `dropped-1794060850154-User` (6 KiB), which holds the readable password that table had. Remove it with `docker exec ms.cassandra.db nodetool clearsnapshot --all` when the stack is up; not done without your approval.

## 6. Deviations from spec
All recorded in the specs and in the notes of `03-tasks.md`.
- **"Last known good" configuration turned off** in the four verifiers (spec §2 updated). Without it, AC-25 failed: tokens of a replaced key were accepted for up to an hour.
- **Conditional CQL written from `SET`/`USING TTL`** (spec §2 updated): the driver prepends `UPDATE <table>`.
- **`DataUnavailableExceptionHandler` logs the cause**, at error level. The spec only asked for the 503.
- **Mappings defined per context**, not in the driver's global configuration.
- **`.gitignore` exception** for `ms.users.infraestructure.test/Data/`, so the new tests are committed.
- **Business spec UC-06 A2** amended with D-07 (seed already protected), approved with the technical spec.

## 7. Risks & follow-ups
- **Repeated sign-in attempts aren't limited (R-05).** Each costs about 100 ms of CPU by design. Follow-up feature.
- **Plain HTTP between services (R-07).** The password crosses `ms.employees` → `ms.users`, and the public key is downloaded, without TLS.
- **Readable copies outside the tables (R-04):** old logs, backups, the snapshot in §5, and Cassandra data files until compaction.
- **Orphan account (R-06)** if an employee fails to save and nobody retries.
- **Ambiguous LWT timeout (R-03)** can revoke a session; accepted with Q-05.
- **`ms.assistant` doesn't start in the development stack** without `ANTHROPIC_API_KEY` (no root `.env`). It was like this before; unrelated to this feature, so its behavior with `ms.users` down and an expired token was checked by unit tests only.
- **The e2e stack now publishes 4 more ports** (`19009`, `19010`, `19020`, `19030`). Another program using them would make a run fail.
- **CI hasn't run this feature yet.** The e2e workflow will run it for the first time on its PR.
