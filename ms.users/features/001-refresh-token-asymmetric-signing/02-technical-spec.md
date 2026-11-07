# [001] Session renewal, exclusive credential issuing and password protection — Technical Specification

> Status: Approved (2026-11-07)
> Based on: `01-business-spec.md` (Approved 2026-11-04)
> Reference implementation followed: `ms.assistant/ms.assistant.api/Extensions/AssistantUnavailableExceptionHandler.cs` (503 mapping), `ms.employees/ms.employees.application/HttpComunications/IAttendanceApiCommunication.cs` (Refit with forwarded token), `ms.users/ms.users.infraestructure/Repositories/UserRepository.cs` (Cassandra mapper)

## 1. Overview
- **Signing.** `ms.users` signs access tokens with RS256 using a private key only it can read, and publishes the public key through OpenID discovery and JWKS. The gateway and the other services validate through `Authority`, so they hold no key and no secret. `Authentication:JWT:Key` disappears from the solution.
- **Sessions.** Login creates a session row and an opaque refresh token. Only the SHA-256 of a refresh token is stored. Each token has its own row, which is how a spent token is told apart from an unknown one. A conditional update (LWT) makes single use atomic.
- **Passwords.** PBKDF2 hashes in the existing column. `ms.users` converts readable values before it starts accepting requests. The seed already contains a hash.
- **Employee creation.** `ms.employees` calls `POST /User` directly with the admin's token, then saves the employee. `EmployeeCreateEvent` and `UserConsumer` are removed, so `ms.users` no longer uses RabbitMQ.

## 2. Components by layer

### ms.users — API
- (~) `Controllers/AuthenticationController.cs` — sends `LoginCommand`, and adds `Refresh` and `Logout` actions. It catches only `InvalidCredentialsException` (401). The catch-all goes away, so an outage is no longer reported as a wrong password.
- (+) `Controllers/WellKnownController.cs` — `[Route(".well-known")]`, anonymous: `openid-configuration` and `jwks.json`, through `GetOpenIdConfigurationQuery` and `GetJwksQuery`. The `jwks_uri` is built from the request's host, so it works both in containers and locally.
- (~) `Controllers/UserController.cs` — `UserAlreadyExistsException` becomes 409.
- (+) `Extensions/DataUnavailableExceptionHandler.cs` — `DataUnavailableException` → 503, modeled on the assistant's handler.
- (~) `Program.cs` — registers the new types, makes `CassandraCluster` and `IUsersContext` singletons (D-09), adds `UseExceptionHandler`, removes the consumer, and runs `ProtectStoredPasswordsCommand` before `app.Run()`. If it throws, the process exits and the restart policy retries.
- (-) `Consumers/UserConsumer.cs`, `Events/EmployeeCreateEvent.cs`, `Mappers/EventMapperProfile.cs` and their tests. The `RabbitMQ.Client` and `ms.rabbitmq` references go from the `.csproj` and the test `.csproj`, and the `ms.rabbitmq` `COPY` from the `Dockerfile`.

### ms.users — Application
- (+) `Commands/LoginCommand.cs`, `RefreshSessionCommand.cs`, `LogoutCommand.cs`, `ProtectStoredPasswordsCommand.cs`, with their `Handlers/`.
- (-) `Queries/GetUserTokenQuery.cs` and its handler. Login now writes, so it's a command.
- (+) `Queries/GetOpenIdConfigurationQuery.cs`, `GetJwksQuery.cs`, with their `Handlers/`.
- (~) `Commands/Handlers/CreateUserAccountCommandHandler.cs` — hashes the password before saving.
- (+) `Security/IPasswordHasher.cs`, `Pbkdf2PasswordHasher.cs` — `Hash`, `Verify`, `IsProtected`.
- (+) `Security/ISigningKeyProvider.cs`, `RsaSigningKeyProvider.cs` — singleton; loads the PEM, or generates it if the file is missing.
- (+) `Security/IAccessTokenIssuer.cs`, `JwtAccessTokenIssuer.cs` — the code of the old `CreateToken`, with the same `nameid` and `role` claims, plus `iss` and `kid`.
- (+) `Security/IRefreshTokenGenerator.cs`, `RefreshTokenGenerator.cs` — 32 random bytes in base64url, and the SHA-256 of a token.
- (+) `Request/RefreshTokenRequest.cs`, `Responses/TokenResponse.cs`, `Responses/OpenIdConfigurationResponse.cs`, `Responses/JwksResponse.cs`.
- (~) `Request/LoginCredentialsRequest.cs` — no `[Required]`, so missing data gets the same 401 (AC-03). The handler treats it as invalid credentials.
- (~) `Responses/UserResponse.cs` — no `Password`.
- (~) `Extensions/AuthenticationExtensions.cs` — validates with the local public key, with no HTTP call to itself.
- (~) `Extensions/SettingsOptions.cs` — see §6. `Communication` is removed.
- (~) `.csproj` — removes the `ms.rabbitmq` reference.

### ms.users — Domain
- (+) `Entities/UserSession.cs` — with `IsActive(DateTimeOffset now, TimeSpan idle)`.
- (+) `Entities/RefreshToken.cs`.
- (+) `Interfaces/ISessionRepository.cs`.
- (~) `Interfaces/IUserRepository.cs` — `GetUser(username, password)` becomes `GetUserByUserName(username)`, which returns null when not found. Adds `ProtectPassword(username, readableValue, hash)`.
- (~) `Entities/User.cs` — `Password` becomes `PasswordHash`.
- (+) `Exceptions/InvalidCredentialsException.cs`, `UserAlreadyExistsException.cs`, `DataUnavailableException.cs`.

### ms.users — Infrastructure
- (+) `Repositories/SessionRepository.cs`.
- (+) `Mappings/CassandraSessionMapping.cs`.
- (~) `Mappings/CassandraUserMapping.cs` — `PasswordHash` maps to the existing `user_pasword`.
- (~) `Repositories/UserRepository.cs` — no password comparison. `DriverException` is translated to `DataUnavailableException`, and a failed insert to `UserAlreadyExistsException`.
- (~) `Data/UsersContext.cs` — singleton, thread-safe. It connects on the first `GetMapper()`, not in the constructor, so the failure is translated too. A failed connection isn't remembered, so the next call tries again. It implements `IDisposable` and closes the session and the cluster when the application stops (D-09).
- (~) `CQLRepositories/UserCQL.cs` — the conditional update. `IMapper.UpdateIfAsync` prepends `UPDATE <table>`, so conditional statements start at `SET` or `USING TTL`.

### ms.employees
- (+) `application/HttpComunications/IUsersApiCommunication.cs` — `[Post("/User")] Task CreateAccount([Body] CreateAccountApiRequest account, [Header("Authorization")] string token)`.
- (+) `application/HttpComunications/Requests/CreateAccountApiRequest.cs`.
- (~) `application/Commands/CreateEmployeeCommand.cs` — adds `Token`.
- (~) `application/Commands/Handlers/CreateEmployeeCommandHandler.cs` — account first, then the employee (§8). Removes `IProducer` and `IMapper` here. `UpdateAttendanceStateCommandHandler` still publishes its event.
- (~) `application/Requests/CreateEmployeeRequest.cs` — `[Required]` on `UserName`, `Password` and `Role`.
- (~) `api/Controllers/EmployeesController.cs` — passes the `Authorization` header, like `AttendanceState`.
- (+) `domain/Exceptions/UsersServiceUnavailableException.cs`, `api/Extensions/UsersServiceUnavailableExceptionHandler.cs` — 503.
- (~) `application/Extensions/CommunicationConfigExtension.cs` — Refit client with a 10 s timeout.
- (~) `api/Program.cs` — `UseExceptionHandler`.
- (-) `application/Events/EmployeeCreateEvent.cs`, its mapping in `EmployeesMapperProfile`, and their tests.

### Validation in the other services
The same change in `ms.gateway/ms.gateway.api/Extensions/Extensions.cs`, `ms.attendances/ms.attendances.api/Extensions/Extensions.cs`, `ms.assistant/ms.assistant.api/Extensions/Extensions.cs` and `ms.employees/ms.employees.api/Extensions/OpenApiExtension.cs`:

```csharp
options.Authority = authentication.JWT.Authority;
options.RequireHttpsMetadata = false;
options.RefreshInterval = TimeSpan.FromSeconds(30);
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,                // issuer comes from the discovery document
    ValidateAudience = false,
    ValidateIssuerSigningKey = true,
    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
    ValidateLifetime = true,
    RequireExpirationTime = true,
    ClockSkew = TimeSpan.Zero
};
options.Events = new JwtBearerEvents
{
    // Without this, a failed key download becomes a 500 (AC-30)
    OnAuthenticationFailed = context => { context.Fail(context.Exception); return Task.CompletedTask; }
};
```
`ExtensionsTests` in the gateway changes: the checks of the symmetric key and of `ValidateIssuer=false` no longer apply.

Each verifier also turns off the "last known good" configuration in a `PostConfigure<JwtBearerOptions>` (`BaseConfigurationManager.UseLastKnownGoodConfiguration = false`). Without it, a token that fails with the current keys is validated again against the previous ones, kept for an hour, so tokens of a replaced key keep working (found by T-62, AC-25).

### Gateway routes
`ocelot.json` and `ocelot.Local.json`, same order: `POST /Authentication/Refresh` and `POST /Authentication/Logout`, both anonymous. `OcelotRoutesTests` and its catalog change, since it now has three anonymous routes, as does `e2e/.../Catalog/GatewayRoutes.cs`. The `/.well-known` routes aren't published: only services use them, and they call `ms.users` directly.

### E2E suite
- (~) `Support/GatewayClient.cs` — `LoginAsync` returns a `TokenPairDto`; adds `RefreshAsync` and `LogoutAsync`.
- (+) `Support/Dtos/TokenPairDto.cs`.
- (~) `Support/E2EEnvironmentFixture.cs` — no wait for the account (AC-40).
- (~) `Security/AuthenticationTests.cs`, `Flows/EmployeeOnboardingTests.cs`.
- (+) `Security/SessionTests.cs`, `Security/SigningTests.cs`, `Security/PasswordTests.cs`.
- (~) `run-e2e.ps1` — the readiness probe reads `accessToken` from the JSON.
- (~) `docker-compose.e2e.yml` — publishes the services on `19009`, `19010`, `19020` and `19030`, to prove direct access (AC-22, AC-26). Spec 002 published only the gateway; it still coexists with the development stack, since the ports are different.

## 3. API contract
| Method | Route | Auth | Request | Response | Errors |
|--------|-------|------|---------|----------|--------|
| POST | `/Authentication` | Anonymous | `{ userName, password }` | 200 `TokenResponse` (**breaking**: was a plain string) | 401 same body in every case; 503 |
| POST | `/Authentication/Refresh` | Anonymous | `{ refreshToken }` | 200 `TokenResponse` | 401 in every rejected case; 503 |
| POST | `/Authentication/Logout` | Anonymous | `{ refreshToken }` | 204 | 400 no token; 503 |
| GET | `/User` | admin | — | 200 `[{ userName, role }]` (**breaking**: no `password`) | 401, 403 |
| POST | `/User` | admin | `{ userName, password, role }` | 200 | 400; **409 exists (was 500)**; 503 |
| POST | `/Employees` | admin | unchanged | 200 unchanged | 400 missing data (**new**); 503 `ms.users` unavailable (**new**) |
| GET | `/.well-known/openid-configuration` | Anonymous, `ms.users` only | — | 200 `{ issuer, jwks_uri }` | — |
| GET | `/.well-known/jwks.json` | Anonymous, `ms.users` only | — | 200 `{ keys: [{ kty, use, alg, kid, n, e }] }` | — |

```json
{ "accessToken": "eyJ...", "refreshToken": "q83v...", "expiresIn": 900 }
```
Logout and refresh are anonymous because the refresh token is itself the proof, and because an expired access token mustn't prevent them. Tokens travel in the body, since no browser client exists to justify cookies.

## 4. Data model & persistence
Added to `cassandra/init.cql`, which runs on every start of the container, so existing volumes get the tables too:

```sql
CREATE TABLE IF NOT EXISTS kusers.user_session (
    session_id uuid PRIMARY KEY,
    session_username text,
    session_created_at timestamp,
    session_last_renewed_at timestamp,
    session_ended_at timestamp,
    session_end_reason text          -- logout | reuse | account-missing
);

CREATE TABLE IF NOT EXISTS kusers.refresh_token (
    token_hash text PRIMARY KEY,     -- SHA-256, hexadecimal
    token_session_id uuid,
    token_spent_at timestamp
);
```
- **Expiry:** a session is active when `ended_at` is null, `now < created_at + 30 days` and `now < last_renewed_at + 7 days`. Rows are inserted with a TTL of 30 days, so they clean themselves up.
- **Seed:** the `INSERT` of `admin` stores a PBKDF2 hash of `1234`. `IF NOT EXISTS` keeps an existing admin as is.
- **Legacy table:** `DROP TABLE IF EXISTS kusers."User";`, because that unused table in the development volume holds readable passwords (D-08).
- **Stored password format:** `pbkdf2-sha256$600000$<salt base64>$<hash base64>`, with a 16-byte salt and a 32-byte hash. `IsProtected` parses the whole format. Sign-in never compares readable values, so an unconverted value simply fails.

**Renewal sequence.** The spend comes last, so any earlier failure leaves the token usable (AC-15):
1. Read the token by hash. Not found → 401.
2. Already spent → end the session with `reuse`, log a warning, 401.
3. Read the session. Ended or expired → 401.
4. Read the user. Not found → end the session with `account-missing`, 401.
5. Insert the new token, and update `last_renewed_at`.
6. `UPDATE refresh_token SET token_spent_at = ? WHERE token_hash = ? IF token_spent_at = null`.
7. Not applied (concurrent use, AC-13) → same as step 2. The new token dies with the session, since step 3 checks the session.
8. Return the new access and refresh tokens.

## 5. Inter-service impact
| Service | Change | Mechanism | Contract |
|---------|--------|-----------|----------|
| `ms.users` | Publishes the public key | HTTP | New `/.well-known/*` |
| Gateway, `ms.employees`, `ms.attendances`, `ms.assistant` | Fetch the public key | HTTP, on first use, cached | `Authentication:JWT:Authority` replaces `Key` |
| `ms.employees` → `ms.users` | Creates the account | HTTP, Refit, forwarded token | `POST /User`. Add to "Consumers of this API" in the `ms.users` README |
| `ms.employees` → `ms.users` | **Removed:** `EmployeeCreateEvent` | RabbitMQ | Event and queue disappear |
| `ms.employees`, `ms.assistant` → others | Unchanged | Forwarded token | Works with the new token |

## 6. Configuration & dependencies
**No new packages.** `Rfc2898DeriveBytes.Pbkdf2` is in .NET, and discovery comes with the JwtBearer package already in use.

```json
"Authentication": {
  "JWT": { "Issuer": "ms.users", "PrivateKeyPath": ".keys/jwt-signing.pem", "AccessTokenMinutes": 15 },
  "Session": { "IdleDays": 7, "AbsoluteDays": 30 }
}
```
That's `ms.users`; `ExpirationHours` and `Key` are removed. The others have only `"Authentication": { "JWT": { "Authority": "http://localhost:9009" } }`, and `ms.employees` adds `Communication:External:UsersApiUrl`.

- **`docker-compose.override.yml`:** `Authentication:JWT:Authority=http://ms.users.api:80` in the gateway and the three services; `Authentication:JWT:PrivateKeyPath=/keys/jwt-signing.pem` and a named volume `users_keys:/keys` in `ms.users.api`; `UsersApiUrl` and `depends_on: ms.users.api` in `ms.employees.api`; removes the RabbitMQ dependency and variable from `ms.users.api`.
- **`.gitignore`:** `.keys/`.
- **Deployment step, once (Q-07):** `docker exec ms.rabbitmq.bus rabbitmqctl delete_queue EmployeeCreateEvent`, documented in the `ms.users` README.
- **Key rotation (AC-25):** replace or delete the file and restart `ms.users`. It isn't a planned procedure, and there's no overlap.

## 7. Security
- **Private key:** only in `ms.users`' volume, with mode 600 on Linux, and never in the repository. Its generation is logged with its `kid`, never its value. Generating it on first start means no secret needs distributing in development (D-02). A production deployment should provide the file from its secret store, and all instances must share it.
- **Algorithm allowlist:** `ValidAlgorithms = RS256` prevents algorithm-confusion attacks.
- **Old secret:** retired, not just unused. It remains in the Git history, which is why tokens forged with it are tested (AC-26).
- **Refresh tokens:** 256 bits of randomness, so a fast SHA-256 is enough. PBKDF2 would add nothing and would slow every renewal.
- **Passwords:** PBKDF2-HMAC-SHA256 with 600,000 iterations, the OWASP minimum. Comparison uses `CryptographicOperations.FixedTimeEquals`. An unknown user also runs one verification, so response time doesn't reveal which accounts exist (BR-13).
- **Logs:** no password, hash or token. The `UserConsumer` log line disappears with the consumer.
- **Internal transport:** the password crosses `ms.employees` → `ms.users` by HTTP, without TLS, as all internal calls do today.

## 8. Error handling & logging
| Failure | Behavior |
|---------|----------|
| Cassandra `DriverException` in login, refresh, logout or account creation | `DataUnavailableException` → 503, logged with the exception |
| Wrong password, unknown user or missing data | `InvalidCredentialsException` → 401 `"Invalid username or password"` |
| Refresh rejected | 401 with one body for every case. Reuse logs `LogWarning` with session id and user name, no token |
| Conversion fails | `LogCritical` with no value, the process exits, and the restart policy retries. Conversion continues with what's left |
| `POST /User` from `ms.employees` returns 409 | `LogWarning`, and creation continues (AC-42) |
| `POST /User` returns anything else, connection error or timeout | `UsersServiceUnavailableException` → 503, and no employee is saved |
| Saving the employee fails after the account was created | Propagates as today. The retry goes through the 409 path |
| Verifier can't download the keys and has none cached | Token rejected with 401, and retried after `RefreshInterval` |
| Verifier can't refresh but has keys cached | Uses the cached keys (AC-24) |

## 9. Traceability
"Manual" means a scripted check against the real stack that stops a service or reads its data, as in feature 002.

| AC | Components | Test level |
|----|------------|------------|
| AC-01 | `LoginCommandHandler`, `JwtAccessTokenIssuer` | Unit, e2e |
| AC-02 | `LoginCommandHandler`, `SessionRepository` | E2e |
| AC-03 | `LoginCommandHandler`, `AuthenticationController` | Unit, e2e |
| AC-04 | `UserRepository`, `DataUnavailableExceptionHandler` | Unit, manual |
| AC-05, AC-06 | `RefreshSessionCommandHandler`, `SessionRepository` | Unit, e2e |
| AC-07 | `RefreshSessionCommandHandler` | Unit (no operation changes a role) |
| AC-08 | `RefreshSessionCommandHandler` | Unit, e2e |
| AC-09 | `UserSession.IsActive` | Unit, with fixed dates |
| AC-10, AC-11, AC-12 | `RefreshSessionCommandHandler` | Unit, e2e |
| AC-13 | LWT in `SessionRepository` | Unit, e2e with parallel requests |
| AC-14 | `RefreshSessionCommandHandler` | Unit |
| AC-15 | Sequence in §4 | Unit, manual |
| AC-16 | `RefreshTokenGenerator`, `SessionRepository` | Unit, manual with `cqlsh` |
| AC-17, AC-18, AC-19 | `LogoutCommandHandler` | Unit, e2e |
| AC-20 | `RefreshTokenRequest` | E2e |
| AC-21 | `LogoutCommandHandler`, handler | Unit, manual |
| AC-22 | Validation in each service | E2e, gateway and direct |
| AC-23 | `SettingsOptions`, config | Stack-free e2e test that scans `appsettings*.json` and compose files, manual `grep` |
| AC-24 | `Authority` | Manual, `ms.users` stopped |
| AC-25 | `RsaSigningKeyProvider`, `RefreshInterval` | Unit for the provider, manual for the rotation |
| AC-26, AC-27 | Validation parameters | E2e, gateway and direct. Tokens built with `HMACSHA256` from .NET |
| AC-28 | `ValidateLifetime`, `AccessTokenMinutes` | Unit for parameters, manual with a 1-minute token |
| AC-29 | Handler, JwtBearer | E2e |
| AC-30 | `OnAuthenticationFailed`, `RefreshInterval` | Manual |
| AC-31, AC-32 | `Pbkdf2PasswordHasher`, `CreateUserAccountCommandHandler` | Unit, manual with `cqlsh` |
| AC-33 | `UserResponse`, `UsersMapperProfile` | Unit, e2e |
| AC-34 | `AccountRequest` (existing `[Required]`), `UserController` | E2e |
| AC-35 | `Pbkdf2PasswordHasher.Verify` | Unit, manual: take the hash with `cqlsh` and sign in with it |
| AC-36, AC-37 | `ProtectStoredPasswordsCommandHandler` | Unit, manual on a volume with readable passwords |
| AC-38 | `init.cql` | E2e, every run starts empty; manual with `cqlsh` |
| AC-39 | Handler, `Program.cs` | Unit, manual |
| AC-40 | `CreateEmployeeCommandHandler` | Unit, e2e (sign-in with no wait); manual: no `EmployeeCreateEvent` queue |
| AC-41 | No logging of the value | Manual: search the logs for a marker password |
| AC-42 | `CreateEmployeeCommandHandler`, `UserController` | Unit, e2e |
| AC-43 | `CreateEmployeeCommandHandler`, handler | Unit, manual |
| AC-44 | `CreateEmployeeCommandHandler` | Unit |
| AC-45 | `EmployeesController` (existing) | E2e, `AccessTests` |
| AC-46 | `CreateEmployeeRequest` | E2e |

## 10. Risks, trade-offs & decisions
- **D-01 — Discovery plus JWKS, with `Authority`.** It's the standard path and needs no custom code: caching and refresh on an unknown key come with JwtBearer. A custom key resolver was rejected as more code to own.
- **D-02 — The key is generated on first start, in a volume.** Nothing secret goes in the repository or in `.env`, and an e2e run gets a fresh key that `down -v` destroys. Rejected: a key in `appsettings`, which is the mistake being fixed; a key in `.env`, which every developer would have to create.
- **D-03 — One row per refresh token, looked up by hash.** It's what tells "spent" (revoke) from "unknown" (nothing changes), as AC-08 and AC-10 require. Rejected: a single current hash per session, which can't tell them apart, so anyone knowing a session id could revoke it.
- **D-04 — Validation code stays in each service.** It's four small copies, matching the current style. A shared library would be a new project, a broader refactor.
- **D-05 — Only the issuer is validated, not the audience.** Refresh tokens are opaque, and no other JWT exists to confuse with an access token.
- **D-06 — Commands instead of `GetUserTokenQuery`.** Login now writes. The controller's route doesn't change.
- **D-07 — The seed stores a hash (proposed change to the business spec).** Cassandra's health check passes before `init.cql` finishes, so `ms.users` could convert before the seed exists, leaving a readable admin until the next restart. UC-06 A2 would read "is seeded already protected". AC-38 doesn't change.
- **D-08 — Dropping the legacy table `kusers."User"`.** Otherwise readable passwords would remain in the development volume, against BR-18. It's destructive, though nothing uses that table. Cassandra's `auto_snapshot` may keep a copy on disk until `nodetool clearsnapshot`.
- **D-09 — One Cassandra connection for the whole service (required, your decision of 2026-11-04).** Today `CassandraCluster` and `UsersContext` are transient, so each resolution builds a new `Cluster` and session that nothing closes, and refreshes every 15 minutes per client would multiply that. `CassandraCluster` and `IUsersContext` become singletons; `IUserRepository` and `ISessionRepository` can stay transient, since the driver's session and mapper are thread-safe. A plain `Lazy` was rejected because by default it remembers a failed connection forever, which would break recovery after an outage (AC-04, AC-15). No AC covers it, since it isn't visible to the business, so it's checked directly: the number of connections to Cassandra stays stable across many requests, measured before and after with `nodetool clientstats`, and the service recovers on its own after Cassandra restarts.
- **R-01 — Library behavior (verified 2026-11-07).** Checked against the real stack: cached keys keep working with `ms.users` down (AC-24); a verifier without keys rejects with 401 instead of 500 and recovers on its own (AC-30); a new key is picked up without restarts (AC-25). One assumption was wrong: tokens of the replaced key were still accepted, through the "last known good" configuration, which is now turned off (§2).
- **R-02 — A key change gives one or more 401s.** The verifier refreshes after the first unknown `kid`, at most every 30 s. A client that retries through renewal recovers.
- **R-03 — An ambiguous LWT timeout.** If step 6 times out but was applied, the client gets a 503, and its retry counts as reuse, revoking the session. This is rare on one node, and within the accepted risk of Q-05.
- **R-04 — Readable passwords remain outside the tables.** Old logs, backups, Cassandra snapshots, and data files until compaction. It's out of scope (§8 of the business spec).
- **R-05 — Login costs more CPU.** Each PBKDF2 verification takes around 100 ms or more by design, and repeated attempts aren't limited (out of scope). It's a denial-of-service vector worth a follow-up.
- **R-06 — Orphan account.** If the employee never gets saved and nobody retries, the account stays, able to sign in, with no employee.
- **R-07 — Plain HTTP between services.** The private key is safe, but the password and the JWKS travel unencrypted on the internal network, so an attacker on that network could read a password or substitute a key. It's the current security posture, and TLS between services would be a separate feature.
