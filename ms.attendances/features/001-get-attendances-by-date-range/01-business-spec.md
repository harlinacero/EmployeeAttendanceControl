# [001] Get attendances by date range — Business Specification

> Status: Approved (2026-10-22)
> Owning service: `ms.attendances`
> Author / requested by: harlinacero

## 1. Summary
An admin can look up the attendance records of one employee between two dates, getting the records of that period (at most 31 days), or an empty list when there are none.

## 2. Business goal / problem
Today attendance can only be consulted as an employee's full history. Admins who review attendance for a specific period (e.g. closing a month for payroll, checking absences in a given week) have to pull the whole history and filter it by hand. A bounded, admin-only query by period gives them exactly what they need and keeps each query small, so no pagination is required.

Current fact, kept out of scope (see §8): today any authenticated user can consult any employee's full history.

## 3. Actors
| Actor | Description | Role / permission |
|-------|-------------|-------------------|
| Admin | Person who manages employees and supervises their attendance | `admin` |
| Employee | Regular user who registers their own attendance; must not be able to use this query | regular (non-admin) |

## 4. User stories
- **US-01** — As an admin, I want to consult an employee's attendances between two dates so that I can review that employee's attendance for a specific period.

## 5. Business rules
- **BR-01** — Only admins can consult attendances by date range. Requests from unidentified users or from non-admins are rejected.
- **BR-02** — The query needs three inputs: the employee (identified by their user name), a "from" date and a "to" date. All three are mandatory and the dates must be valid calendar dates.
- **BR-03** — "From" cannot be later than "to". Equal dates are valid (a one-day query).
- **BR-04** — The range covers at most 31 calendar days, counting both ends (1 Oct → 31 Oct is valid; 1 Oct → 1 Nov is 32 days and is rejected).
- **BR-05** — Both ends are inclusive and whole days: every attendance recorded on the "from" day, on the "to" day, or between them is returned, whatever its time of day. Dates are taken as stored in the attendance history, with no time-zone conversion.
- **BR-06** — If the employee has no attendances in the range, the result is an empty list. This is a successful answer, not an error. An employee that doesn't exist is treated the same way.
- **BR-07** — Results are ordered by date, most recent first, and returned in full (no pagination).
- **BR-08** — The query is read-only: it never creates or changes attendance records.

## 6. Use cases

### UC-01 — Consult an employee's attendances by date range (US-01)
- **Actor:** Admin
- **Preconditions:** The admin is identified in the system with the `admin` role.
- **Main flow:**
  1. The admin asks for the attendances of an employee, giving the employee's user name, a "from" date and a "to" date.
  2. The system verifies the requester is identified and is an admin (BR-01).
  3. The system verifies the three inputs are present and the dates are valid (BR-02).
  4. The system verifies "from" is not later than "to" (BR-03).
  5. The system verifies the range does not exceed 31 calendar days (BR-04).
  6. The system finds that employee's attendances recorded from the start of the "from" day to the end of the "to" day (BR-05).
  7. The system returns them ordered most recent first (BR-07).
- **Alternative flows:**
  - **A1** (at step 6) — The employee has no attendances in the range, or does not exist → the system returns an empty list as a successful answer (BR-06). Flow ends.
  - **A2** (at step 4) — "From" and "to" are the same day → valid; the flow continues and returns that day's attendances.
  - **A3** (at step 5) — The range is exactly 31 calendar days → valid; the flow continues.
- **Exception flows:**
  - **E1** (at step 2) — The requester is not identified → rejected as not authenticated; nothing is returned.
  - **E2** (at step 2) — The requester is identified but is not an admin → rejected as not authorized; nothing is returned.
  - **E3** (at step 3) — The user name, "from" or "to" is missing, or a date is not a valid date → rejected as invalid, saying which input is wrong.
  - **E4** (at step 4) — "From" is later than "to" → rejected as invalid, saying the start date cannot be later than the end date. Attendances are not looked up.
  - **E5** (at step 5) — The range exceeds 31 calendar days → rejected as invalid, saying the range cannot exceed 31 days. Attendances are not looked up.
  - **E6** (at step 6) — The attendance history is unavailable → the query fails with a system error and the failure is recorded for support; no partial data is returned.
- **Postconditions:** No data changes in any outcome (BR-08). On success the admin receives the (possibly empty) list; on rejection or failure they receive the reason.

## 7. Acceptance criteria
Each criterion is testable and will become at least one test.

- **AC-01** (UC-01 main, BR-05, BR-07) — *Given* employee "jdoe" has attendances on 2026-03-02, 2026-03-10 and 2026-04-05 *When* an admin consults "jdoe" from 2026-03-01 to 2026-03-31 *Then* only the 2026-03-10 and 2026-03-02 attendances are returned, in that order (most recent first).
- **AC-02** (UC-01 main, BR-05) — *Given* "jdoe" has an attendance at 08:00 on 2026-03-01 and another at 18:00 on 2026-03-31 *When* an admin consults "jdoe" from 2026-03-01 to 2026-03-31 *Then* both attendances are returned.
- **AC-03** (UC-01 main) — *Given* "jdoe" and "asmith" both have attendances in March 2026 *When* an admin consults "jdoe" for March 2026 *Then* only "jdoe"'s attendances are returned.
- **AC-04** (UC-01 A1, BR-06) — *Given* "jdoe" has no attendances between 2026-09-01 and 2026-09-30 *When* an admin consults "jdoe" for that range *Then* the answer is successful with an empty list.
- **AC-05** (UC-01 A1, BR-06) — *Given* there is no employee "ghost" *When* an admin consults "ghost" for any valid range *Then* the answer is successful with an empty list.
- **AC-06** (UC-01 A2, BR-03) — *Given* "jdoe" has an attendance on 2026-03-10 *When* an admin consults "jdoe" from 2026-03-10 to 2026-03-10 *Then* that attendance is returned.
- **AC-07** (UC-01 A3, BR-04) — *Given* an admin *When* they consult a range of exactly 31 calendar days (2026-01-01 to 2026-01-31) *Then* the query is accepted.
- **AC-08** (UC-01 E1, BR-01) — *Given* an unidentified requester *When* they consult attendances by date range *Then* the request is rejected as not authenticated and nothing is returned.
- **AC-09** (UC-01 E2, BR-01) — *Given* an identified user without the `admin` role *When* they consult any employee's attendances by date range (including their own) *Then* the request is rejected as not authorized and nothing is returned.
- **AC-10** (UC-01 E3, BR-02) — *Given* an admin *When* they consult without a user name, without "from", without "to", or with a date that is not valid *Then* the request is rejected as invalid, identifying the wrong input.
- **AC-11** (UC-01 E4, BR-03) — *Given* an admin *When* they consult from 2026-03-10 to 2026-03-01 *Then* the request is rejected as invalid with a message that the start date cannot be later than the end date, and attendances are not looked up.
- **AC-12** (UC-01 E5, BR-04) — *Given* an admin *When* they consult from 2026-01-01 to 2026-02-01 (32 calendar days) *Then* the request is rejected as invalid with a message that the range cannot exceed 31 days, and attendances are not looked up.
- **AC-13** (UC-01 E6) — *Given* the attendance history is unavailable *When* an admin makes a valid query *Then* the query fails with a system error, the failure is recorded, and no partial data is returned.

### Flow coverage
| Flow | AC |
|------|----|
| UC-01 main | AC-01, AC-02, AC-03 |
| UC-01 A1 | AC-04, AC-05 |
| UC-01 A2 | AC-06 |
| UC-01 A3 | AC-07 |
| UC-01 E1 | AC-08 |
| UC-01 E2 | AC-09 |
| UC-01 E3 | AC-10 |
| UC-01 E4 | AC-11 |
| UC-01 E5 | AC-12 |
| UC-01 E6 | AC-13 |

## 8. Out of scope
- Pagination.
- Employees consulting their own attendances by range (admins only for now).
- Consulting several or all employees at once; totals, reports or exports.
- Restricting the existing full-history query, which today any authenticated user can use for any employee. Recommended as a separate feature.
- Checking against the employee registry whether the employee exists (BR-06 treats unknown employees as having no attendances).

## 9. Glossary (new terms only)
- **Date range** — a pair of calendar dates "from" and "to", both inclusive, with "from" ≤ "to" and covering at most 31 calendar days.

## 10. Assumptions & open questions
- [x] Q-01 — How are the 31 days counted? (answer: calendar days, both ends inclusive — BR-04)
- [x] Q-02 — Unknown employee: empty list or not found? (answer: empty list — BR-06)
- [x] Q-03 — Are dates taken as calendar days as stored in the attendance history, with no time-zone conversion? (answer: yes; time zones are not handled in this feature — BR-05)
- [x] Q-04 — Ordering? (answer: most recent first — BR-07)
