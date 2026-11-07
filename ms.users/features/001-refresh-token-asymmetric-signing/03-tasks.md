# [001] Session renewal, exclusive credential issuing and password protection — Tasks (TDD)

> Status: Approved (2026-11-07)
> Based on: `02-technical-spec.md` (Approved 2026-11-07)

Legend: `[TEST]` write a failing test (red) · `[IMPL]` make it pass (green) · `[REFACTOR]` · `[SETUP]` config/infra/docs · `[TEST:manual]` scripted check against the real stack, with the result recorded in `04-checklist.md`

Test project paths are shortened:
- `users.app.test` = `ms.users/ms.users.test/ms.users.application.test`, and likewise `users.api.test` and `users.infra.test`
- `employees.app.test` and `employees.api.test` under `ms.employees/ms.employees.test`
- `e2e` = `e2e/EmployeeAttendanceControl.e2e.test`

## Group 0 — Baseline
- [x] **T-01** `[SETUP]` Run `dotnet test EmployeeAttendanceControl.sln` and record the totals as the baseline. Tests removed on purpose are listed in the notes, so the final count can be compared.

## Group 1 — AC-31, AC-32, AC-35: password hashing
- [x] **T-02** `[TEST]` `users.app.test/Security/Pbkdf2PasswordHasherTests.cs`:
  - `HashShouldNotContainPassword` — the result has neither the password nor its Base64 (AC-31)
  - `HashShouldDifferForSamePassword` — two hashes of the same password are different (AC-32)
  - `VerifyShouldAcceptCorrectPassword`, `VerifyShouldRejectWrongPassword` (AC-31)
  - `VerifyShouldRejectStoredValueUsedAsPassword` — verifying the hash against itself fails (AC-35)
  - `VerifyShouldRejectReadableStoredValue` — a stored value that isn't in the format fails, even if it equals the password (AC-35)
  - `IsProtectedShouldRecognizeOnlyCompleteFormat` — true for a real hash, false for `1234`, for `pbkdf2-sha256$x` and for empty
- [x] **T-03** `[IMPL]` `ms.users.application/Security/IPasswordHasher.cs`, `Pbkdf2PasswordHasher.cs` — format `pbkdf2-sha256$600000$salt$hash`, `FixedTimeEquals`

## Group 2 — AC-31, AC-33, AC-34, AC-42: accounts
- [x] **T-04** `[TEST]` `users.app.test/Commands/CreateUserAccountCommandHandlerTests.cs` → `HandleShouldStoreOnlyHashedPassword` — the saved `User.PasswordHash` is the hasher's result, never the password (AC-31)
- [x] **T-05** `[TEST]` `users.infra.test/Repositories/UserRepositoryTests.cs` → `CreateUserShouldThrowUserAlreadyExistsWhenNotApplied` — replaces `CreateUserShouldThrowExceptionWh…` (AC-42)
- [x] **T-06** `[TEST]` `users.api.test/Controllers/UserControllerTests.cs` → `AccountShouldReturnConflictWhenUserExists` — 409 (AC-42)
- [x] **T-07** `[TEST]` `users.app.test/Mappers/UsersMapperProfileTests.cs` → `UserResponseShouldNotExposePassword` — `UserResponse` has no property whose name contains `Password` (AC-33)
- [x] **T-08** `[IMPL]` `User.Password` → `PasswordHash` (domain, mapping, repository); `Exceptions/UserAlreadyExistsException.cs`; `CreateUserAccountCommandHandler` hashes; `UserRepository.CreateUser` throws the new exception; `UserController` returns 409; `UserResponse` without `Password`. Existing tests are updated for the new names.

## Group 3 — AC-04, AC-15, AC-21, D-09: data unavailable and one connection
- [x] **T-09** `[TEST]` `users.infra.test/Repositories/UserRepositoryTests.cs` → `GetUserByUserNameShouldThrowDataUnavailableWhenDriverFails` — a `NoHostAvailableException` becomes `DataUnavailableException` (AC-04)
- [x] **T-10** `[TEST]` `users.infra.test/Data/UsersContextTests.cs`:
  - `GetMapperShouldConnectOnlyOnce` — two calls, one `Connect` (D-09)
  - `GetMapperShouldRetryAfterFailedConnection` — the first `Connect` throws, the second call connects (D-09)
  - `DisposeShouldCloseSessionAndCluster` (D-09)
- [x] **T-11** `[TEST]` `users.api.test/Extensions/DataUnavailableExceptionHandlerTests.cs`:
  - `TryHandleShouldReturn503ForDataUnavailable`
  - `TryHandleShouldIgnoreOtherExceptions`

  (AC-04, AC-15, AC-21)
- [x] **T-12** `[IMPL]` `Exceptions/DataUnavailableException.cs`; `CassandraCluster` exposes `ICluster`; `UsersContext` connects on first use and remembers no failure, plus `IDisposable`; `UserRepository` translates `DriverException`; `DataUnavailableExceptionHandler`; `Program.cs` adds singletons and `UseExceptionHandler`

## Group 4 — AC-01, AC-02, AC-03, AC-16, AC-25: sign-in and signing
- [x] **T-13** `[TEST]` `users.app.test/Security/RsaSigningKeyProviderTests.cs` (temp folder):
  - `ShouldGenerateKeyWhenFileIsMissing` — file created, and the log has the `kid` but not the key
  - `ShouldReuseExistingKey` — two instances give the same `kid`
  - `ShouldUseNewKeyWhenFileIsReplaced` — another file, another `kid` (AC-25)
- [x] **T-14** `[TEST]` `users.app.test/Security/JwtAccessTokenIssuerTests.cs`:
  - `IssueShouldSignWithRs256AndKeyId`
  - `IssueShouldExpireAfterConfiguredMinutes` — 15 (AC-01)
  - `IssueShouldIncludeNameIdentifierRoleAndIssuer` — same claims as today (AC-22)
- [x] **T-15** `[TEST]` `users.app.test/Security/RefreshTokenGeneratorTests.cs`:
  - `GenerateShouldReturnUrlSafe256BitValue`
  - `GenerateShouldNotRepeat`
  - `HashShouldBeStableAndNotContainToken` (AC-16)
- [x] **T-16** `[TEST]` `users.app.test/Commands/LoginCommandHandlerTests.cs`:
  - `HandleShouldReturnTokensAndCreateSession` (AC-01)
  - `HandleShouldStoreOnlyRefreshTokenHash` (AC-16)
  - `HandleShouldCreateIndependentSessionEachTime` — two logins, two session ids (AC-02)
  - `HandleShouldRejectWrongPassword`, `HandleShouldRejectUnknownUserAfterVerifyingDummyHash`, `HandleShouldRejectMissingData` — all throw `InvalidCredentialsException` (AC-03, BR-13)
- [x] **T-17** `[TEST]` `users.infra.test/Repositories/SessionRepositoryTests.cs` → `CreateShouldInsertSessionAndTokenWithTimeToLive` (AC-01)
- [x] **T-18** `[TEST]` `users.api.test/Controllers/AuthenticationControllerTests.cs` — replaces the current two:
  - `DoLoginShouldReturnOkWithTokens` (AC-01)
  - `DoLoginShouldReturnUnauthorizedForInvalidCredentials` (AC-03)
  - `DoLoginShouldNotCatchDataUnavailable` (AC-04)
- [x] **T-19** `[IMPL]` Domain: `UserSession`, `RefreshToken`, `ISessionRepository`, `InvalidCredentialsException`, `IUserRepository.GetUserByUserName`. Application: `RsaSigningKeyProvider`, `JwtAccessTokenIssuer`, `RefreshTokenGenerator`, `LoginCommand` and its handler, `TokenResponse`. Infrastructure: `SessionRepository`, `CassandraSessionMapping`. API: the controller. Removes `GetUserTokenQuery`, its handler and their tests. `LoginCredentialsRequest` without `[Required]`

## Group 5 — AC-05 to AC-15: renewal
- [x] **T-20** `[TEST]` `users.app.test/…` → `UserSessionTests.cs` (it's a domain type, tested where the domain is used):
  - `IsActiveShouldBeTrueWithinLimits`
  - `IsActiveShouldBeFalseAfterIdleLimit`
  - `IsActiveShouldBeFalseAfterAbsoluteLimitEvenIfRenewedRecently`
  - `IsActiveShouldBeFalseWhenEnded`

  (AC-09)
- [x] **T-21** `[TEST]` `users.app.test/Commands/RefreshSessionCommandHandlerTests.cs`, with fixed dates through a `Mock<TimeProvider>` (no testing package added):
  - `HandleShouldReturnNewPairAndSpendPresentedToken` (AC-05, AC-06)
  - `HandleShouldUseCurrentRole` (AC-07)
  - `HandleShouldRejectUnknownTokenWithoutChanges`, `HandleShouldRejectMissingToken` (AC-08)
  - `HandleShouldRejectExpiredSession` (AC-09)
  - `HandleShouldRevokeSessionWhenTokenIsSpent` — revoked with `reuse`, a warning logged without the token value, other sessions untouched (AC-10, AC-11)
  - `HandleShouldRejectEndedSession` (AC-12)
  - `HandleShouldRevokeSessionWhenSpendIsNotApplied` (AC-13)
  - `HandleShouldEndSessionWhenUserIsMissing` (AC-14)
  - `HandleShouldNotSpendTokenWhenEarlierStepFails` — insert throws `DataUnavailableException`, and `TrySpend` isn't called (AC-15)
- [x] **T-22** `[TEST]` `users.infra.test/Repositories/SessionRepositoryTests.cs`:
  - `TrySpendShouldUseConditionalUpdate` — CQL with `IF token_spent_at = null`, returns `Applied` (AC-13)
  - `EndShouldSetEndedAtAndReason`
  - `GetTokenShouldReturnNullWhenNotFound`
  - `ShouldThrowDataUnavailableWhenDriverFails` (AC-15)
- [x] **T-23** `[TEST]` `users.api.test/Controllers/AuthenticationControllerTests.cs`:
  - `RefreshShouldReturnOkWithTokens`
  - `RefreshShouldReturnUnauthorizedForInvalidCredentials`

  (AC-05, AC-08)
- [x] **T-24** `[IMPL]` `RefreshSessionCommand` and its handler (sequence in spec §4), `SessionRepository` methods, `RefreshTokenRequest`, controller action, `TimeProvider.System` in DI

## Group 6 — AC-17 to AC-21: sign out
- [x] **T-25** `[TEST]` `users.app.test/Commands/LogoutCommandHandlerTests.cs`:
  - `HandleShouldEndSessionOfPresentedToken` (AC-17)
  - `HandleShouldDoNothingForUnknownToken` (AC-18)
  - `HandleShouldEndSessionWhenTokenIsSpent` (AC-19)
  - `HandleShouldNotCatchDataUnavailable` (AC-21)
- [x] **T-26** `[TEST]` `users.api.test/Controllers/AuthenticationControllerTests.cs` → `LogoutShouldReturnNoContent` (AC-17)
- [x] **T-27** `[IMPL]` `LogoutCommand` and its handler, controller action, `[Required]` on `RefreshTokenRequest.RefreshToken` for logout only (separate request class if needed so refresh keeps returning 401, AC-08 vs AC-20)

## Group 7 — AC-22, AC-25, AC-26: publishing the public key
- [x] **T-28** `[TEST]` `users.app.test/Queries/GetJwksQueryHandlerTests.cs`:
  - `HandleShouldPublishOnlyPublicParameters` — `kty`, `use`, `alg`, `kid`, `n`, `e`; no `d`, `p`, `q`, `dp`, `dq` or `qi` (BR-10)
  - `HandleShouldUseProviderKeyId` (AC-25)
- [x] **T-29** `[TEST]` `users.app.test/Queries/GetOpenIdConfigurationQueryHandlerTests.cs` → `HandleShouldReturnIssuerAndJwksUri`
- [x] **T-30** `[TEST]` `users.api.test/Controllers/WellKnownControllerTests.cs`:
  - `OpenIdConfigurationShouldReturnOk`
  - `JwksShouldReturnOk`
- [x] **T-31** `[TEST]` `users.app.test/Extensions/AuthenticationExtensionsTests.cs`:
  - `UseAuthenticationBearerShouldValidateWithLocalPublicKey`
  - `UseAuthenticationBearerShouldAllowOnlyRs256` (AC-26)
- [x] **T-32** `[IMPL]` Queries, handlers, responses, `WellKnownController`, `AuthenticationExtensions`, `SettingsOptions` and `appsettings.json` of `ms.users` (§6)

## Group 8 — AC-22 to AC-30: validation in the other services
- [x] **T-33** `[TEST]` `ms.gateway/ms.gateway.test/ms.gateway.api.test/Extensions/ExtensionsTests.cs` — replaces the key and issuer checks:
  - `AddGatewayAuthenticationShouldUseConfiguredAuthority`
  - `AddGatewayAuthenticationShouldAllowOnlyRs256` (AC-26)
  - `AddGatewayAuthenticationShouldValidateIssuer` (AC-27)
  - `AddGatewayAuthenticationShouldRejectExpiredTokens` (kept, AC-28)
  - `AddGatewayAuthenticationShouldRefreshKeysAfterThirtySeconds` (AC-25, AC-30)
  - `AddGatewayAuthenticationShouldFailInsteadOfThrowingWhenKeysAreUnavailable` — invoking `OnAuthenticationFailed` leaves a failed result (AC-30)
  - `AddGatewayAuthenticationShouldKeepShortClaimNames` (kept)
- [x] **T-34** `[TEST]` The same five behavior tests (authority, RS256 only, issuer, refresh interval, fail instead of throwing) in:
  - `ms.attendances/ms-attendances.test/ms-attendances.api.test/Extensions/ExtensionsTests.cs`
  - `ms.assistant/ms.assistant.test/ms.assistant.api.test/Extensions/ExtensionsTests.cs`
  - `employees.api.test/Extensions/OpenApiExtensionTests.cs`

  (AC-22 to AC-30)
- [x] **T-35** `[IMPL]` The four extensions (spec §2), their `SettingsOptions` (`Authority` only) and `appsettings.json` files

## Group 9 — AC-36 to AC-39: converting stored passwords
- [x] **T-36** `[TEST]` `users.app.test/Commands/ProtectStoredPasswordsCommandHandlerTests.cs`:
  - `HandleShouldProtectReadablePasswords` (AC-36)
  - `HandleShouldSkipProtectedPasswords` (AC-37)
  - `HandleShouldPropagateFailureWithoutLoggingValues` — the exception propagates, and no log entry has the password or hash (AC-39)
  - `HandleShouldLogOnlyCounts`
- [x] **T-37** `[TEST]` `users.infra.test/Repositories/UserRepositoryTests.cs` → `ProtectPasswordShouldUpdateOnlyIfValueIsUnchanged` — conditional CQL (AC-37)
- [x] **T-38** `[IMPL]` Command, handler, `ProtectPassword` in the repository, `UserCQL`, call before `app.Run()` with `LogCritical` and exit on failure
- [x] **T-39** `[SETUP]` `cassandra/init.cql` — the two tables, the seed with a PBKDF2 hash of `1234` (generated with `Pbkdf2PasswordHasher`), and `DROP TABLE IF EXISTS kusers."User";` (spec §4, D-07, D-08)

## Group 10 — AC-40 to AC-46: creating employees
- [x] **T-40** `[TEST]` `employees.app.test/Commands/CreateEmployeeCommandHandlerTests.cs` — replaces `HandleShouldCreateEmployeeAndSendE…`:
  - `HandleShouldCreateAccountBeforeEmployee` — call order verified (AC-40)
  - `HandleShouldForwardCallerToken` (AC-40)
  - `HandleShouldContinueWhenAccountAlreadyExists` — 409 → employee saved, warning logged without the password (AC-42)
  - `HandleShouldNotSaveEmployeeWhenUsersServiceFails` — 500, `HttpRequestException` and timeout each throw `UsersServiceUnavailableException`, and the repository isn't called (AC-43)
  - `HandleShouldPropagateEmployeeSaveFailureAfterAccountCreated` (AC-44)
- [x] **T-41** `[TEST]` `employees.api.test/Controllers/EmployeesControllerTests.cs` → `CreateEmployeeShouldPassAuthorizationHeader` (AC-40)
- [x] **T-42** `[TEST]` `employees.api.test/Extensions/UsersServiceUnavailableExceptionHandlerTests.cs` → `TryHandleShouldReturn503` (AC-43)
- [x] **T-43** `[IMPL]` `IUsersApiCommunication`, `CreateAccountApiRequest`, command with `Token`, handler, exception and its handler, `[Required]` in `CreateEmployeeRequest`, `UsersApiUrl` in settings and Refit client with a 10 s timeout, `UseExceptionHandler`
- [x] **T-44** `[IMPL]` Remove `EmployeeCreateEvent` from `ms.employees` (event, mapping, mapper test) and from `ms.users` (consumer, event, `EventMapperProfile`, their tests, `UseRabbitConsumer`, `Communication` settings, RabbitMQ references in both `.csproj`, `COPY` in the `Dockerfile`). Build stays green (AC-40, BR-17)

## Group 11 — Gateway routes
- [x] **T-45** `[TEST]` `ms.gateway/.../Configuration/OcelotRoutesTests.cs` — add the two routes to the catalog as anonymous; `LoginRouteShouldNotRequireAuthentication` covers all three anonymous routes. Red until the routes exist
- [x] **T-46** `[IMPL]` `ocelot.json` and `ocelot.Local.json`, same order

## Group 12 — End-to-end checks
- [x] **T-47** `[TEST]` `e2e/Catalog/GatewayRoutes.cs` — add `Refresh` and `Logout` as anonymous; `CatalogShouldMatchGatewayRoutes` red until T-46, then green
- [x] **T-48** `[TEST]` `e2e/Catalog/SigningSecretTests.cs` → `NoServiceShouldHoldSigningSecret` — stack-free: no `appsettings*.json` or compose file has `JWT:Key`, `JWT__Key` or the old secret (AC-23)
- [x] **T-49** `[IMPL]` `e2e/Support/Dtos/TokenPairDto.cs`; `GatewayClient`: `LoginAsync` returns `TokenPairDto`, plus `RefreshAsync`, `LogoutAsync` and `SendToServiceAsync`; `E2ESettings` service URLs; fixture without waiting; `run-e2e.ps1` reads `accessToken`; `docker-compose.e2e.yml` publishes `19009`, `19010`, `19020` and `19030`
- [x] **T-50** `[TEST]` `e2e/Security/AuthenticationTests.cs`:
  - `LoginShouldReturnTokenPairForSeededAdmin` — three-part access token, non-empty refresh token, `expiresIn` 900 (AC-01, AC-38)
  - `LoginShouldRejectMissingDataLikeWrongPassword` — same status and body as a wrong password (AC-03)
- [x] **T-51** `[TEST]` `e2e/Security/SessionTests.cs`:
  - `RefreshShouldRotateTokens` (AC-05, AC-06)
  - `SecondLoginShouldKeepFirstSessionUsable` (AC-02)
  - `RefreshWithUnknownTokenShouldBeRejected` (AC-08)
  - `ReusedTokenShouldRevokeOnlyItsSession` — the newest token of that session fails, the other session renews (AC-10, AC-11, AC-12)
  - `ConcurrentRefreshShouldSucceedAtMostOnce` — `Task.WhenAll`, at most one 200, then the session is revoked (AC-13)
  - `LogoutShouldEndOnlyCurrentSession` — the access token still works (AC-17)
  - `LogoutWithUnknownTokenShouldSucceed` (AC-18)
  - `LogoutWithSpentTokenShouldEndSession` (AC-19)
  - `LogoutWithoutTokenShouldBeRejectedAsInvalid` (AC-20)
  - `AccessTokenShouldNotRenew` (AC-29)
- [x] **T-52** `[TEST]` `e2e/Security/SigningTests.cs`:
  - `ValidTokenShouldWorkThroughGatewayAndDirectly` — one protected call to each service, directly and through the gateway (AC-22)
  - `TokenForgedWithOldSecretShouldBeRejectedEverywhere` — HS256 built with `HMACSHA256` (AC-26)
  - `AlteredTokenShouldBeRejectedEverywhere` — role changed to `admin` without re-signing (AC-27)
  - `RefreshTokenShouldNotWorkAsBearer` (AC-29)
- [x] **T-53** `[TEST]` `e2e/Security/PasswordTests.cs`:
  - `UserListShouldNotExposePasswords` — raw JSON has no `password` (AC-33)
  - `AccountWithoutPasswordShouldBeRejected` (AC-34)
  - `EmployeeWithoutPasswordShouldBeRejected` (AC-46)
  - `EmployeeForExistingAccountShouldKeepItsPassword` (AC-42)
  - `RegularUserShouldNotCreateEmployeeOrAccount` — rejected, and the user name can't sign in afterwards (AC-45)
- [x] **T-54** `[TEST]` `e2e/Flows/EmployeeOnboardingTests.cs` → `CreateEmployeeShouldMakeAccountUsable` signs in right after creation, with no `WaitUntilAsync` (AC-40)

## Group 13 — Configuration and compose
- [x] **T-55** `[SETUP]` `docker-compose.override.yml`:
  - gateway and three services: `Authority`
  - `ms.users.api`: key path, `users_keys` volume, no RabbitMQ variable or dependency
  - `ms.employees.api`: `UsersApiUrl` and `depends_on: ms.users.api`
- [x] **T-56** `[SETUP]` `.gitignore`: `.keys/`

## Group 14 — Manual checks against the real stack
Each one is recorded in `04-checklist.md` with its evidence.
- [x] **T-57** `[TEST:manual]` Data unavailable — Cassandra stopped:
  - sign-in gives 503, and sign-in with the correct password doesn't give 401 (AC-04)
  - refresh gives 503, and the same token works after Cassandra returns (AC-15)
  - logout gives 503, and the session is still renewable afterwards (AC-21)
  - the service recovers on its own after Cassandra restarts (D-09)
- [x] **T-58** `[TEST:manual]` `cqlsh`:
  - `refresh_token` holds hashes and no token that was returned (AC-16)
  - `user` has no readable password (AC-31, AC-36)
  - two accounts with the same password have different values (AC-32)
  - the admin is protected on an empty volume (AC-38)
  - the hash taken from `cqlsh` doesn't work as a password (AC-35)
- [x] **T-59** `[TEST:manual]` Conversion on the development volume:
  - before: list readable passwords, without printing them, by count. After start: count 0, and the admin signs in with `1234` (AC-36)
  - restart: values unchanged (AC-37)
  - `kusers."User"` no longer exists (D-08)
- [x] **T-60** `[TEST:manual]` Conversion failure: Cassandra unavailable at start → `ms.users` doesn't accept requests, the log has no value, and it finishes once Cassandra returns (AC-39)
- [x] **T-61** `[TEST:manual]` `ms.users` stopped:
  - tokens already issued still work through the gateway and directly (AC-24)
  - a freshly started verifier with `ms.users` down rejects, then accepts with no restart once `ms.users` returns (AC-30)
  - creating an employee gives 503 and no employee exists (AC-43)
- [x] **T-62** `[TEST:manual]` Key change: delete the key file and restart `ms.users` → new tokens are accepted by every verifier within 30 s with no restart, and old ones are rejected (AC-25)
- [x] **T-63** `[TEST:manual]` `AccessTokenMinutes=1` temporarily → after 1 minute the token is rejected by the gateway and directly (AC-28). Restore afterwards
- [x] **T-64** `[TEST:manual]` Create an employee with a marker password, then search the logs of every container for it: zero matches (AC-41). RabbitMQ has no `EmployeeCreateEvent` queue after the deployment step (AC-40, Q-07)
- [x] **T-65** `[TEST:manual]` Connections: `nodetool clientstats` before and after 200 requests to `ms.users` → the count stays stable (D-09)
- [x] **T-66** `[TEST:manual]` Configuration review: `grep` the repository for `JWT:Key`, `JWT__Key` and `SymmetricSecurityKey` → only in tests that forge tokens on purpose (AC-23)

## Group 15 — Documentation
- [x] **T-67** `[SETUP]` `ms.users/README.md`:
  - API: new routes, contract changes, discovery
  - sessions, key generation and rotation
  - password storage and conversion
  - deployment step (Q-07)
  - "Consumers of this API": `ms.employees` → `POST /User`
  - removal of RabbitMQ
- [x] **T-68** `[SETUP]` `ms.employees/README.md` (account creation, 503), `ms.communications/README.md` (event removed), `ms.gateway`, `ms.attendances` and `ms.assistant` READMEs (validation through `Authority`)
- [x] **T-69** `[SETUP]` Root `README.md`: architecture, route table, authentication diagram and cross-service communication. `CLAUDE.md`: events list (`EmployeeCreateEvent` gone), glossary (User / account no longer created from an event; session, refresh token), sync communication (`ms.employees` → `POST /User`), authorization line (RS256 + `Authority`). `e2e/README.md`: new checks and service ports

## Final verification
- [x] **T-70** `dotnet build EmployeeAttendanceControl.sln` and `dotnet test EmployeeAttendanceControl.sln` — 0 failed; totals compared with T-01
- [x] **T-71** `powershell -File e2e/run-e2e.ps1` — 0 failed, and the environment is destroyed

## AC coverage
| AC | Test tasks |
|----|------------|
| AC-01 | T-14, T-16, T-17, T-18, T-50 |
| AC-02 | T-16, T-51 |
| AC-03 | T-16, T-18, T-50 |
| AC-04 | T-09, T-11, T-18, T-57 |
| AC-05 | T-21, T-23, T-51 |
| AC-06 | T-21, T-51 |
| AC-07 | T-21 |
| AC-08 | T-21, T-23, T-51 |
| AC-09 | T-20, T-21 |
| AC-10 | T-21, T-51 |
| AC-11 | T-21, T-51 |
| AC-12 | T-21, T-51 |
| AC-13 | T-21, T-22, T-51 |
| AC-14 | T-21 |
| AC-15 | T-11, T-21, T-22, T-57 |
| AC-16 | T-15, T-16, T-58 |
| AC-17 | T-25, T-26, T-51 |
| AC-18 | T-25, T-51 |
| AC-19 | T-25, T-51 |
| AC-20 | T-51 |
| AC-21 | T-11, T-25, T-57 |
| AC-22 | T-14, T-34, T-52 |
| AC-23 | T-48, T-66 |
| AC-24 | T-61 |
| AC-25 | T-13, T-28, T-33, T-62 |
| AC-26 | T-31, T-33, T-52 |
| AC-27 | T-33, T-52 |
| AC-28 | T-33, T-63 |
| AC-29 | T-51, T-52 |
| AC-30 | T-33, T-34, T-61 |
| AC-31 | T-02, T-04, T-58 |
| AC-32 | T-02, T-58 |
| AC-33 | T-07, T-53 |
| AC-34 | T-53 |
| AC-35 | T-02, T-58 |
| AC-36 | T-36, T-58, T-59 |
| AC-37 | T-36, T-37, T-59 |
| AC-38 | T-50, T-58 |
| AC-39 | T-36, T-60 |
| AC-40 | T-40, T-41, T-54, T-64 |
| AC-41 | T-64 |
| AC-42 | T-05, T-06, T-40, T-53 |
| AC-43 | T-40, T-42, T-61 |
| AC-44 | T-40 |
| AC-45 | T-53 |
| AC-46 | T-53 |

## Implementation notes
(Deviations from the plan discovered while implementing.)

- **Baseline (T-01):** 14 projects, 182 tests. Final: 14 projects, 282 tests. Removed on purpose, with the code they tested: `UserConsumerTests` (5), `EventMapperProfileTests` (2), `GetUserTokenQueryHandlerTests` (2), `UsersMapperProfileShouldMapUserResponseToUser` (`UserResponse` no longer maps back to `User`), `EmployeesMapperProfileShouldMapCreateEmployeeCommandToEmployeeCreateEvent`, and the gateway's symmetric-key and no-issuer checks. The repository's `GetUser*` and the employee handler's tests were rewritten for the new contracts.
- **The e2e suite found a defect the unit tests couldn't (T-51):** `IMapper.UpdateIfAsync` prepends `UPDATE <table>` to the statement, so the two conditional updates written as full `UPDATE ...` were invalid CQL. Refresh returned 503, and converting readable passwords would have failed in every existing installation. Fixed by writing them from `SET`/`USING TTL`; the unit tests assert the new text. The 503 also exposed that `DataUnavailableExceptionHandler` logged nothing, so it now logs the cause at error level.
- **Verifiers kept accepting tokens of a replaced key (T-62).** Microsoft.IdentityModel validates a token that fails with the current keys again against the "last known good" configuration, kept for an hour by default. Each verifier now sets `UseLastKnownGoodConfiguration = false` in a `PostConfigure<JwtBearerOptions>`, with a red-then-green unit test per service (`...ShouldRejectTokensOfReplacedKeys`). Spec §2 and R-01 updated.
- **Cassandra mappings** no longer register themselves in `MappingConfiguration.Global`: `UsersContext` builds its own `MappingConfiguration`. The global one throws when a mapping is defined twice, which the context tests do.
- **`.gitignore`:** the existing `data/` rule also ignores `ms.users.infraestructure.test/Data/`, so `UsersContextTests` would never have been committed. An exception was added for that folder, which also starts tracking the existing `CassandraClusterTests.cs` in it.
- **T-48** was written after T-35, so it passed at once. It was then checked against the old configuration: with the previous gateway `appsettings.json` restored it fails, and passes again once restored.
- **Manual checks** ran against a kept e2e stack (`eac-e2e`), not the development one, except T-59. Two checks first proved nothing and were redone: AC-32 had only one account to compare (redone with two accounts with the same password), and T-62 didn't delete the key because Git Bash rewrote the path (redone with `MSYS_NO_PATHCONV=1`). T-65 first counted 0 connections because of a wrong filter (redone with `Total connected clients`), and compared with the previous image to show the defect existed.
