# [002] End-to-end regression tests — Business Specification

> Status: Approved (2026-10-30), scope amended and approved 2026-11-03 (§11)
> Owning service: solution-wide (root `features/`)
> Author / requested by: harlinacero

## 1. Summary
An automated regression suite that starts the whole system in a throwaway environment, runs every operation clients can call and the complete attendance flow with disposable test data, reports the result, and then destroys the environment and all its data. Developers run it locally, the shipping workflow runs it before a feature is pushed, and it runs automatically on every pull request.

## 2. Business goal / problem
Today only unit tests exist. Each one checks a single piece in isolation, with every collaborator faked. Nothing checks that the services still work **together**: that an employee created by an admin really gets a login, that marking attendance really reaches the history, that the single entry point still routes and protects every operation. A feature can pass all unit tests and still break a flow that worked before, and nobody notices until someone uses the system by hand.

The suite turns "what worked before the new feature" into an automatic check. Each feature is shipped only if the existing behavior still holds.

## 3. Actors
| Actor | Description | Role / permission |
|-------|-------------|-------------------|
| Developer | Runs the suite on their machine, before or while working on a feature | Local machine with the container tooling |
| Shipping workflow | The team's "close the feature" routine (commit, push, PR) | Runs the suite as a gate before pushing |
| Pull request check | Automatic run on every pull request to the main branch | Repository automation; no secrets for paid services |
| Seeded admin | The administrator account that exists in every fresh environment | `admin` |
| Test employee | An employee the suite creates during the run, with a regular user account | regular user |

## 4. User stories
- **US-01** — As a developer, I want to run one command that checks the whole system end to end and leaves nothing behind, so that I know my feature didn't break existing behavior.
- **US-02** — As a developer, I want the suite to check the complete attendance flow (employee created → account usable → attendance marked → history updated), so that cross-service behavior is protected and not only single pieces.
- **US-03** — As a developer, I want the suite to check who can and cannot use each operation, so that a feature can't silently open or close access.
- **US-04** — As the team, we want the suite to run automatically on every pull request, so that a regression can't be merged unnoticed.
- **US-05** — As a developer, I want the shipping workflow to run the suite before pushing, so that I don't open a pull request I already know is broken.

## 5. Business rules
- **BR-01 — Isolation.** A run uses its own environment. It can run while the developer's own environment is up, and it never reads, changes or deletes the developer's data.
- **BR-02 — Disposable data.** Everything a run creates (employees, accounts, attendance records, messages) is destroyed when the run ends, whether checks passed, failed, or the run was interrupted. Because the attendance history is append-only, data is removed by destroying the environment, never by adding delete operations to the system.
- **BR-03 — Unique test data.** Each run creates its test employees with identifiers unique to that run, so runs never clash with each other or with seeded data.
- **BR-04 — Real entry point.** Checks call the system the way a client does, through the single entry point. Direct service calls are not used to check behavior.
- **BR-05 — Current behavior is the baseline.** The suite protects today's behavior. When today's behavior looks wrong, the suite still checks it as it is, and the doubt is recorded as an observation (§10) for a separate feature.
- **BR-06 — Unit suite stays independent.** Running the regular test suite doesn't need the environment and doesn't run the end-to-end checks. Nothing in the regular suite fails because the environment is down.
- **BR-07 — Bounded waiting.** Some effects arrive with a delay, because services talk to each other through messages. The suite waits for them up to a time limit and checks again meanwhile, instead of pausing for a fixed time. If the time limit passes, the check fails and says what never arrived.
- **BR-08 — Paid assistant is opt-in.** The AI assistant's real answer costs money and isn't deterministic. It's checked only when the developer asks for it and provides the access key, and never in the pull request check. Rejecting an unauthenticated request to the assistant costs nothing, so it's always checked.
- **BR-09 — Full catalog.** Every operation exposed through the entry point has at least one check. A feature that adds or changes an operation also adds or updates its checks.
- **BR-10 — Clear report.** A run ends with a pass/fail result and the list of failed checks with the reason. A failed check, or an environment that never became ready, makes the whole run fail.

## 6. Use cases

### UC-01 — Run the regression suite (US-01)
- **Actor:** Developer (or the shipping workflow / pull request check, see UC-04 and UC-05)
- **Preconditions:** container tooling installed and running; source code of the branch to check.
- **Main flow:**
  1. The developer starts the suite with one command.
  2. The system builds and starts a fresh, isolated environment with all services.
  3. The system waits until every service and the entry point are ready.
  4. The system runs all checks (UC-02, UC-03).
  5. The system reports the result: passed/failed counts and the failed checks.
  6. The system destroys the environment and all its data.
- **Alternative flows:**
  - **A1** (at step 2) — The developer's own environment is running → the isolated environment starts alongside it. The developer's environment and data are untouched after the run (BR-01).
  - **A2** (at step 1) — The developer asks for the assistant check and provides the access key → the real assistant check also runs (UC-03 A1).
  - **A3** (at step 1) — The developer asks to keep the environment after the run, to investigate a failure → step 6 is skipped and the system says how to destroy the environment later.
- **Exception flows:**
  - **E1** (at step 2–3) — A service doesn't start or isn't ready within the time limit → no checks run; the run fails and names the services that weren't ready; the environment is destroyed.
  - **E2** (at step 4) — One or more checks fail → the remaining checks still run; the run fails and lists the failed checks with the reason; the environment is destroyed.
  - **E3** (at any step) — The run is interrupted (cancelled, crashed) → leftovers of the interrupted run are destroyed at the latest when the next run starts. They never accumulate and are never mistaken for the developer's environment.
  - **E4** (independent) — The developer runs the regular test suite while no environment exists → the end-to-end checks aren't run, and the regular suite passes or fails exactly as before (BR-06).
  - **E5** (at step 1) — The assistant check is requested without the access key → that check is reported as skipped, with the reason. It isn't reported as passed, and it doesn't fail the run.
- **Postconditions:** on success and on failure, no environment, data or messages of the run remain (except with A3); the developer's environment is unchanged.

### UC-02 — Check the complete attendance flow (US-02)
- **Actor:** the suite, acting as the seeded admin and as a test employee
- **Preconditions:** environment ready (UC-01 step 3); the seeded admin exists.
- **Main flow:**
  1. The admin signs in and gets access.
  2. The admin creates a test employee with a unique user name, a password and the regular role.
  3. The test employee appears in the admin's employee list.
  4. The test employee's account becomes usable: the employee can sign in with the password given in step 2 (it arrives with a delay, BR-07).
  5. The new account appears in the admin's account list, with the regular role.
  6. The test employee marks themselves as present, with notes.
  7. A new record appears in the test employee's attendance history (with a delay, BR-07): present, today's date, the employee's full name, and the notes followed by the number of attendances the employee had before.
  8. The test employee marks themselves as absent.
  9. The history now has two records, the first one unchanged (append-only).
  10. The admin queries the test employee's attendance for a date range that includes today, and gets both records.
  11. The test employee adds an attendance record directly to their own history, and it appears in the history.
- **Alternative flows:**
  - **A1** (at step 10) — The date range doesn't include today → no records are returned.
  - **A2** (at step 2) — The admin creates an account directly, without an employee → that account can sign in with its role.
- **Exception flows:**
  - **E1** (at step 4) — The account never becomes usable within the time limit → the check fails, saying the account wasn't created from the new employee.
  - **E2** (at step 7 or 9) — The history record never appears within the time limit → the check fails, saying the attendance change didn't reach the history.
  - **E3** (at step 1) — The admin can't sign in → every check that needs the admin fails with that reason. They aren't reported as passed.
- **Postconditions:** the test employee, account and records exist only inside the run's environment and are destroyed with it (BR-02).

### UC-03 — Check access to every operation (US-03)
- **Actor:** the suite, acting as anonymous caller, test employee and admin
- **Preconditions:** environment ready; a test employee with a usable account (UC-02 step 4).
- **Main flow:**
  1. For every operation that requires identification, a call without identification is rejected as not authenticated.
  2. For every admin-only operation (account list, account creation, employee list, employee creation, attendance by date range), a call by the test employee is rejected as not authorized.
  3. For every operation open to any signed-in user (mark attendance, read history, add attendance record, assistant), a call by the test employee is accepted (the assistant only with A1).
  4. Signing in with a wrong password or an unknown user is rejected as invalid credentials.
  5. A date-range query whose start is after its end, or that spans more than 31 days, is rejected as invalid.
- **Alternative flows:**
  - **A1** (at step 3) — The assistant check is enabled (UC-01 A2) → the test employee asks the assistant a question and gets an answer. Only the fact that an answer arrives is checked, not its content.
  - **A2** (at step 3) — A signed-in employee reads another user's history → today this is accepted (observation OBS-01). The suite checks it as it is (BR-05).
- **Exception flows:**
  - **E1** (at step 1–3) — An operation answers differently than expected (opened, closed, or missing from the entry point) → the check fails, naming the operation, the caller and the actual outcome.
- **Postconditions:** as UC-02.

### UC-04 — Run on every pull request (US-04)
- **Actor:** Pull request check
- **Preconditions:** a pull request to the main branch is opened or updated.
- **Main flow:**
  1. The repository automation runs the suite (UC-01) on the pull request's code, without the assistant check.
  2. The result is shown on the pull request as passed or failed, with the failed checks available.
- **Exception flows:**
  - **E1** (at step 1) — Any check fails, or the environment never becomes ready → the pull request shows the check as failed.
- **Postconditions:** nothing persists between pull request runs.

### UC-05 — Gate the shipping workflow (US-05)
- **Actor:** Shipping workflow
- **Preconditions:** the developer closes a feature with the shipping workflow.
- **Main flow:**
  1. After the regular test suite passes, the workflow runs the regression suite (UC-01).
  2. If it passes, the workflow continues to commit, push and pull request, and includes the regression result in the pull request description.
- **Exception flows:**
  - **E1** (at step 1) — The regression suite fails → the workflow stops before committing, shows the failed checks, and asks how to proceed.
  - **E2** (at step 1) — The container tooling isn't available on the machine → the workflow says so and asks whether to continue without the regression run. It never treats a skipped run as a passed one.

## 7. Acceptance criteria
- **AC-01** (UC-01 main, BR-10) — *Given* a branch with no regressions, *When* the developer runs the suite with one command, *Then* an isolated environment is started, all checks run, a pass result with counts is reported, and the environment is destroyed afterwards.
- **AC-02** (UC-01 main, BR-02) — *Given* a completed run, *When* the developer lists the environments and stored data on the machine, *Then* nothing of that run remains.
- **AC-03** (UC-01 A1, BR-01) — *Given* the developer's own environment running with its data, *When* the suite runs, *Then* both environments run at the same time, and afterwards the developer's environment is still running with the same data.
- **AC-04** (UC-01 A3) — *Given* the developer asks to keep the environment, *When* the run ends, *Then* the environment is still up and the system says how to destroy it.
- **AC-05** (UC-01 E1) — *Given* a service that can't start, *When* the suite runs, *Then* no checks run, the run fails naming that service, and the environment is destroyed.
- **AC-06** (UC-01 E2, BR-10) — *Given* a regression in one operation, *When* the suite runs, *Then* the other checks still run, the run fails, the report names the failed check with the reason, and the environment is destroyed.
- **AC-07** (UC-01 E3, BR-02) — *Given* leftovers of an interrupted run, *When* the next run starts, *Then* the leftovers are destroyed before the new environment starts, and the developer's environment is untouched.
- **AC-08** (UC-01 E4, BR-06) — *Given* no environment running, *When* the developer runs the regular test suite, *Then* no end-to-end check runs and the result is the same as before this feature.
- **AC-09** (UC-01 A2, UC-01 E5, UC-03 A1, BR-08) — *Given* the assistant check requested, *When* the access key is provided, *Then* a signed-in employee's question gets an answer. *When* it isn't provided, *Then* the check is reported as skipped with the reason and the run doesn't fail because of it. *When* it isn't requested, *Then* the assistant is never called.
- **AC-10** (UC-02 main 1–5, BR-03, BR-07) — *Given* a fresh environment, *When* the admin creates an employee with a unique user name, *Then* the employee appears in the employee list, and within the time limit the employee can sign in with their password and appears in the account list with the regular role.
- **AC-11** (UC-02 main 6–7) — *Given* a usable test employee with no records, *When* they mark themselves present with notes, *Then* within the time limit their history has one record: present, today, their full name, and the notes followed by the count of previous attendances (0).
- **AC-12** (UC-02 main 8–9) — *Given* the record of AC-11, *When* the employee marks themselves absent, *Then* the history has two records, and the first one is unchanged.
- **AC-13** (UC-02 main 10, UC-02 A1) — *Given* the two records, *When* the admin queries a range that includes today, *Then* both are returned. *When* the range doesn't include today, *Then* none are returned.
- **AC-14** (UC-02 main 11) — *Given* a signed-in test employee, *When* they add an attendance record directly, *Then* it appears in their own history.
- **AC-15** (UC-02 A2) — *Given* the admin, *When* they create an account directly with a unique user name and the regular role, *Then* that account can sign in.
- **AC-16** (UC-02 E1, UC-02 E2, BR-07) — *Given* an effect that never arrives, *When* the time limit passes, *Then* the check fails with a message saying which effect didn't arrive. It doesn't hang, and it doesn't pass.
- **AC-17** (UC-02 E3) — *Given* the admin can't sign in, *When* the suite runs, *Then* the checks that need the admin fail with that reason. None of them is reported as passed.
- **AC-18** (UC-03 main 1, BR-09) — *Given* each operation that requires identification (all except signing in), *When* it's called without identification, *Then* it's rejected as not authenticated.
- **AC-19** (UC-03 main 2) — *Given* the test employee, *When* they call each admin-only operation, *Then* each is rejected as not authorized.
- **AC-20** (UC-03 main 3) — *Given* the test employee, *When* they call each operation open to any signed-in user (except the assistant), *Then* each is accepted.
- **AC-21** (UC-03 main 4) — *Given* the seeded admin, *When* someone signs in with a wrong password, or with an unknown user name, *Then* sign-in is rejected as invalid credentials.
- **AC-22** (UC-03 main 5) — *Given* the admin, *When* they query a date range whose start is after its end, or that spans 32 days, *Then* the query is rejected as invalid.
- **AC-23** (UC-03 A2, BR-05) — *Given* two test employees, *When* one reads the other's history, *Then* it's accepted (today's behavior, OBS-01).
- **AC-24** (UC-03 E1, BR-09) — *Given* the list of operations exposed through the entry point, *When* the suite is reviewed, *Then* every operation has at least one check, and an operation without a check makes the suite fail.
- **AC-25** (UC-04 main, BR-08) — *Given* a pull request to the main branch, *When* it's opened or updated, *Then* the suite runs without the assistant check, and the pull request shows the result.
- **AC-26** (UC-04 E1) — *Given* a pull request that breaks a check, *When* the automation runs, *Then* the pull request shows the check as failed, and the failed check can be seen.
- **AC-27** (UC-05 main) — *Given* a feature being shipped with a green regular suite, *When* the regression suite passes, *Then* the workflow continues, and the pull request description includes the regression result.
- **AC-28** (UC-05 E1, E2) — *Given* a feature being shipped, *When* the regression suite fails or can't run, *Then* the workflow stops before committing and asks the developer. A skipped run is never reported as passed.

### Flow coverage
| Flow | AC |
|------|----|
| UC-01 main | AC-01, AC-02 |
| UC-01 A1 | AC-03 |
| UC-01 A2 | AC-09 |
| UC-01 A3 | AC-04 |
| UC-01 E1 | AC-05 |
| UC-01 E2 | AC-06 |
| UC-01 E3 | AC-07 |
| UC-01 E4 | AC-08 |
| UC-01 E5 | AC-09 |
| UC-02 main | AC-10, AC-11, AC-12, AC-13, AC-14 |
| UC-02 A1 | AC-13 |
| UC-02 A2 | AC-15 |
| UC-02 E1 | AC-16 |
| UC-02 E2 | AC-16 |
| UC-02 E3 | AC-17 |
| UC-03 main | AC-18, AC-19, AC-20, AC-21, AC-22 |
| UC-03 A1 | AC-09 |
| UC-03 A2 | AC-23 |
| UC-03 E1 | AC-18, AC-19, AC-20, AC-24 |
| UC-04 main | AC-25 |
| UC-04 E1 | AC-26 |
| UC-05 main | AC-27 |
| UC-05 E1 | AC-28 |
| UC-05 E2 | AC-28 |

## 8. Out of scope
- Changing any service's behavior, including the observations in §10, except the two startup defects in §11. They go to separate features.
- Delete or cleanup operations in the system itself. Cleanup is done only by destroying the environment (BR-02).
- Load, performance and resilience testing (e.g. a broker that goes down mid-flow).
- Checking the content or quality of the assistant's answers.
- Running against shared or remote environments: only the throwaway environment created by the run.
- Cases whose current behavior isn't defined (e.g. creating an employee whose user name already exists), see Q-01.

## 9. Glossary (new terms only)
- **Regression suite / end-to-end check** — an automated check that calls the running system through its entry point, with all services and stores real, as opposed to a unit test, where collaborators are faked.
- **Run environment** — the throwaway, isolated copy of the whole system created for one run and destroyed after it.
- **Test employee** — an employee created by a run, with an identifier unique to that run, that exists only in the run environment.
- **Time limit** — the maximum time a check waits for a delayed effect (an account or a history record) before failing.

## 10. Assumptions & open questions
- **OBS-01** — Any signed-in user can read any other user's attendance history: the history read takes the user name as a parameter and doesn't compare it with the caller. The suite checks this as current behavior (AC-23). It should be reviewed in a separate feature.
- **OBS-02** — When an employee marks attendance, the notes saved in the history get the number of previous attendances appended (e.g. `notes [0] Asistencias`). The suite checks this as current behavior (AC-11).
- **OBS-03** — The seeded admin's password is a fixed, known value in every fresh environment. That's acceptable for a throwaway environment; the suite uses it.
- [ ] **Q-01** — What should happen when an admin creates an employee whose user name already exists? It isn't defined today, so it's out of scope. (proposal: a separate feature defines it, then this suite adds the check)
- [x] **Q-02** — Time limit for delayed effects: assumed 60 seconds per effect, and 5 minutes for the environment to become ready (Cassandra and SQL Server start slowly). (answer: accepted as assumed, 2026-10-30)
- [x] **Q-03** — A full run (build, start, checks, teardown) is expected to take several minutes, mostly building and starting containers. Is that acceptable for the pull request check and the shipping workflow? (answer: accepted, 2026-10-30)
- [x] **Q-04** — The shipping workflow is a skill installed on the developer's machine, not part of the repository. Changing it (UC-05) means editing that skill outside the repository. (answer: accepted; the skill is edited outside the repo, 2026-10-30)

## 11. Scope amendment: startup defects found by the first run (2026-11-03)
The first real run showed that a fresh environment can't work, for two reasons older than this feature. Without fixing them the suite can never pass, so the requester decided to fix them here, including the underlying cause of the second one.
- **Defect 1 — A fresh installation can't start.** Preparing the accounts store on empty storage fails, so the store restarts forever and nobody can sign in. Environments with existing data hide it.
- **Defect 2 — A service can silently stop receiving messages.** If a service starts before the message broker accepts connections, it never receives messages and reports nothing. Attendance changes wait in the queue and don't reach the history until someone restarts the service.

### UC-06 — Start the system from nothing
- **Actor:** Developer, or anyone installing the system
- **Preconditions:** no stored data.
- **Main flow:**
  1. The system starts with empty storage.
  2. The accounts store is prepared, and the seeded admin exists.
  3. Each service that receives messages connects to the broker and starts receiving.
- **Alternative flows:**
  - **A1** (at step 3) — The broker isn't accepting connections yet → the service keeps trying until it can connect, without a restart, and then receives the messages that were waiting.
- **Exception flows:**
  - **E1** (at step 3) — A connection attempt fails → the failure is recorded in the service's log, with the attempt number.
  - **E2** (at step 3) — The service is told to stop while it's still trying → it stops trying and shuts down normally.

### Acceptance criteria
- **AC-29** (UC-06 main 1–2) — *Given* no stored data, *When* the system starts, *Then* the seeded admin can sign in.
- **AC-30** (UC-06 main 3, A1) — *Given* a service that receives messages and a broker that isn't accepting connections yet, *When* the broker becomes available, *Then* the service starts receiving without a restart, including the messages already waiting.
- **AC-31** (UC-06 E1) — *Given* a failed connection attempt, *When* it happens, *Then* it's recorded in the log with the attempt number, and the service keeps trying.
- **AC-32** (UC-06 E2) — *Given* a service still trying to connect, *When* it's told to stop, *Then* it stops trying and shuts down without an error.

| Flow | AC |
|------|----|
| UC-06 main | AC-29, AC-30 |
| UC-06 A1 | AC-30 |
| UC-06 E1 | AC-31 |
| UC-06 E2 | AC-32 |
