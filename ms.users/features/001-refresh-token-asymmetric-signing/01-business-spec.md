# [001] Session renewal, exclusive credential issuing and password protection — Business Specification

> Status: Approved (2026-11-04)
> Owning service: `ms.users`
> Author / requested by: harlinacero

## 1. Summary
People stay signed in through a session they can renew without typing their password again, and can end it. Only the authentication service can issue access credentials; every other service can check them but can't create them. Passwords stop being readable anywhere: in storage, in answers, in logs and in the message queue.

## 2. Business goal / problem
- **A long-lived credential is a long-lived risk.** Today, signing in gives one credential that works for 4 hours. It can't be withdrawn, so a stolen one works until it expires. Shortening it would force people to type their password more often.
- **Every service can issue credentials.** The gateway and all the services hold the same secret that creates credentials. Whoever obtains it from any one of them can impersonate anyone, including an admin.
- **Passwords are exposed in four places.** They're stored in plain text, any admin can read everyone's in the list of accounts, they're written to the log when an employee is created, and they travel in plain text through the message queue.

This feature separates a short-lived access credential from a session that can be renewed and ended, detects when a renewal credential has been stolen, leaves the power to issue credentials in one service, and removes those four exposures.

## 3. Actors
| Actor | Description | Role / permission |
|-------|-------------|-------------------|
| Person | Anyone with an account, through any client | `admin` or regular user |
| Authentication service | The only one that issues credentials and manages sessions | — |
| Verifying service | The gateway and the employee, attendance and assistant services | Checks credentials, can't issue them |
| Administrator | Creates employees and accounts | `admin` |
| Attacker | Someone holding a stolen or forged credential, or a copy of the stored data | — |

## 4. User stories
- **US-01** — As a person, I want to sign in and get a session, so that I can work and stay signed in on each of my devices.
- **US-02** — As a person, I want my session to renew without my password, so that a short-lived access credential doesn't interrupt my work.
- **US-03** — As a person, I want a stolen renewal credential to stop working, so that nobody can keep my session alive behind my back.
- **US-04** — As a person, I want to sign out of the session I'm using, so that it can't be renewed any more.
- **US-05** — As the system owner, I want only the authentication service to be able to issue credentials, so that compromising another service doesn't allow impersonation.
- **US-06** — As a person, I want my password to be unreadable by anyone, including administrators and whoever obtains the data, so that it can't be stolen and reused.
- **US-07** — As the system owner, I want the passwords already stored to be protected as soon as this goes live, so that no readable password remains and nobody has to change theirs.
- **US-08** — As an administrator, I want to create an employee and their account in one step and know the result at once, so that I don't discover later that the person can't sign in.

## 5. Business rules
- **BR-01** — Signing in starts a new session and gives an access credential and a renewal credential.
- **BR-02** — An access credential lasts **15 minutes** (today: 4 hours). See Q-01.
- **BR-03** — A session ends after **7 days without renewal**, and in any case **30 days after sign-in**. See Q-01.
- **BR-04** — A person can have several sessions at once, with no limit. Each one is independent: renewing, signing out of or revoking one doesn't affect the others.
- **BR-05** — A renewal credential works **once**. Using it gives a new access credential and a new renewal credential, and it becomes spent.
- **BR-06** — Using a spent renewal credential counts as theft: it's rejected and **that whole session is revoked**, including its newest renewal credential. There's no tolerance window. The event is logged without the credential's value.
- **BR-07** — A new access credential carries the account's **current** role, not the one at sign-in.
- **BR-08** — Signing out ends only the current session. An access credential already issued keeps working until it expires, at most 15 minutes.
- **BR-09** — A copy of the stored session data can't be used to renew a session.
- **BR-10** — Only the authentication service can issue access credentials. No other service holds anything that lets it do so.
- **BR-11** — An access credential can't be used to renew, and a renewal credential can't be used to call an operation.
- **BR-12** — When the authentication service can't confirm the outcome, it says it's temporarily unavailable. It doesn't say the credentials are wrong, and it doesn't end or spend anything.
- **BR-13** — Every sign-in failure gets the same answer, so it doesn't reveal whether an account exists.
- **BR-14** — A password is stored only in a protected form that can't be turned back into the password. The protection is deliberately slow to compute, and two accounts with the same password have different stored values.
- **BR-15** — The stored value can't be used as a password.
- **BR-16** — No operation returns a password or its stored value, and neither is written to a log.
- **BR-17** — Passwords don't travel through the message queue.
- **BR-18** — When this goes live, every readable password already stored is protected. People keep the password they have, including the initial administrator.
- **BR-19** — Creating an employee creates their account in the same operation, so they can sign in as soon as it's confirmed. If the account can't be created, the employee isn't created either.

## 6. Use cases

### UC-01 — Sign in (US-01)
- **Actor:** Person
- **Preconditions:** the person has an account.
- **Main flow:**
  1. The person provides their user name and password.
  2. The system checks them, and starts a new session.
  3. The system gives an access credential and a renewal credential.
- **Alternative flows:**
  - **A1** (at step 2) — the person already has a session on another device → a new, independent session starts, and the other one keeps working.
- **Exception flows:**
  - **E1** (at step 2) — wrong password, unknown user or missing data → rejected as not authorized, with the same answer in every case. No session starts.
  - **E2** (at step 2) — the system can't reach its data → reported as temporarily unavailable. No session starts. (Today this is reported as a wrong password.)
- **Postconditions:** on success, a new active session. On failure, nothing changes.

### UC-02 — Renew a session (US-02, US-03)
- **Actor:** Person (through their client)
- **Preconditions:** the person holds a renewal credential.
- **Main flow:**
  1. The client presents the renewal credential.
  2. The system checks that it's the session's current one, and that the session is active.
  3. The system marks it as spent, and gives a new access credential and a new renewal credential.
- **Alternative flows:**
  - **A1** (at step 1) — the previous access credential hasn't expired yet → renewal works the same way.
  - **A2** (at step 3) — the account's role changed after sign-in → the new access credential carries the current role.
- **Exception flows:**
  - **E1** (at step 2) — unknown, malformed or missing credential → rejected as not authorized. Nothing changes.
  - **E2** (at step 2) — the session has expired (BR-03) → rejected as not authorized. The person must sign in.
  - **E3** (at step 2) — the credential is spent → rejected as not authorized, the session is revoked and the event is logged. The latest credential of that session stops working too. The person's other sessions are unaffected.
  - **E4** (at step 2) — the session was already ended or revoked → rejected as not authorized.
  - **E5** (at step 2) — the same credential arrives twice at the same moment → one succeeds at most. The other counts as reuse (E3), so the session is revoked.
  - **E6** (at step 2) — the account no longer exists → rejected as not authorized, and the session ends.
  - **E7** (at step 2 or 3) — the system can't reach its data → reported as temporarily unavailable. The credential isn't spent, so the client can retry with it.
- **Postconditions:** on success, the session is active and only the newest renewal credential works. On E3, the session is revoked for good.

### UC-03 — Sign out (US-04)
- **Actor:** Person
- **Preconditions:** the person holds a renewal credential.
- **Main flow:**
  1. The person asks to sign out, presenting the renewal credential.
  2. The system ends that session, and confirms it.
- **Alternative flows:**
  - **A1** (at step 2) — the session was already ended, or the credential is unknown → it's confirmed the same way, and nothing changes.
  - **A2** (at step 2) — the credential is spent → that session ends, and it's confirmed.
- **Exception flows:**
  - **E1** (at step 1) — no credential provided → rejected as invalid.
  - **E2** (at step 2) — the system can't reach its data → reported as temporarily unavailable. The session stays active, so the person knows it didn't end.
- **Postconditions:** the session can't be renewed. Other sessions are unaffected. The access credential works until it expires (BR-08).

### UC-04 — Check credentials without being able to issue them (US-05)
- **Actor:** Verifying service
- **Preconditions:** a client calls a protected operation.
- **Main flow:**
  1. The client presents an access credential, through the gateway or directly to a service.
  2. The verifying service confirms it was issued by the authentication service and hasn't expired, using only public information from that service.
  3. The operation continues, with the permissions it has today.
- **Alternative flows:**
  - **A1** (at step 2) — the authentication service is down, and the verifying service already has its public information → credentials keep being accepted until they expire. Only sign-in, renewal and sign-out are affected.
  - **A2** (at step 2) — the authentication service changed its signing identity → the verifying services accept the new credentials without being restarted or reconfigured. Credentials from the old identity are rejected, so clients renew or sign in.
- **Exception flows:**
  - **E1** (at step 2) — a credential forged with the old shared secret → rejected as not authorized, by the gateway and by every service.
  - **E2** (at step 2) — a credential issued by anyone else, or altered → rejected as not authorized.
  - **E3** (at step 2) — an expired credential → rejected as not authorized.
  - **E4** (at step 2) — a renewal credential used as an access credential → rejected as not authorized.
  - **E5** (at step 2) — the verifying service has no public information yet and the authentication service is down → the credential is rejected, and the service starts accepting credentials on its own once the authentication service returns, with no restart.
- **Postconditions:** no verifying service has what it would need to issue a credential.

### UC-05 — Protect passwords (US-06)
- **Actor:** Administrator, Person
- **Preconditions:** the administrator is signed in.
- **Main flow:**
  1. The administrator creates an account with a password.
  2. The system stores only the protected form.
  3. The person signs in with that password, and the system checks it against the protected form.
- **Alternative flows:**
  - **A1** (at step 2) — another account has the same password → the stored values are different.
  - **A2** (any time) — the administrator lists the accounts → the list shows user names and roles, with no password or stored value.
- **Exception flows:**
  - **E1** (at step 1) — no password provided → rejected as invalid, and no account is created.
  - **E2** (at step 3) — someone presents the stored value as the password → rejected as not authorized.
- **Postconditions:** the readable password exists nowhere in the system.

### UC-06 — Protect the passwords already stored (US-07)
- **Actor:** Authentication service, when it starts
- **Preconditions:** some accounts have a readable password.
- **Main flow:**
  1. The service finds the accounts whose password is still readable.
  2. It replaces each one with its protected form.
  3. It starts accepting requests, and those people sign in with the same password.
- **Alternative flows:**
  - **A1** (at step 1) — a password is already protected → it's left as it is, so starting again changes nothing.
  - **A2** (at step 1) — on an empty installation, the initial administrator is seeded already protected, so there's nothing to convert. (Amended 2026-11-07 with the technical spec, D-07: the seed can run after the conversion, which would leave it readable.)
- **Exception flows:**
  - **E1** (at step 1 or 2) — the data can't be reached, or an account can't be updated → the service doesn't start accepting requests, the failure is logged without any password, and the next start continues with what's left.
- **Postconditions:** no readable password is stored. Copies outside the current data, such as old logs and backups, aren't covered (see §8).

### UC-07 — Create an employee and their account (US-08)
- **Actor:** Administrator
- **Preconditions:** the administrator is signed in.
- **Main flow:**
  1. The administrator provides the employee's data and password.
  2. The employee service asks the authentication service directly to create the account.
  3. The system saves the employee, and confirms.
  4. The employee can sign in immediately.
- **Alternative flows:**
  - **A1** (at step 2) — an account with that user name already exists, for example from an earlier attempt that failed at step 3 → the employee is created and linked to it by user name, the existing password stays, and the event is logged. This matches today's behavior and lets the administrator retry.
- **Exception flows:**
  - **E1** (at step 2) — the authentication service is unavailable → reported as temporarily unavailable, and no employee or account is created.
  - **E2** (at step 3) — the employee can't be saved after the account was created → the failure is reported and the account stays. Retrying completes the employee through A1.
  - **E3** (at step 1) — the caller isn't an administrator → rejected as not authorized, as today.
  - **E4** (at step 1) — no password provided → rejected as invalid, and nothing is created.
- **Postconditions:** on success, the employee and their account both exist, and no password went through the message queue, a log or the employee's stored data.

## 7. Acceptance criteria
- **AC-01** (UC-01 main, BR-01, BR-02) — *Given* an account, *When* the person signs in with correct data, *Then* they get an access credential valid for 15 minutes and a renewal credential.
- **AC-02** (UC-01 A1, BR-04) — *Given* an active session, *When* the person signs in again, *Then* both sessions can be renewed independently.
- **AC-03** (UC-01 E1, BR-13) — *Given* a wrong password, an unknown user or missing data, *When* signing in, *Then* it's rejected as not authorized with the same answer, and no session starts.
- **AC-04** (UC-01 E2, BR-12) — *Given* the system can't reach its data, *When* signing in, *Then* it's reported as temporarily unavailable, not as wrong credentials.
- **AC-05** (UC-02 main, BR-05) — *Given* an active session, *When* the current renewal credential is presented, *Then* a working access credential and a new renewal credential are returned, and the new one works for the next renewal.
- **AC-06** (UC-02 A1) — *Given* an access credential that hasn't expired, *When* the session is renewed, *Then* it succeeds.
- **AC-07** (UC-02 A2, BR-07) — *Given* the account's role changed after sign-in, *When* the session is renewed, *Then* the new access credential carries the current role.
- **AC-08** (UC-02 E1) — *Given* an unknown, malformed or missing credential, *When* renewing, *Then* it's rejected as not authorized and no session changes.
- **AC-09** (UC-02 E2, BR-03) — *Given* a session with 7 days without renewal, or 30 days since sign-in, *When* renewing, *Then* it's rejected as not authorized, even if it was renewed recently.
- **AC-10** (UC-02 E3, BR-06) — *Given* a renewal credential that was already used, *When* it's presented again, *Then* it's rejected, the event is logged without the credential's value, and the newest credential of that session stops working too.
- **AC-11** (UC-02 E3, BR-04) — *Given* a session revoked for reuse, *When* the person's other session renews, *Then* it succeeds.
- **AC-12** (UC-02 E4) — *Given* an ended or revoked session, *When* any of its credentials is presented, *Then* it's rejected as not authorized.
- **AC-13** (UC-02 E5) — *Given* the same credential presented twice at the same moment, *When* both are processed, *Then* one succeeds at most, and the session ends up revoked.
- **AC-14** (UC-02 E6) — *Given* the account no longer exists, *When* renewing, *Then* it's rejected as not authorized and the session ends.
- **AC-15** (UC-02 E7, BR-12) — *Given* the system can't reach its data, *When* renewing, *Then* it's reported as temporarily unavailable, and the same credential works once it recovers.
- **AC-16** (BR-09) — *Given* the stored session data, *When* it's read, *Then* it doesn't contain any usable renewal credential.
- **AC-17** (UC-03 main, BR-04, BR-08) — *Given* two active sessions, *When* the person signs out of one, *Then* that one can't be renewed, the other can, and the access credential already issued works until it expires.
- **AC-18** (UC-03 A1) — *Given* an ended session or an unknown credential, *When* signing out, *Then* it's confirmed and nothing changes.
- **AC-19** (UC-03 A2) — *Given* a spent credential, *When* signing out with it, *Then* its session ends and it's confirmed.
- **AC-20** (UC-03 E1) — *Given* no credential, *When* signing out, *Then* it's rejected as invalid.
- **AC-21** (UC-03 E2, BR-12) — *Given* the system can't reach its data, *When* signing out, *Then* it's reported as temporarily unavailable and the session stays active.
- **AC-22** (UC-04 main) — *Given* a valid access credential, *When* it's presented through the gateway and directly to each service, *Then* every operation answers with the same permissions it has today.
- **AC-23** (UC-04 main, BR-10) — *Given* the configuration and code of the gateway and of the employee, attendance and assistant services, *When* they're inspected, *Then* none holds a signing secret or issues credentials.
- **AC-24** (UC-04 A1) — *Given* the authentication service is down and a verifying service already has its public information, *When* a valid access credential is presented, *Then* it's accepted.
- **AC-25** (UC-04 A2) — *Given* the signing identity changed, *When* a credential from the new identity is presented, *Then* it's accepted with no restart or reconfiguration of the verifying services, and one from the old identity is rejected.
- **AC-26** (UC-04 E1) — *Given* a credential forged with the old shared secret, *When* it's presented to the gateway and to each service, *Then* it's rejected as not authorized.
- **AC-27** (UC-04 E2) — *Given* a credential that is altered or issued by anyone else, *When* it's presented, *Then* it's rejected as not authorized.
- **AC-28** (UC-04 E3) — *Given* an expired access credential, *When* it's presented, *Then* it's rejected as not authorized.
- **AC-29** (UC-04 E4, BR-11) — *Given* a renewal credential presented to call an operation, or an access credential presented to renew, *When* processed, *Then* it's rejected as not authorized.
- **AC-30** (UC-04 E5) — *Given* a verifying service without public information and the authentication service down, *When* a credential is presented, *Then* it's rejected; and when the authentication service returns, valid credentials are accepted with no restart.

- **AC-31** (UC-05 main, BR-14) — *Given* a new account, *When* the stored data is read, *Then* the password isn't there, and the person can sign in with it.
- **AC-32** (UC-05 A1, BR-14) — *Given* two accounts with the same password, *When* the stored data is read, *Then* the stored values are different.
- **AC-33** (UC-05 A2, BR-16) — *Given* an administrator listing accounts, *When* the answer arrives, *Then* it has no password or stored value.
- **AC-34** (UC-05 E1) — *Given* an account request with no password, *When* it's processed, *Then* it's rejected as invalid and no account exists.
- **AC-35** (UC-05 E2, BR-15) — *Given* an account's stored value, *When* it's presented as the password, *Then* sign-in is rejected as not authorized.
- **AC-36** (UC-06 main, BR-18) — *Given* accounts with readable passwords, *When* the service starts, *Then* none remains readable, and each person signs in with the same password.
- **AC-37** (UC-06 A1) — *Given* protected passwords, *When* the service starts again, *Then* their stored values don't change.
- **AC-38** (UC-06 A2, BR-18) — *Given* an empty installation, *When* the system has started, *Then* the initial administrator's password is protected and they can sign in with it.
- **AC-39** (UC-06 E1, BR-16) — *Given* the data fails during conversion, *When* the service starts, *Then* it doesn't accept requests, the log has no password, and a later start finishes the conversion.
- **AC-40** (UC-07 main, BR-17, BR-19) — *Given* an administrator creating an employee, *When* it's confirmed, *Then* the employee can sign in at once, with no waiting, and nothing with the password was placed in the message queue.
- **AC-41** (UC-07 main, BR-16) — *Given* an employee was created, *When* the logs of the services involved are read, *Then* they don't contain the password.
- **AC-42** (UC-07 A1) — *Given* an account with that user name exists, *When* the employee is created, *Then* it succeeds, the existing password still works, the password provided doesn't, and the event is logged.
- **AC-43** (UC-07 E1, BR-19) — *Given* the authentication service is unavailable, *When* an employee is created, *Then* it's reported as temporarily unavailable, and no employee exists.
- **AC-44** (UC-07 E2) — *Given* the employee can't be saved after the account was created, *When* it's reported and the administrator retries once it recovers, *Then* the employee is created and can sign in.
- **AC-45** (UC-07 E3) — *Given* a caller who isn't an administrator, *When* creating an employee, *Then* it's rejected as not authorized, and no employee or account exists.
- **AC-46** (UC-07 E4) — *Given* an employee request with no password, *When* it's processed, *Then* it's rejected as invalid and nothing is created.

### Flow coverage
| Flow | AC |
|------|----|
| UC-01 main | AC-01 |
| UC-01 A1 | AC-02 |
| UC-01 E1 | AC-03 |
| UC-01 E2 | AC-04 |
| UC-02 main | AC-05, AC-16 |
| UC-02 A1 | AC-06 |
| UC-02 A2 | AC-07 |
| UC-02 E1 | AC-08 |
| UC-02 E2 | AC-09 |
| UC-02 E3 | AC-10, AC-11 |
| UC-02 E4 | AC-12 |
| UC-02 E5 | AC-13 |
| UC-02 E6 | AC-14 |
| UC-02 E7 | AC-15 |
| UC-03 main | AC-17 |
| UC-03 A1 | AC-18 |
| UC-03 A2 | AC-19 |
| UC-03 E1 | AC-20 |
| UC-03 E2 | AC-21 |
| UC-04 main | AC-22, AC-23 |
| UC-04 A1 | AC-24 |
| UC-04 A2 | AC-25 |
| UC-04 E1 | AC-26 |
| UC-04 E2 | AC-27 |
| UC-04 E3 | AC-28 |
| UC-04 E4 | AC-29 |
| UC-04 E5 | AC-30 |
| UC-05 main | AC-31 |
| UC-05 A1 | AC-32 |
| UC-05 A2 | AC-33 |
| UC-05 E1 | AC-34 |
| UC-05 E2 | AC-35 |
| UC-06 main | AC-36 |
| UC-06 A1 | AC-37 |
| UC-06 A2 | AC-38 |
| UC-06 E1 | AC-39 |
| UC-07 main | AC-40, AC-41 |
| UC-07 A1 | AC-42 |
| UC-07 E1 | AC-43 |
| UC-07 E2 | AC-44 |
| UC-07 E3 | AC-45 |
| UC-07 E4 | AC-46 |

## 8. Out of scope
- **Copies of passwords outside the current data:** old logs, backups, and messages already in the queue when this goes live (Q-07).
- Changing or recovering a password, and password strength rules. No such operation exists today.
- Signing out of all sessions, listing sessions, or a limit on the number of sessions.
- A planned procedure for changing the signing identity with an overlap period. AC-25 only covers that the verifying services pick up the change.
- Withdrawing an access credential before it expires.
- Limiting repeated sign-in attempts.

## 9. Glossary (new terms only)
- **Access credential** — short-lived proof of identity, presented on each operation.
- **Renewal credential** — single-use proof, presented only to the authentication service to get new credentials.
- **Session** — the sign-in on one device, from sign-in until it expires, is ended or is revoked. A renewal credential belongs to one session.
- **Spent** — a renewal credential that has already been used.
- **Revoked** — ended by the system because of suspected theft.
- **Signing identity** — what the authentication service uses to issue credentials. Its public part lets others check them, but not create them.

## 10. Assumptions & open questions
Decisions already taken: asymmetric signing with published public information, single use with reuse detection, several sessions, and signing out of the current one only.

- [x] Q-01 — Durations: access 15 minutes, session 7 days without renewal, 30 days at most. (answer: accepted, no change requested)
- [x] Q-02 — Plaintext passwords: fixed in this feature, stored with PBKDF2. RS256 was considered and discarded for passwords because it's fast, unsalted and can be attacked with the public key; it stays for signing credentials. Existing passwords are converted when this goes live. They also leave the list of accounts, the logs and the message queue. (answer: 2026-11-04)
- [x] Q-03 — Sign-in returns two credentials, which breaks the current contract. (answer: accepted, no transition period)
- [x] Q-04 — Everyone signs in again once when this goes live. (answer: accepted for this occasion)
- [x] Q-05 — No tolerance window: a client that loses a renewal's response and retries gets its session revoked. (answer: risk accepted, unstable networks aren't a concern now)
- [x] Q-06 — Sign-in reports "temporarily unavailable" when the data can't be reached. (answer: accepted)
- [x] Q-08 — Account creation for a new employee: a direct call from the employee service to the authentication service, in one step for the administrator. (answer: 2026-11-04)
- [x] Q-07 — A request waiting in the message queue when this goes live contains a readable password, and nothing would process it any more. It's discarded, and the administrator creates that account by hand if one was pending. (answer: accepted)
- [x] Q-09 — An employee created with a user name that already has an account keeps that account's password, and the password provided is ignored (UC-07 A1). It's today's behavior and it's what makes retrying possible, but the administrator isn't told in the answer, only the log records it. (answer: accepted)
- [x] Q-10 — If the conversion of existing passwords fails, the authentication service doesn't start (UC-06 E1). That favors security over availability: nobody can sign in until it's fixed. (answer: accepted)
