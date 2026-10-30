# [001] AI attendance assistant — Implementation Checklist

> Last reviewed: 2026-10-29
> Overall: ⚠️ In progress. The code and all automated tests are done. Still pending: the manual run with a real Anthropic API key (T-38) and the answer-quality review (`eval-questions.md`).

## 1. Acceptance criteria
| AC | Flow | Status | Proving test(s) | Implementation |
|----|------|--------|-----------------|----------------|
| AC-01 | UC-01 main | ✅ | `GetMyAttendancesToolTests.ExecuteAsyncShouldQueryCallerAttendancesWithCallerToken`, `ChatCommandHandlerTests.HandleShouldExecuteRequestedToolWithCallerContextAndSendResultBack`, `AssistantControllerTests.ChatShouldSendCommandWithCallerIdentityRoleAndToken` | `Tools/GetMyAttendancesTool.cs`, `Commands/Handlers/ChatCommandHandler.cs`, `Controllers/AssistantController.cs` |
| AC-02 | UC-01 A3, UC-03 A2 | ✅ | `AssistantToolCatalogTests.ForShouldReturnOnlyNonAdminToolsWhenCallerIsNotAdmin`, `ChatCommandHandlerTests.HandleShouldOfferOnlyCatalogToolsForCallerRole`, `…HandleShouldReturnErrorResultWhenModelRequestsToolNotOffered` | `Tools/AssistantToolCatalog.cs`, `ChatCommandHandler.ExecuteToolAsync` |
| AC-03 | UC-01 A3 | ✅ | `GetMyAttendancesToolTests.ExecuteAsyncShouldIgnoreUserNameInInput`, `…ToolShouldBeAvailableToEveryoneAndHaveNoInputs` | `GetMyAttendancesTool` (empty schema, user from `ToolContext`) |
| AC-04 | UC-01 A1, UC-02 A3 | ✅ | `GetMyAttendancesToolTests…ReturnEmptyArrayWhenNoAttendances`, `GetAttendancesByDateRangeToolTests…ReturnEmptyArrayWhenNoAttendances` | `AssistantToolBase.RetrieveAsync` |
| AC-05 | UC-01 A2/A4/A5 | ✅ | `ChatCommandHandlerTests.HandleShouldReturnModelTextWhenNoToolIsRequested` | `ChatCommandHandler` |
| AC-06 | BR-07 | ✅ | `HttpComunicationsTests.InterfacesShouldOnlyDeclareGetOperations` | `HttpComunications/*` (only `[Get]`) |
| AC-07 | UC-01 A6 | ✅ | `ChatCommandHandlerTests.HandleShouldKeepSameToolsAndPassNotesOnlyAsToolResult`, `AssistantPromptTests.BuildShouldStateToolResultsAreDataNotInstructions` | `ChatCommandHandler`, `Prompts/AssistantPrompt.cs` |
| AC-08 | UC-01 E1 | ✅ | `AssistantControllerTests.ControllerShouldRequireAuthentication`; smoke test: no token → `401` | `[Authorize]` on `AssistantController`, `AddAuthenticationSettings` |
| AC-09 | UC-01 E2 | ✅ | `ChatRequestTests` (11 cases); smoke test: empty question → `400` | `Requests/ChatRequest.cs` |
| AC-10 | UC-01 E3, UC-02 E2, UC-03 E2 | ✅ | `*ToolTests.ExecuteAsyncShouldReturnErrorResultWhenServiceIsUnavailable` (3), `ChatCommandHandlerTests.HandleShouldContinueWhenToolReturnsError` | `AssistantToolBase.RetrieveAsync` |
| AC-11 | UC-01 E4, UC-02 E3, UC-03 E3 | ⚠️ | `ChatCommandHandlerTests.HandleShouldPropagateModelUnavailableException`, `AssistantUnavailableExceptionHandlerTests` (2) | `ClaudeAssistantModel` (exception mapping), `Extensions/AssistantUnavailableExceptionHandler.cs` — the mapping of SDK rate-limit/5xx/network errors to `503` has no automated test (D-03) and is not yet checked manually (T-38) |
| AC-12 | UC-02 main | ✅ | `AssistantToolCatalogTests.ForShouldReturnAllToolsWhenCallerIsAdmin`, `GetAttendancesByDateRangeToolTests.ExecuteAsyncShouldQueryRequestedEmployeeAndRangeWithCallerToken` | `AssistantToolCatalog`, `GetAttendancesByDateRangeTool` |
| AC-13 | UC-02 A2/A4 | ✅ | `GetAttendancesByDateRangeToolTests` (range > 31, from > to, invalid date, missing field) | `GetAttendancesByDateRangeTool` |
| AC-14 | UC-02 A1 | ✅ | `ChatCommandHandlerTests.HandleShouldReturnModelTextWhenNoToolIsRequested` (same mechanism) | `ChatCommandHandler` |
| AC-15 | UC-03 main | ✅ | `ListEmployeesToolTests.ExecuteAsyncShouldReturnEmployeesWithCurrentAttendanceState` | `Tools/ListEmployeesTool.cs` |
| AC-16 | UC-03 A1 | ✅ | `ListEmployeesToolTests.ExecuteAsyncShouldReturnEmptyArrayWhenNoEmployees` | `ListEmployeesTool` |
| AC-17 | US-04 | ✅ | `ChatCommandHandlerTests.HandleShouldSendHistoryInOrderFollowedByQuestion`, `ClaudeMessageMapperTests.ToParamsShouldKeepMessageOrderAndRoles` | `ChatCommandHandler`, `AI/ClaudeMessageMapper.cs` |
| AC-18 | BR-14 | ✅ | `ChatCommandHandlerTests.HandleShouldLogUsageWithoutQuestionOrAnswerText` | `ChatCommandHandler` (usage log) |

## 2. Tasks
| Task | Status | Evidence / note |
|------|--------|-----------------|
| T-01 – T-03 | ✅ | 7 projects in the solution; `Anthropic` 13.3.0 in `Directory.Packages.props`; the build passes |
| T-04 – T-05 | ✅ | `ChatRequestTests` 11/11 |
| T-06 – T-07 | ✅ | `AssistantToolCatalogTests` 2/2 |
| T-08 – T-09 | ✅ | `GetMyAttendancesToolTests` 5/5 |
| T-10 – T-11 | ✅ | `GetAttendancesByDateRangeToolTests` 8/8 |
| T-12 – T-13 | ✅ | `ListEmployeesToolTests` 4/4 |
| T-14 – T-15 | ✅ | `HttpComunicationsTests` 1/1 (green on the first run, by design) |
| T-16 – T-17 | ✅ | `AssistantPromptTests` 7/7 |
| T-18 – T-19 | ✅ | `ChatCommandHandlerTests` 12/12 |
| T-20 – T-21 | ✅ | `ClaudeMessageMapperTests` 8/8 |
| T-22 | ✅ | `ClaudeAssistantModel`, `AddAssistantModel`; the smoke test reached the Anthropic API |
| T-23 – T-26 | ✅ | `AssistantControllerTests` 4/4, `AssistantUnavailableExceptionHandlerTests` 2/2 |
| T-27 – T-32 | ✅ | `Program.cs`, options with `ValidateOnStart`, appsettings, `launchSettings` (`:9030`), Dockerfile, compose (`docker compose config` valid), `.gitignore` (`.env`), `.env.example` |
| T-33 – T-34 | ✅ | `OcelotRoutesTests` 19/19 (red with 3 failures before the routes were added) |
| T-35 – T-36 | ✅ | README section + diagram + route table; `eval-questions.md` (20 questions) |
| T-37 | ✅ | See §4 |
| T-38 | ⚠️ | Local smoke test only (dummy key). The real-key compose run is pending |

## 3. Convention compliance
- [x] Layering `api` / `application` / `domain` / `infraestucture`, spelled `infraestucture` (D-02); test projects under `ms.assistant/ms.assistant.test`, with folders that mirror the source.
- [x] File-scoped classes in block namespaces, namespace = folder path.
- [x] CQRS with MediatR: `ChatCommand` is a `record : IRequest<ChatResponse>`, and the handler is in `Commands/Handlers`.
- [x] Thin controller: `[ApiController]`, `[Route("[controller]")]`, only `IMediator`, `Ok(await _mediator.Send(...))`, user from `ClaimTypes.NameIdentifier`.
- [x] `[Authorize]` for any authenticated user; roles from `ClaimTypes.Role`.
- [x] Constructor injection into `private readonly _camelCase` fields.
- [x] Configuration through `IOptions<T>` (`SettingsOptions`, `AssistantOptions`, `AnthropicOptions`). No raw `IConfiguration` reads in classes.
- [x] DI in `Extensions` static classes (`AddAssistant`, `AddAssistantModel`, `AddAuthenticationSettings`, `AddSwaggerOpenApi`).
- [x] Structured logging with message templates; all I/O is async.
- [x] xUnit + Moq; `Mock<T>` fields in the constructor; `MethodShouldExpectedBehavior` names; `// Arrange // Act // Assert` (one test uses a combined `// Act & Assert` for `ThrowsAsync`).
- [x] Central Package Management: versions only in `Directory.Packages.props`; the new package `Anthropic` was approved in the tech spec.
- [x] Gateway: the route is in **both** `ocelot.json` and `ocelot.Local.json`, in the same position, with `Bearer`; `OcelotRoutesTests` was updated.
- [x] Settings in `appsettings.json`, overridden in `docker-compose.override.yml`.
- [ ] ⚠ **AutoMapper isn't used.** CLAUDE.md says "Mapping with AutoMapper `Profile`s per layer". Here the mapping is done by hand in two places:
  - `ClaudeMessageMapper` works with SDK union types (`TryPick*`) that AutoMapper can't map cleanly;
  - the tools serialize the Refit DTOs straight to JSON for the model, so there is no type-to-type mapping.
  
  Suggested fix: keep it as is and record the exception in CLAUDE.md, or add AutoMapper only for the `ChatMessageRequest` → `ChatMessage` conversion. That would mean one more package reference for very little gain.
- [ ] ⚠ **One class per file:** `api/Extensions/SettingsOptions.cs` holds 6 small classes. It copies the existing `SettingsOptions.cs` in ms.attendances and ms.employees, so it's consistent with the codebase but not with the rule.

## 4. Build & tests
```
dotnet build EmployeeAttendanceControl.sln   → 0 errors
dotnet test EmployeeAttendanceControl.sln    → 13 test projects, Passed 175, Failed 0, Skipped 0
  ms.assistant.application.test 50 · ms.assistant.infraestucture.test 8 · ms.assistant.api.test 6 · ms.gateway.api.test 19
  (existing: ms.attendances 20+9+15, ms.employees 9+9+3, ms.users 9+7+11 — unchanged and green)
```
Manual smoke test (`dotnet run`, dummy key, locally signed JWT):
- no token → `401`;
- `{"question":""}` → `400` with `errors.Question`;
- `{"question":"hola"}` → reaches the Anthropic API, which rejects the dummy key (`AnthropicUnauthorizedException`) → logged + `500`, as designed (§8).

## 5. Missing / pending
- ⚠️ **T-38 with a real API key** (`docker compose up -d --build` with `.env`):
  - admin and employee questions through the gateway `:9000`;
  - `ms.attendances.api` stopped → the assistant explains the failure (AC-10 end to end);
  - the `503` path (AC-11; e.g. disconnect the network or use an unreachable base URL);
  - the usage log line has no question or answer text.
- ⚠️ **Answer-quality review**: run the 20 questions in `eval-questions.md` and record the results (business spec Q-03).
- Open business questions: Q-04 (budget / per-user limit), Q-05 (limits OK), Q-06 (**data privacy approval** for sending attendance data to Anthropic).

## 6. Deviations from spec
- **D-10 (approved):** settings are split into `Assistant` (application) and `Anthropic` (infrastructure).
- **`AssistantToolBase<TTool>`** (new shared base class for the tools) and **`TimeProvider`** in `ChatCommandHandler`. Both are internal refinements; the tech spec isn't updated (they don't change any contract).
- **`Refit.Reflection`** reference added to `ms.assistant.application` (already pinned; found in the smoke test).
- **`Anthropic` 13.x API:** messages are built as `UserMessageParam` / `AssistantMessageParam`. It's an internal detail of `ClaudeMessageMapper`.

## 7. Risks & follow-ups
- **Cost:** each question takes 1–5 model calls. Watch the usage log line, and consider per-user quotas (Q-04).
- **SDK adapter coverage:** `ClaudeAssistantModel` has no automated test. Exception mapping depends on the SDK's exception types; recheck after upgrading `Anthropic`.
- **Duplicated rule:** the 31-day limit exists in both ms.attendances and the assistant (D-06); the README notes it.
- **Effort `low`:** if the quality review shows weak answers, raise `Anthropic:Effort` to `medium` in config.
- **Refusal fallback** is not enabled (D-09); revisit if refusals appear in the logs.
- **Next phases (out of scope):** write actions with confirmation (phase 2), stored conversations, the scheduled insights agent, streaming answers, an automated eval.
