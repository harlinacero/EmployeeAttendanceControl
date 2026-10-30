# [001] API gateway (single entry point) — Business Specification

> Status: Approved (2026-10-22)
> Owning service: `ms.gateway` (new)
> Author / requested by: harlinacero

## 1. Summary
Client applications get a single entry point to the system. Through it they can use every existing operation of the user, employee and attendance services, with the same paths and the same results as today. Unidentified requests and requests without the required role are rejected at that entry point, before they reach any service.

## 2. Business goal / problem
Today each service is reached at its own address. A client has to know three addresses and which operation lives where. Every new client repeats that knowledge, and moving or splitting a service breaks all of them.

A single entry point means clients know one address. It also gives one place where unidentified or under-privileged requests are stopped early, while each service keeps its own checks. The services are not changed, and direct access to them remains available, for example for debugging.

## 3. Actors
| Actor | Description | Role / permission |
|-------|-------------|-------------------|
| Anonymous client | Someone not identified yet; can only log in | none |
| Employee | Identified regular user | regular (non-admin) |
| Admin | Identified user who manages employees, users and attendance | `admin` |

## 4. User stories
- **US-01** — As any client, I want to reach every existing operation through one entry point, with the same paths, inputs and results as today, so that I only need to know one address.
- **US-02** — As the system owner, I want unidentified or under-privileged requests to be rejected at the entry point, so that services only receive requests that are allowed to reach them.

## 5. Business rules
- **BR-01** — The entry point exposes exactly the existing operations, with the same paths, inputs and outputs as when calling each service directly:

  | # | Operation | Service | Who may use it |
  |---|-----------|---------|----------------|
  | 1 | Log in | users | anyone |
  | 2 | List user accounts | users | admin |
  | 3 | Create a user account | users | admin |
  | 4 | List employees | employees | admin |
  | 5 | Create an employee | employees | admin |
  | 6 | Mark own attendance state (present/absent with notes) | employees | any identified user |
  | 7 | Consult an employee's full attendance history | attendances | any identified user |
  | 8 | Consult an employee's attendances by date range | attendances | admin |
  | 9 | Record an attendance for oneself | attendances | any identified user |

- **BR-02** — Logging in is the only operation open to unidentified clients.
- **BR-03** — For any other operation, the entry point rejects the request as not authenticated if the requester is not identified, or their identification is invalid or expired. The request is not forwarded.
- **BR-04** — For admin-only operations (2, 3, 4, 5 and 8), the entry point rejects identified non-admins as not authorized. The request is not forwarded.
- **BR-05** — Accepted requests are forwarded with the requester's identity, so each service applies its own rules exactly as today (e.g. "own" attendance, input validation). The outcome the client sees is the service's outcome, unchanged.
- **BR-06** — A request for an operation not in the catalog of BR-01 is rejected as not found.
- **BR-07** — If the service behind an operation is unavailable, that request fails with a "service unavailable" error. Operations of the other services keep working.
- **BR-08** — Services remain reachable directly, at their current addresses, with their current behavior.
- **BR-09** — The calls services make to each other (employees consulting attendance history) keep going directly between services, not through the entry point.
- **BR-10** — The entry point stores no business data and does not change what is sent or returned.
- **BR-11** — Rejections made by the entry point (BR-03, BR-04) carry only the outcome (not authenticated / not authorized), with no detailed message, as the services do today.
- **BR-12** — The entry point uses the same identification the users service issues today on log in. There is no new log-in mechanism.
- **BR-13** — To make testing easier, the entry point can run in two ways, and reaches every service in both:
  - **(a)** inside the containerized environment, together with the services;
  - **(b)** on a developer machine outside that environment, reaching the services at the addresses they publish to the machine.

  Choosing a mode needs no code change, only configuration.

## 6. Use cases

### UC-01 — Use an existing operation through the entry point (US-01)
- **Actor:** Anonymous client, Employee or Admin
- **Preconditions:** The entry point and the service behind the operation are running. For any operation other than log in, the actor holds a valid identification obtained by logging in.
- **Main flow:**
  1. The actor sends a request for one of the operations of BR-01 to the entry point, with the same path and inputs they would use against the service.
  2. The entry point recognizes the operation and the service that handles it (BR-01).
  3. The entry point checks that the actor may use it (UC-02).
  4. The entry point forwards the request to the service, together with the actor's identity (BR-05).
  5. The service processes it under its own rules.
  6. The entry point returns the service's answer to the actor unchanged (BR-10).
- **Alternative flows:**
  - **A1** (at step 1) — The operation is log in → no identification is needed (BR-02); the flow continues at step 4 and returns the identification issued by the users service.
  - **A2** (at step 5) — The service rejects the request under its own rules (invalid input, wrong credentials, not found…) → that rejection reaches the actor unchanged, exactly as with direct access (BR-05).
  - **A3** (at step 1) — The actor calls the service directly instead of the entry point → it behaves as today (BR-08).
- **Exception flows:**
  - **E1** (at step 2) — The path does not match any operation of BR-01 → rejected as not found; nothing is forwarded (BR-06).
  - **E2** (at step 4) — The service behind the operation is unavailable → the request fails with a "service unavailable" error; requests to other services are unaffected (BR-07).
- **Postconditions:** On success, the actor gets the same result as calling the service directly. The entry point stores nothing in any outcome.

### UC-02 — Reject requests that may not reach a service (US-02)
- **Actor:** Anonymous client, Employee or Admin
- **Preconditions:** The actor sends a request for an operation other than log in (UC-01 step 3).
- **Main flow:**
  1. The entry point checks that the request carries a valid, unexpired identification (BR-03).
  2. If the operation is admin-only, the entry point checks that the identified actor has the `admin` role (BR-04).
  3. Both checks pass → the request continues to UC-01 step 4.
- **Alternative flows:**
  - **A1** (at step 2) — The operation is open to any identified user (6, 7, 9) → the role check is skipped; the flow continues at step 3.
- **Exception flows:**
  - **E1** (at step 1) — No identification → rejected as not authenticated; nothing is forwarded.
  - **E2** (at step 1) — Identification invalid (tampered or not issued by the system) or expired → rejected as not authenticated; nothing is forwarded.
  - **E3** (at step 2) — Identified, but not admin, for an admin-only operation → rejected as not authorized; nothing is forwarded.
- **Postconditions:** A rejected request never reaches a service. An accepted request continues as in UC-01.

## 7. Acceptance criteria
Each criterion is testable and will become at least one test.

- **AC-01** (UC-01 main, BR-01) — *Given* the entry point is running *When* the system is inspected *Then* the operations it exposes are exactly the 9 of BR-01, each mapped to the right service with the same path and method, and no others.
- **AC-02** (UC-01 main, BR-05, BR-10) — *Given* an admin with a valid identification *When* they list employees through the entry point *Then* they get the same list as calling the employees service directly.
- **AC-03** (UC-01 main, UC-02 A1, BR-05) — *Given* an employee "jdoe" with a valid identification *When* they record an attendance through the entry point *Then* the record is saved for "jdoe", which shows that the identity reached the service.
- **AC-04** (UC-01 main, BR-05) — *Given* an admin *When* they consult "jdoe"'s attendances by date range through the entry point, with the date filter in the request *Then* they get the same result as calling the attendance service directly, which shows that query inputs are forwarded.
- **AC-05** (UC-01 A1, BR-02) — *Given* an anonymous client with valid credentials *When* they log in through the entry point *Then* they receive an identification, and that identification is accepted by the entry point for other operations.
- **AC-06** (UC-01 A2, BR-05) — *Given* an anonymous client with wrong credentials *When* they log in through the entry point *Then* they get the same "invalid user name or password" rejection the users service gives.
- **AC-07** (UC-01 A2, BR-05) — *Given* an admin *When* they consult attendances by date range through the entry point with a range over 31 days *Then* they get the same "invalid" rejection and message as from the attendance service.
- **AC-08** (UC-01 A3, BR-08) — *Given* the entry point is running *When* a client calls any service directly at its current address *Then* it behaves as today.
- **AC-09** (UC-01 E1, BR-06) — *Given* any client *When* they request a path that is not one of the 9 operations (e.g. an unknown path, or a known path with a method not in the catalog) *Then* it is rejected as not found.
- **AC-10** (UC-01 E2, BR-07) — *Given* the attendance service is down *When* a client calls an attendance operation through the entry point *Then* the request fails with a "service unavailable" error, *And* listing employees through the entry point still works.
- **AC-11** (UC-02 E1, BR-03) — *Given* an anonymous client *When* they call any operation other than log in through the entry point *Then* it is rejected as not authenticated and the service never receives it.
- **AC-12** (UC-02 E2, BR-03) — *Given* a client with an expired identification, or one not issued by the system *When* they call a protected operation through the entry point *Then* it is rejected as not authenticated and the service never receives it.
- **AC-13** (UC-02 E3, BR-04) — *Given* an employee (non-admin) with a valid identification *When* they call each admin-only operation (2, 3, 4, 5, 8) through the entry point *Then* each is rejected as not authorized and the service never receives it.
- **AC-14** (UC-02 main, UC-02 A1, BR-04) — *Given* an employee with a valid identification *When* they call each operation open to any identified user (6, 7, 9) through the entry point *Then* each is forwarded and answered by the service.
- **AC-15** (BR-09) — *Given* the entry point is running *When* an employee marks their attendance state *Then* the employees service still consults the attendance history directly, not through the entry point.
- **AC-16** (BR-13a) — *Given* the entry point runs inside the containerized environment with the services *When* a client logs in and lists employees as admin through it *Then* both succeed.
- **AC-17** (BR-13b) — *Given* the services run in the containerized environment and the entry point runs on the developer machine outside it *When* a client logs in and lists employees as admin through it *Then* both succeed.
- **AC-18** (BR-11) — *Given* the entry point rejects a request as not authenticated or not authorized *When* the client reads the answer *Then* it carries only that outcome, with no detailed message.

### Flow coverage
| Flow | AC |
|------|----|
| UC-01 main | AC-01, AC-02, AC-03, AC-04 |
| UC-01 A1 | AC-05 |
| UC-01 A2 | AC-06, AC-07 |
| UC-01 A3 | AC-08 |
| UC-01 E1 | AC-09 |
| UC-01 E2 | AC-10 |
| UC-02 main | AC-14 |
| UC-02 A1 | AC-03, AC-14 |
| UC-02 E1 | AC-11 |
| UC-02 E2 | AC-12 |
| UC-02 E3 | AC-13 |
| BR-09 (internal calls) | AC-15 |
| BR-11 (rejection content) | AC-18 |
| BR-13 (run modes) | AC-16, AC-17 |

## 8. Out of scope
- Changing any service's operations, rules or answers.
- Hiding the services' direct addresses (BR-08; they remain reachable).
- Routing service-to-service calls through the entry point (BR-09).
- Rate limiting, caching, response aggregation, load balancing across several copies of a service, request retries.
- A unified API documentation page for all services at the entry point.
- Changing who may use each operation: the catalog in BR-01 mirrors today's permissions.

## 9. Glossary (new terms only)
- **Entry point (API gateway)** — the single address through which clients reach every operation of the system. It forwards each request to the service that handles it, after checking identity and role.
- **Operation catalog** — the fixed list of operations in BR-01 that the entry point exposes.

## 10. Assumptions & open questions
- [x] Q-01 — Where does the gateway live? (answer: new `ms.gateway` service folder, following the `ms.*` convention)
- [x] Q-02 — Keep direct access to services? (answer: yes, both — BR-08)
- [x] Q-03 — Who checks identity and role? (answer: both the entry point and the services — BR-03, BR-04, BR-05)
- [x] Q-04 — Public paths? (answer: same paths as today — BR-01)
- [x] Q-05 — Same identification as the users service issues today? (answer: yes, no new log-in — BR-12)
- [x] Q-06 — Rejections at the entry point carry only the outcome? (answer: yes, as the services do today — BR-11)
- [x] Q-07 — Where does the entry point run? (answer: both inside the containerized environment and on a developer machine outside it, selected by configuration — BR-13)
