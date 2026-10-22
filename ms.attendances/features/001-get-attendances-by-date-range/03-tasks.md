# [001] Get attendances by date range — Tasks (TDD)

> Status: Approved (2026-10-22)
> Based on: `02-technical-spec.md` (Approved 2026-10-22)

Legend: `[TEST]` write a failing test (red) · `[IMPL]` make it pass (green) · `[REFACTOR]` · `[SETUP]` config/infra/docs

Test projects (abbreviations used below):
- `app.test` = `ms.attendances/ms-attendances.test/ms.attendances.application.test`
- `infra.test` = `ms.attendances/ms-attendances.test/ms.attendances.infraestucture.test`
- `api.test` = `ms.attendances/ms-attendances.test/ms-attendances.api.test` (namespace `ms_attendances.api.test`)

All tests: xUnit + Moq, mocks as `Mock<T>` fields created in the constructor, `// Arrange // Act // Assert`, names `MethodShouldExpectedBehavior`.

## Group 1 — Input validation (AC-06, AC-07, AC-10, AC-11, AC-12)
Tests run `Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true)` on `GetAttendancesByDateRangeRequest`.

- [x] **T-01** `[TEST]` `app.test/Request/GetAttendancesByDateRangeRequestTests.cs`
  - `ValidateShouldPassWhenRangeIsValid` — `jdoe`, 2026-03-01 → 2026-03-31: valid, no results.
  - `ValidateShouldPassWhenFromEqualsTo` — 2026-03-10 → 2026-03-10: valid. (AC-06)
  - `ValidateShouldPassWhenRangeIsExactly31Days` — 2026-01-01 → 2026-01-31: valid. (AC-07)
  - `ValidateShouldFailWhenUserNameIsMissing` — `UserName` null/empty: invalid, an error with member `UserName`. (AC-10)
  - `ValidateShouldFailWhenFromIsMissing` — `From` null: invalid, an error with member `From`. (AC-10)
  - `ValidateShouldFailWhenToIsMissing` — `To` null: invalid, an error with member `To`. (AC-10)
  - `ValidateShouldFailWhenFromIsLaterThanTo` — 2026-03-10 → 2026-03-01: invalid, message "The start date cannot be later than the end date.", members `From`, `To`. (AC-11)
  - `ValidateShouldFailWhenRangeExceeds31Days` — 2026-01-01 → 2026-02-01: invalid, message "The date range cannot exceed 31 days.", members `From`, `To`. (AC-12)
- [x] **T-02** `[IMPL]` `ms.attendances.application/Request/GetAttendancesByDateRangeRequest.cs` — class with `[Required] UserName`, `[Required] DateOnly? From/To`, `MaxRangeDays = 31`, `IValidatableObject.Validate` (makes T-01 pass).

## Group 2 — Repository: filter by employee and whole-day range, most recent first (AC-01, AC-02, AC-03, AC-06)
Tests capture the `FilterDefinition` and `FindOptions` passed to the mocked `IMongoCollection<AttendanceMongo>.FindAsync`. They render them with `RenderArgs<AttendanceMongo>(BsonSerializer.SerializerRegistry.GetSerializer<AttendanceMongo>(), BsonSerializer.SerializerRegistry)` and assert on the resulting `BsonDocument`. The cursor mock follows the existing `AttendanceRepositoryTests`.

- [x] **T-03** `[TEST]` `infra.test/Repositories/AttendanceRepositoryTests.cs` (add to existing class)
  - `GetAttendancesByDateRangeShouldFilterByUserName` — rendered filter has `UserName == "jdoe"`. (AC-03)
  - `GetAttendancesByDateRangeShouldFilterFromStartOfFromDayToStartOfDayAfterTo` — 2026-03-01 → 2026-03-31 renders `Date $gte 2026-03-01T00:00:00Z` and `Date $lt 2026-04-01T00:00:00Z`. (AC-01, AC-02)
  - `GetAttendancesByDateRangeShouldCoverWholeDayWhenFromEqualsTo` — 2026-03-10 → 2026-03-10 renders `$gte 2026-03-10T00:00Z`, `$lt 2026-03-11T00:00Z`. (AC-06)
  - `GetAttendancesByDateRangeShouldSortByDateDescending` — rendered `options.Sort` equals `{ Date: -1 }`. (AC-01)
  - `GetAttendancesByDateRangeShouldReturnMappedRecords` — the cursor's documents are mapped through `IMapper.Map<IEnumerable<AttendanceRecord>>` and returned. (AC-01)
- [x] **T-04** `[IMPL]`
  - `ms.attendances.domain/Repositories/IAttendanceRepository.cs` — add `GetAttendancesByDateRange(string userName, DateOnly from, DateOnly to)`.
  - `ms.attendances.infraestucture/Repositories/AttendanceRepository.cs` — implement as in tech spec §2 (`And(Eq, Gte, Lt)`, UTC bounds, `Sort.Descending`, `await ToListAsync()`).
  - Makes T-03 pass.

## Group 3 — Query handler: map results, empty list, failure logged (AC-01, AC-04, AC-05, AC-13)
- [x] **T-05** `[TEST]` `app.test/Queries/GetAttendancesByDateRangeQueryHandlerTests.cs` (mocks: `IAttendanceRepository`, `IMapper`, `ILogger<GetAttendancesByDateRangeQueryHandler>`)
  - `HandleShouldReturnMappedAttendances` — the repository is called with (`jdoe`, 2026-03-01, 2026-03-31) and the handler returns the mapper's `AttendanceResponse` list unchanged, preserving order. (AC-01)
  - `HandleShouldReturnEmptyListWhenNoAttendancesInRange` — the repository returns an empty list, so the result is empty and not null. (AC-04)
  - `HandleShouldReturnEmptyListWhenEmployeeDoesNotExist` — the repository returns an empty list for `ghost`, so the result is empty and no exception is thrown. (AC-05)
  - `HandleShouldLogErrorAndRethrowWhenRepositoryFails` — the repository throws `MongoException`. Asserts `Assert.ThrowsAsync<MongoException>` and that `ILogger.Log` was called once with `LogLevel.Error` and the exception. (AC-13)
    - `app.test` has no MongoDB.Driver reference, so if needed the test uses `TimeoutException` instead. Note it in Implementation notes.
- [x] **T-06** `[IMPL]`
  - `ms.attendances.application/Queries/GetAttendancesByDateRangeQuery.cs` — the record.
  - `ms.attendances.application/Queries/Handlers/GetAttendancesByDateRangeQueryHandler.cs` — handler with try/catch, `LogError(ex, "Error getting attendances for {UserName} between {From} and {To}", ...)` and `throw;`.
  - Add `<PackageReference Include="Microsoft.Extensions.Logging" />` to `ms.attendances.application.csproj` only if `ILogger<T>` doesn't resolve transitively.
  - Makes T-05 pass.

## Group 4 — Controller: endpoint and authorization (AC-04, AC-08, AC-09)
- [x] **T-07** `[TEST]` `api.test/Controllers/AttendancesControllerTests.cs` (add to existing class)
  - `AttendancesByDateRangeShouldReturnOkResult` — the mediator returns `List<AttendanceResponse>`. Asserts `OkObjectResult` with value `List<AttendanceResponse>` and that the mediator received `GetAttendancesByDateRangeQuery("jdoe", 2026-03-01, 2026-03-31)`. (AC-01)
  - `AttendancesByDateRangeShouldReturnOkWithEmptyListWhenNoAttendances` — the mediator returns an empty list, so the result is `OkObjectResult` with an empty list. (AC-04)
  - `AttendancesControllerShouldRequireAuthentication` — the class has `[Authorize]`. (AC-08)
  - `AttendancesByDateRangeShouldRequireAdminRole` — the method has `[Authorize]` with `Roles == "admin"`. (AC-09)
  - `AttendancesByDateRangeShouldBeHttpGetByDateRange` — the method has `[HttpGet]` with template `ByDateRange`.
- [x] **T-08** `[IMPL]` `ms.attendances.api/Controllers/AttendancesController.cs` — `AttendancesByDateRange([FromQuery] GetAttendancesByDateRangeRequest request)` with `[HttpGet("ByDateRange")]` and `[Authorize(Roles = "admin")]` (makes T-07 pass).

## Group 5 — Wiring & setup
- [x] **T-09** `[SETUP]` DI: confirm nothing extra is needed. MediatR scans the application assembly via `AddConsummer`, and the repository and mapper are already registered. Add no registration unless the build or run proves otherwise.
- [x] **T-10** `[SETUP]` `README.md` — document `GET /Attendances/ByDateRange?userName=&from=&to=` (admin only, max 31 days, most recent first) in the `ms.attendances` section.

## Final verification
- [x] **T-11** Build and run the full suite: `dotnet build EmployeeAttendanceControl.sln` then `dotnet test EmployeeAttendanceControl.sln`. All tests must pass, including the existing ones.
- [x] **T-12** Manual check via Swagger (`docker compose up -d --build`), per tech spec D-03:
  - admin token, valid range → `200` with results ordered most recent first.
  - `from=2026-02-30` → `400` naming `From`.
  - non-admin token → `403`.
  - no token → `401`.

## AC coverage
| AC | Test tasks |
|----|------------|
| AC-01 | T-03, T-05, T-07 |
| AC-02 | T-03 |
| AC-03 | T-03 |
| AC-04 | T-05, T-07 |
| AC-05 | T-05 |
| AC-06 | T-01, T-03 |
| AC-07 | T-01 |
| AC-08 | T-07 (+ T-12 manual) |
| AC-09 | T-07 (+ T-12 manual) |
| AC-10 | T-01 (missing inputs); unparsable date: T-12 manual (D-03) |
| AC-11 | T-01 |
| AC-12 | T-01 |
| AC-13 | T-05 |

## Implementation notes
(Deviations from the plan discovered while implementing.)

- T-03: the new repository tests mock `IAsyncCursor.MoveNextAsync` (not `MoveNext` like the older tests) because the new method uses `await cursor.ToListAsync()`. A shared helper `CreateRepositoryCapturingFind` captures the filter and options passed to `FindAsync`.
- T-05: the failure test uses `TimeoutException` instead of `MongoException` because `app.test` does not reference MongoDB.Driver, as foreseen in the plan.
- T-06: `ILogger<T>` resolves transitively in `ms.attendances.application`, so no `PackageReference` was added to the `.csproj`.
- T-07: `AttendancesControllerShouldRequireAuthentication` passed before T-08 because the class already had `[Authorize]`. It is kept as a regression guard for AC-08; the other T-07 tests were red as expected.
- T-09: no DI change. `AddConsummer` registers MediatR handlers from the application assembly (`typeof(CreateAttendanceCommandHandler).Assembly`), which also contains `GetAttendancesByDateRangeQueryHandler`; `ILogger<T>` is provided by the host. To be confirmed at runtime in T-12.
- T-10: README had no endpoint table, so a `### ms.attendances endpoints` table was added under Components with the three existing/new routes.
- T-08: `request.From!.Value` / `request.To!.Value` used to silence CS8629 in the API project (nullable enabled); non-null is guaranteed by `[Required]` + `[ApiController]` automatic 400.
- T-11: solution build 0 errors; full suite 92/92 passed (9 test projects). Nullability/xUnit warnings in the new tests were fixed (`Exception?` formatter, `string?` theory parameter, `null!` initializers, `method!`).
- T-12: run against the local stack with curl (script instead of Swagger UI), admin + non-admin employee `sdd001user` with 5 seeded records (2026-03-01, 03-02, 03-10, 03-31, 04-05). Results: AC-01/02 200 with 4 records 03-31→03-10→03-02→03-01 (no 04-05); AC-03/04/05 200 `[]`; AC-06 200 one record; AC-07 200; AC-08 401; AC-09 403; AC-10 400 for missing `UserName`, missing `From` and `from=2026-02-30` ("The value '2026-02-30' is not valid for From."); AC-11/12 400 with the expected messages; AC-13 (Mongo container stopped) 500 and `fail: GetAttendancesByDateRangeQueryHandler ... Error getting attendances for sdd001user ...` in the logs; regression `GET /Attendances?userName=` as non-admin still 200. DI wiring (T-09) confirmed at runtime. Test data (`sdd001user` employee/user and its 5 records) remains in the local dev databases.
