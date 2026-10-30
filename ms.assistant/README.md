# ms.assistant

AI assistant service. Answers attendance questions in natural language (e.g. "¿cuántas veces falté este mes?", "who is absent right now?") using Claude, the Anthropic model. It reads data from the other services with the caller's own permissions. It is read-only and has no persistence.

## How it works (agent)

The service is an **agent**: the model decides which information it needs, and the service fetches it.

```
1. tools    = tools allowed for the caller's role
2. messages = previous messages (sent by the client) + new question
3. loop (max Assistant:MaxToolRounds):
     reply = Claude(system prompt, messages, tools)
     no tool requested → return reply text
     otherwise         → run each requested tool with the caller's JWT and send the results back
4. log usage (tokens, tools used; never the question or answer text)
```

- **The model decides, the code acts.** Claude only asks for a tool. `ms.assistant` runs it by calling the owning service with the caller's `Authorization` header, so each service still checks permissions.
- **Tools depend on the role.** A regular employee is only offered `get_my_attendances`, which has no user input: the user always comes from the JWT.
- **Tool failures don't stop the loop.** If a service is down, the tool returns an error result and the model explains it to the user instead of inventing data.

| Tool | Offered to | Calls |
|------|-----------|-------|
| `get_my_attendances` | everyone | `GET ms.attendances /Attendances?userName={caller}` |
| `get_attendances_by_date_range` | admin | `GET ms.attendances /Attendances/ByDateRange` (max 31 days) |
| `list_employees` | admin | `GET ms.employees /Employees` |

## Responsibility

The solution is split into four projects:

- `ms.assistant.api`: HTTP API, JWT, Swagger, DI, and the `503` exception handler.
- `ms.assistant.application`: `ChatCommand` and its handler (the agent loop), the tools, the system prompt (`Prompts/AssistantPrompt.cs`), and the Refit clients to the other services.
- `ms.assistant.domain`: provider-neutral types (`ChatMessage`, `ToolCall`, `ToolResult`, `ModelReply`) and the model port `IAssistantModel`.
- `ms.assistant.infraestucture`: the Claude adapter (`ClaudeAssistantModel`, `ClaudeMessageMapper`) built on the official `Anthropic` SDK. It is the only project that references the SDK.

## API

In development it is exposed at `http://localhost:9030`. Clients use the gateway (`http://localhost:9000/Assistant/Chat`).

| Method | Route | Authorization | Function |
| ------ | ----- | ------------- | -------- |
| `POST` | `/Assistant/Chat` | JWT (any user) | Answers a question, using the previous messages of the conversation. |

```json
{ "question": "¿Y la semana pasada?",
  "history": [ { "role": "user", "content": "¿Cuántas veces falté este mes?" },
               { "role": "assistant", "content": "Dos veces: el 3 y el 17 de octubre." } ] }
```
```json
{ "answer": "La semana pasada no tienes ausencias registradas." }
```

| Status | When |
| ------ | ---- |
| `400` | Question empty or longer than 2,000 characters, more than 20 previous messages, or a previous message with an invalid role or empty content |
| `401` | No valid JWT |
| `503` | The AI service is unavailable (rate limit, server error, network, timeout) |
| `500` | Invalid Anthropic configuration (e.g. wrong API key) |

## Configuration

`appsettings.json`, overridden by environment variables in `docker-compose.override.yml`:

| Key | Default | Description |
| --- | ------- | ----------- |
| `Anthropic:ApiKey` | (empty) | **Required.** Never committed. In Docker it comes from `ANTHROPIC_API_KEY` in the root `.env` file (copy `.env.example`). Locally, use `dotnet user-secrets`. |
| `Anthropic:Model` | `claude-opus-5-5` | Claude model. |
| `Anthropic:Effort` | `low` | Reasoning effort (`low`, `medium`, `high`, `max`). Raise it if answers are weak. |
| `Anthropic:MaxTokens` | `4096` | Maximum answer length per model call. |
| `Anthropic:TimeoutSeconds` | `60` | Timeout per model call. |
| `Assistant:MaxToolRounds` | `5` | Maximum model calls per question. |
| `Communication:External:AttendanceApiUrl` | `http://localhost:9020` | Direct URL of ms.attendances (in Docker: `http://ms.attendances.api:80`). |
| `Communication:External:EmployeeApiUrl` | `http://localhost:9010` | Direct URL of ms.employees (in Docker: `http://ms.employees.api:80`). |
| `Authentication:JWT` | — | Same key as the other services. |

The service fails at startup if `Anthropic:ApiKey` is missing.

## Communication with other services

- **Refit towards ms.attendances and ms.employees** (direct, not through the gateway), forwarding the caller's `Authorization` header. It depends on the contracts of `GET /Attendances`, `GET /Attendances/ByDateRange` and `GET /Employees`. Changing them breaks the assistant's tools.
- **Anthropic API** (external HTTPS) through the `Anthropic` SDK. Attendance data (names, dates, notes) is sent to Anthropic to produce the answers.
- No RabbitMQ events.

## Security

- `[Authorize]` on the controller. The role is taken from `ClaimTypes.Role` and the user from `ClaimTypes.NameIdentifier`.
- Admin-only tools are never offered to regular users, and the loop refuses any tool outside the allowed list.
- Read-only: the Refit clients only declare `GET` (a unit test enforces this).
- Data returned by the services is only passed to the model as tool results, never as instructions. The system prompt says so (protection against prompt injection in notes).
- Logs contain no question text, answer text or tokens.

## Dependencies

- .NET `10.0` and ASP.NET Core.
- MediatR and JWT Bearer.
- Refit (+ `Refit.HttpClientFactory`, `Refit.Reflection`) for the calls to the other services.
- `Anthropic` (official Anthropic C# SDK) in the infrastructure project.
- Swashbuckle for Swagger.

Package versions are managed centrally via `Directory.Packages.props` at the repository root (NuGet Central Package Management).

## Tests

The `ms.assistant.test` folder contains xUnit test projects, one per layer:

- `ms.assistant.api.test`: controller and `503` handler.
- `ms.assistant.application.test`: request validation, tools, tool catalog, system prompt, and the agent loop (`ChatCommandHandlerTests` uses a scripted `Mock<IAssistantModel>`, so no real AI calls).
- `ms.assistant.infraestucture.test`: mapping to and from the Anthropic SDK types.

They use `xunit`, `Moq`, and `coverlet.collector`. Run them with:

```powershell
dotnet test ms.assistant/ms.assistant.test/ms.assistant.api.test
dotnet test ms.assistant/ms.assistant.test/ms.assistant.application.test
dotnet test ms.assistant/ms.assistant.test/ms.assistant.infraestucture.test
```

Answer quality can't be unit-tested. A person reviews it with the sample questions in `features/001-ai-attendance-assistant/eval-questions.md`.

## Running

```powershell
Copy-Item .env.example .env      # set ANTHROPIC_API_KEY=... in .env
docker compose up -d --build ms.attendances.api ms.employees.api ms.assistant.api ms.gateway.api
docker compose logs -f ms.assistant.api
```

The design (business spec, technical spec, tasks, checklist) is in `features/001-ai-attendance-assistant/`.
