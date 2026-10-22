# [001] Get attendances by date range — Technical Specification

> Status: Approved (2026-10-22)
> Based on: `01-business-spec.md` (Approved 2026-10-22)
> Reference implementation followed: `GET /Attendances` — `AttendancesController.Attendances` → `GetAllAttendancesQuery` / `GetAllAttendancesQueryHandler` → `IAttendanceRepository.GetAllAttendances` → `AttendanceRepository`

## 1. Overview
A new admin-only endpoint `GET /Attendances/ByDateRange?userName=&from=&to=` is added to `AttendancesController`, alongside the existing `GET /Attendances`. That route is left untouched because `ms.employees` calls it through Refit.

Input validation (BR-02, BR-03, BR-04) lives in a query-string request DTO that uses DataAnnotations and `IValidatableObject`. `[ApiController]` already turns model-state errors into an automatic `400 ValidationProblemDetails` that names the failing field, so invalid requests never reach MediatR and the history is never queried for them.

Valid requests are sent as `GetAttendancesByDateRangeQuery`. Its handler calls a new repository method that builds a Mongo filter on `UserName` plus the whole-day bounds `[from 00:00, to+1 00:00)`, sorted by `Date` descending. The handler maps the result to `AttendanceResponse`, the same DTO `GET /Attendances` returns.

## 2. Components by layer

### API
- (~) `ms.attendances.api/Controllers/AttendancesController.cs` — new action:
  ```csharp
  [HttpGet("ByDateRange")]
  [Authorize(Roles = "admin")]
  public async Task<IActionResult> AttendancesByDateRange([FromQuery] GetAttendancesByDateRangeRequest request)
      => Ok(await _mediator.Send(new GetAttendancesByDateRangeQuery(request.UserName, request.From.Value, request.To.Value)));
  ```
  The class-level `[Authorize]` stays; the method-level `Roles = "admin"` adds the role requirement.

### Application
- (+) `ms.attendances.application/Request/GetAttendancesByDateRangeRequest.cs` — `class GetAttendancesByDateRangeRequest : IValidatableObject`
  - `[Required] string UserName`
  - `[Required] DateOnly? From`, `[Required] DateOnly? To` (nullable so that a missing value is reported as "required" instead of defaulting to `0001-01-01`; a non-date value such as `2026-02-30` fails model binding with "The value '…' is not valid for From.")
  - `const int MaxRangeDays = 31`
  - `Validate(...)`:
    - `From > To` → `ValidationResult("The start date cannot be later than the end date.", [nameof(From), nameof(To)])`
    - `To.DayNumber - From.DayNumber + 1 > MaxRangeDays` → `ValidationResult("The date range cannot exceed 31 days.", [nameof(From), nameof(To)])`
- (+) `ms.attendances.application/Queries/GetAttendancesByDateRangeQuery.cs` — `record GetAttendancesByDateRangeQuery(string UserName, DateOnly From, DateOnly To) : IRequest<IEnumerable<AttendanceResponse>>`
- (+) `ms.attendances.application/Queries/Handlers/GetAttendancesByDateRangeQueryHandler.cs` — injects `IAttendanceRepository`, `IMapper`, `ILogger<GetAttendancesByDateRangeQueryHandler>`. It calls `GetAttendancesByDateRange(UserName, From, To)` and maps to `IEnumerable<AttendanceResponse>`. On exception it logs an error and rethrows (§8).
- (=) `AttendanceResponse` and `AttendanceProfile` are reused as they are; no new mapping.

### Domain
- (~) `ms.attendances.domain/Repositories/IAttendanceRepository.cs` — new method:
  `Task<IEnumerable<AttendanceRecord>> GetAttendancesByDateRange(string userName, DateOnly from, DateOnly to);` (`from`/`to` inclusive)

### Infrastructure
- (~) `ms.attendances.infraestucture/Repositories/AttendanceRepository.cs` — implementation:
  ```csharp
  var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
  var endExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
  var filter = Builders<AttendanceMongo>.Filter.And(
      Builders<AttendanceMongo>.Filter.Eq(a => a.UserName, userName),
      Builders<AttendanceMongo>.Filter.Gte(a => a.Date, start),
      Builders<AttendanceMongo>.Filter.Lt(a => a.Date, endExclusive));
  var options = new FindOptions<AttendanceMongo, AttendanceMongo> { Sort = Builders<AttendanceMongo>.Sort.Descending(a => a.Date) };
  var cursor = await _context.AttendanceCollection.FindAsync(filter, options);
  return _mapper.Map<IEnumerable<AttendanceRecord>>(await cursor.ToListAsync());
  ```
  Unlike `GetAllAttendances`, this method uses `await cursor.ToListAsync()` and never `.Result`.

## 3. API contract
| Method | Route | Auth | Request | Response | Errors |
|--------|-------|------|---------|----------|--------|
| GET | `/Attendances/ByDateRange?userName={userName}&from={yyyy-MM-dd}&to={yyyy-MM-dd}` | JWT, role `admin` | query string | `200` `AttendanceResponse[]` (possibly `[]`), most recent first | `401` not authenticated · `403` not admin · `400` invalid input (ValidationProblemDetails) · `500` history unavailable |

Example, `GET /Attendances/ByDateRange?userName=jdoe&from=2026-03-01&to=2026-03-31`:
```json
[
  { "id": "…", "userName": "jdoe", "fullName": "John Doe", "date": "2026-03-10T08:00:00Z", "attendance": true,  "notes": "" },
  { "id": "…", "userName": "jdoe", "fullName": "John Doe", "date": "2026-03-02T08:05:00Z", "attendance": false, "notes": "Sick" }
]
```
400 example (range > 31 days):
```json
{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "From": ["The date range cannot exceed 31 days."], "To": ["The date range cannot exceed 31 days."] } }
```

## 4. Data model & persistence
- No schema change. The query reads the existing collection `DbHistoricalAttendance.Attendances` (`AttendanceMongo`).
- No new index. Ranges are at most 31 days for a single user and the collection is small. An index `{ UserName: 1, Date: -1 }` would be the natural follow-up if volume grows (§10).

## 5. Inter-service impact
| Service | Change | Mechanism (event / HTTP) | Contract |
|---------|--------|--------------------------|----------|
| ms.employees | None. The existing `GET /Attendances` Refit route is not modified. | HTTP (Refit) | unchanged |
| ms.users | None. The JWT already carries `ClaimTypes.Role` (`admin`). | — | unchanged |

## 6. Configuration & dependencies
- No new config keys, no docker-compose changes.
- DI: nothing to register. MediatR already scans the application assembly (`AddConsummer`), and `AttendanceRepository` / `IMapper` are already registered.
- ⚠ **Package reference (no new package):** the handler needs `ILogger<T>` (`Microsoft.Extensions.Logging.Abstractions`). If MediatR doesn't already bring it in transitively, `ms.attendances.application.csproj` gets `<PackageReference Include="Microsoft.Extensions.Logging" />`. That package is already pinned in `Directory.Packages.props` (10.0.12), so the change is only a reference in this project, not a new dependency for the solution.
- README: add the new endpoint to the `ms.attendances` description / sequence section.

## 7. Security
- Authentication: JWT bearer (class-level `[Authorize]`), configured by `AddAuthenticationSettings`. No token → `401` (BR-01, E1).
- Authorization: `[Authorize(Roles = "admin")]` on the action. An authenticated non-admin gets `403`, including when they query their own `userName` (BR-01, E2).
- Read-only: there are no writes on any path (BR-08).

## 8. Error handling & logging
| Case | Behavior |
|------|----------|
| No/invalid token | `401` from the JWT middleware; the controller is not executed |
| Not admin | `403` from the authorization filter; the controller is not executed |
| Missing `userName`/`from`/`to`, or unparsable date | `400` ValidationProblemDetails with the field name as key (automatic `[ApiController]` model-state response); MediatR is not called |
| `from > to` | `400`, message "The start date cannot be later than the end date."; MediatR is not called |
| Range > 31 days | `400`, message "The date range cannot exceed 31 days."; MediatR is not called |
| No records / unknown employee | `200 []` |
| Mongo unavailable / driver exception | The handler logs `LogError(ex, "Error getting attendances for {UserName} between {From} and {To}", ...)` and rethrows. ASP.NET Core returns `500` and no partial data is returned. |

## 9. Traceability
| AC | Components | Test level (unit / integration) |
|----|------------|---------------------------------|
| AC-01 (range filter + order) | `AttendanceRepository.GetAttendancesByDateRange` (filter bounds, `Sort` descending); handler mapping | unit: repository (rendered filter and sort), handler |
| AC-02 (whole days, inclusive ends) | `AttendanceRepository` (`Gte` start 00:00, `Lt` to+1 00:00) | unit: repository (rendered filter) |
| AC-03 (only that employee) | `AttendanceRepository` (`Eq UserName`) | unit: repository (rendered filter) |
| AC-04 (empty range → []) | handler, controller | unit: handler, controller |
| AC-05 (unknown employee → []) | same path as AC-04 | unit: handler |
| AC-06 (same day) | `GetAttendancesByDateRangeRequest` (valid); repository bounds `[d 00:00, d+1 00:00)` | unit: request validation, repository |
| AC-07 (exactly 31 days valid) | `GetAttendancesByDateRangeRequest` | unit: request validation |
| AC-08 (unauthenticated) | `[Authorize]` on `AttendancesController` | unit: controller (attribute via reflection) |
| AC-09 (non-admin) | `[Authorize(Roles = "admin")]` on the action | unit: controller (attribute via reflection) |
| AC-10 (missing / invalid input) | `GetAttendancesByDateRangeRequest` `[Required]`; `DateOnly` model binding | unit: request validation (missing fields). Unparsable date relies on framework model binding (see D-03). |
| AC-11 (from > to) | `GetAttendancesByDateRangeRequest.Validate` | unit: request validation |
| AC-12 (> 31 days) | `GetAttendancesByDateRangeRequest.Validate` | unit: request validation |
| AC-13 (history unavailable) | `GetAttendancesByDateRangeQueryHandler` (log + rethrow) | unit: handler (repository throws → exception propagates, error logged) |

## 10. Risks, trade-offs & decisions
- **D-01 — New route instead of adding optional params to `GET /Attendances`.** That route is non-admin and is consumed by `ms.employees` via Refit. Changing its auth or contract would break the client and mix two business rules. Rejected: overloading the existing action.
- **D-02 — Validation in the request DTO (DataAnnotations + `IValidatableObject`) using `[ApiController]`'s automatic 400.** It needs no new package, keeps the controller thin, and gives the "which input is wrong" message for free. Rejected: FluentValidation (new package, team decision), MediatR pipeline behavior plus an exception-to-400 middleware (more infrastructure than the feature needs), and validation in the handler (the history would be called through MediatR before rejecting).
- **D-03 — Unparsable dates are handled by framework model binding.** `DateOnly` binding failures are added to `ModelState` by ASP.NET Core and returned as `400` with the field name. Since the project has no integration tests, this specific path isn't unit-tested. It is verified manually via Swagger in the final verification task.
- **D-04 — Day bounds built as UTC `DateTime`s with no time-zone conversion (Q-03).** The Mongo driver stores `DateTime` as UTC, and building the bounds with `DateTimeKind.Utc` compares calendar days exactly as they are stored. If dates were originally written as local times, records near midnight are bucketed by their stored (UTC) day. This is accepted for this feature.
- **D-05 — Sorting in Mongo (`Sort.Descending(Date)`) rather than in memory.** It uses the database ordering and is cheap. Ties (same timestamp) have no guaranteed secondary order.
- **D-06 — The ">31 days" rule lives in one constant (`MaxRangeDays`) in the request DTO.** It is not configurable via `IOptions`; the business spec fixes it at 31.
- **Risk:** no index on `{UserName, Date}`. Acceptable at current volume; listed as a follow-up.
