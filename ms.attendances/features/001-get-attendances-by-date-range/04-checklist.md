# [001] Get attendances by date range — Implementation Checklist

> Last reviewed: 2026-10-22
> Overall: ✅ Complete

Reviewed against `01-business-spec.md`, `02-technical-spec.md`, `03-tasks.md` (all Approved 2026-10-22), the working-tree code, a fresh full test run, and the T-12 run against the local docker-compose stack.

## 1. Acceptance criteria
| AC | Flow | Status | Proving test(s) | Implementation |
|----|------|--------|-----------------|----------------|
| AC-01 range filter, most recent first | UC-01 main | ✅ | `AttendanceRepositoryTests.GetAttendancesByDateRangeShouldFilterFromStartOfFromDayToStartOfDayAfterTo`, `…ShouldSortByDateDescending`, `…ShouldReturnMappedRecords`; `GetAttendancesByDateRangeQueryHandlerTests.HandleShouldReturnMappedAttendances`; `AttendancesControllerTests.AttendancesByDateRangeShouldReturnOkResult`. T-12: 4 records 03-31→03-10→03-02→03-01, 04-05 excluded | `AttendanceRepository.GetAttendancesByDateRange`, `GetAttendancesByDateRangeQueryHandler`, `AttendancesController.AttendancesByDateRange` |
| AC-02 whole days, inclusive ends | UC-01 main | ✅ | `AttendanceRepositoryTests.GetAttendancesByDateRangeShouldFilterFromStartOfFromDayToStartOfDayAfterTo` (`$gte 03-01T00:00Z`, `$lt 04-01T00:00Z`). T-12: 03-01 08:00 and 03-31 18:00 returned | `AttendanceRepository.GetAttendancesByDateRange` |
| AC-03 only that employee | UC-01 main | ✅ | `AttendanceRepositoryTests.GetAttendancesByDateRangeShouldFilterByUserName`. T-12: `admin` → `[]` while `sdd001user` has records | `AttendanceRepository.GetAttendancesByDateRange` |
| AC-04 no records → empty list | UC-01 A1 | ✅ | `GetAttendancesByDateRangeQueryHandlerTests.HandleShouldReturnEmptyListWhenNoAttendancesInRange`, `AttendancesControllerTests.AttendancesByDateRangeShouldReturnOkWithEmptyListWhenNoAttendances`. T-12: 200 `[]` | handler, controller |
| AC-05 unknown employee → empty list | UC-01 A1 | ✅ | `GetAttendancesByDateRangeQueryHandlerTests.HandleShouldReturnEmptyListWhenEmployeeDoesNotExist`. T-12: `ghost` → 200 `[]` | handler, repository |
| AC-06 same day | UC-01 A2 | ✅ | `GetAttendancesByDateRangeRequestTests.ValidateShouldPassWhenFromEqualsTo`, `AttendanceRepositoryTests.GetAttendancesByDateRangeShouldCoverWholeDayWhenFromEqualsTo`. T-12: one record (03-10) | `GetAttendancesByDateRangeRequest`, `AttendanceRepository` |
| AC-07 exactly 31 days valid | UC-01 A3 | ✅ | `GetAttendancesByDateRangeRequestTests.ValidateShouldPassWhenRangeIsExactly31Days`. T-12: 200 | `GetAttendancesByDateRangeRequest` |
| AC-08 unauthenticated → rejected | UC-01 E1 | ✅ | `AttendancesControllerTests.AttendancesControllerShouldRequireAuthentication` (passed before implementation; regression guard). T-12: 401 | class-level `[Authorize]` on `AttendancesController` (pre-existing) |
| AC-09 non-admin → rejected | UC-01 E2 | ✅ | `AttendancesControllerTests.AttendancesByDateRangeShouldRequireAdminRole`. T-12: non-admin querying own data → 403 | `[Authorize(Roles = "admin")]` on `AttendancesByDateRange` |
| AC-10 missing / invalid input | UC-01 E3 | ✅ ¹ | `GetAttendancesByDateRangeRequestTests.ValidateShouldFailWhenUserNameIsMissing` (null, ""), `…WhenFromIsMissing`, `…WhenToIsMissing`. T-12: 400 naming `UserName` / `From`; `from=2026-02-30` → 400 "The value '2026-02-30' is not valid for From." | `GetAttendancesByDateRangeRequest` `[Required]`; `DateOnly` model binding + `[ApiController]` automatic 400 |
| AC-11 from > to | UC-01 E4 | ✅ ² | `GetAttendancesByDateRangeRequestTests.ValidateShouldFailWhenFromIsLaterThanTo`. T-12: 400 with the expected message | `GetAttendancesByDateRangeRequest.Validate` |
| AC-12 > 31 days | UC-01 E5 | ✅ ² | `GetAttendancesByDateRangeRequestTests.ValidateShouldFailWhenRangeExceeds31Days`. T-12: 400 with the expected message | `GetAttendancesByDateRangeRequest.Validate` |
| AC-13 history unavailable | UC-01 E6 | ✅ | `GetAttendancesByDateRangeQueryHandlerTests.HandleShouldLogErrorAndRethrowWhenRepositoryFails`. T-12 with the Mongo container stopped: 500 and `fail: …GetAttendancesByDateRangeQueryHandler … Error getting attendances for sdd001user between …` logged | `GetAttendancesByDateRangeQueryHandler` (log + rethrow) |

¹ The unparsable-date part of AC-10 is proven only by the manual T-12 run, not by an automated test. This was agreed in tech spec D-03 because the project has no integration tests.
² "Attendances are not looked up" relies on `[ApiController]` short-circuiting with 400 before the action runs. That is framework behavior; unit tests prove the validation result, not the short-circuit.

Every flow (main, A1–A3, E1–E6) has at least one passing test.

## 2. Tasks
| Task | Status | Evidence / note |
|------|--------|-----------------|
| T-01 | ✅ | 9 test cases in `ms.attendances.application.test/Request/GetAttendancesByDateRangeRequestTests.cs`; red (type missing) before T-02; all pass |
| T-02 | ✅ | `ms.attendances.application/Request/GetAttendancesByDateRangeRequest.cs` |
| T-03 | ✅ | 5 tests added to `ms.attendances.infraestucture.test/Repositories/AttendanceRepositoryTests.cs`; red (method missing) before T-04; all pass |
| T-04 | ✅ | `IAttendanceRepository.GetAttendancesByDateRange`, `AttendanceRepository.GetAttendancesByDateRange` |
| T-05 | ✅ | 4 tests in `ms.attendances.application.test/Queries/GetAttendancesByDateRangeQueryHandlerTests.cs`; red before T-06; all pass |
| T-06 | ✅ | `GetAttendancesByDateRangeQuery.cs`, `Handlers/GetAttendancesByDateRangeQueryHandler.cs`; no `.csproj` change needed |
| T-07 | ✅ | 5 tests added to `ms-attendances.api.test/Controllers/AttendancesControllerTests.cs`; 4 red before T-08, 1 already green (pre-existing `[Authorize]`) |
| T-08 | ✅ | `AttendancesController.AttendancesByDateRange` |
| T-09 | ✅ | No DI change. Confirmed at runtime: the handler resolved and logged in T-12 |
| T-10 | ✅ | `README.md` — new `### ms.attendances endpoints` table |
| T-11 | ✅ | See §4 |
| T-12 | ✅ | curl script against the local stack; results in `03-tasks.md` Implementation notes |

## 3. Convention compliance
- [x] Layering respected: request DTO, query and handler in application; repository interface in domain; Mongo filter in infrastructure; thin controller in api
- [x] Block namespaces matching folder paths; one public class per file
- [x] CQRS: `record GetAttendancesByDateRangeQuery(...) : IRequest<IEnumerable<AttendanceResponse>>`; handler in `Queries/Handlers` named `<Name>Handler`
- [x] Controller is thin, injects only `IMediator`, returns `Ok(await _mediator.Send(...))`; action named as a noun (`AttendancesByDateRange`)
- [x] Authorization: `[Authorize(Roles = "admin")]` for an admin-only endpoint
- [x] Constructor injection into `private readonly _camelCase` fields
- [x] Mapping reuses the existing `AttendanceProfile` (`AttendanceRecord` → `AttendanceResponse`); no persistence type leaks into the domain
- [x] No raw `IConfiguration`; no new config keys
- [x] Structured logging with `ILogger<T>` and a message template
- [x] All I/O async; the new repository method uses `await cursor.ToListAsync()` (no `.Result`)
- [x] No new packages; Central Package Management untouched
- [x] Tests: xUnit + Moq; `Mock<T>` fields built in the constructor; `MethodShouldExpectedBehavior` names; `// Arrange // Act // Assert`
- [x] Existing Refit contract `GET /Attendances?userName=` unchanged (regression checked in T-12)
- [x] Attendance history stays append-only; the feature is read-only
- [x] `AttendanceRepositoryTests` has a nested private helper class `CapturedFind` (test-only holder for captured arguments). Reviewed with the user on 2026-10-22: accepted as is, not a violation of "one class per file".
- [x] File encoding: the 5 new `.cs` files were saved without a UTF-8 BOM, unlike the existing files. Fixed on 2026-10-22: BOM added, application tests rerun (20/20 passed). Line endings are normalized by `core.autocrlf=true`.

## 4. Build & tests
```
dotnet build EmployeeAttendanceControl.sln
  0 Errores (no warnings from the feature's files)

dotnet test EmployeeAttendanceControl.sln
  ms-attendances.api.test             Superado: 15  Con error: 0
  ms.attendances.application.test     Superado: 20  Con error: 0
  ms.attendances.infraestucture.test  Superado:  9  Con error: 0
  ms.employees.* (3 projects)         Superado: 21  Con error: 0
  ms.users.* (3 projects)             Superado: 27  Con error: 0
  Total: 92 passed, 0 failed, 0 skipped (23 of them belong to this feature)
```

## 5. Missing / pending
- Nothing pending in code or docs.
- T-12 test data (`sdd001user` in SQL Server `dbo.Employee`, Cassandra `kusers.user` and its 5 Mongo records) was deleted on 2026-10-22 and verified with 0 remaining rows. The docker-compose stack was stopped.
- Committed and pushed on `feature/001-get-attendances-by-date-range`; PR opened to `master`.

## 6. Deviations from spec
- Tech spec §6 ⚠ (possible `Microsoft.Extensions.Logging` reference in `ms.attendances.application.csproj`): not needed, because `ILogger<T>` resolves transitively. Spec not updated; recorded here and in `03-tasks.md`.
- Tech spec §2 controller snippet uses `request.From.Value`. The code uses `request.From!.Value` to silence CS8629 in the nullable-enabled API project. No behavior change; spec not updated.
- T-12 was executed with a curl script instead of the Swagger UI. The checks are the same.
- T-05 failure test uses `TimeoutException` instead of `MongoException`. This was foreseen in the task list, and it is also the exception the driver actually threw in T-12.

## 7. Risks & follow-ups
- No `{ UserName: 1, Date: -1 }` index on `DbHistoricalAttendance.Attendances` (tech spec D-04/§10). Fine at current volume; add one if the history grows.
- Dates are bucketed by their stored UTC day with no time-zone conversion (Q-03, D-04). Records written near local midnight may fall on the neighboring day.
- The existing `GET /Attendances` still lets any authenticated user read any employee's full history. It is out of scope here (business spec §8) and recommended as a separate feature.
- The existing `GetAllAttendances` still uses sync-over-async (`.ToListAsync().Result`). It was left untouched and is a candidate for a small refactor.
- The unparsable-date path (AC-10) and the 400 short-circuit (AC-11/12) have no automated test because the project has no integration tests. Consider `WebApplicationFactory` tests if the team adopts them.
