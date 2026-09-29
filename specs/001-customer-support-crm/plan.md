# Implementation Plan: Customer Support CRM

**Input**: `spec.md` (approved) · **Constitution**: `.specify/memory/constitution.md`

This is the *how*. Every decision below either traces to a spec requirement
or is justified on its own (Constitution §5). Task-level breakdown is
`tasks.md`, written after this plan is approved.

## 1. Technical Context

| | |
|---|---|
| Available runtime | .NET SDK 10.0.401 (`dotnet --version`, checked just now) |
| Node.js / npm | **Not present** — `where node` / `where npm` both return no match |
| Python / uv | **Not present** — same check as the previous attempt in this session |
| Database server | None installed; no Docker |

## 2. Stack decision (Constitution §4 — environment truth)

**Backend**: ASP.NET Core Web API (C#) + EF Core.
**Persistence**: SQLite (file-based, no server process, no Docker).
**Frontend**: Blazor WebAssembly.
**Auth**: ASP.NET Core Identity (password hashing, user store) + JWT bearer
tokens for the API.

**Why not React/Node for the frontend**: no Node.js or npm on this machine
(evidence above). A React frontend could be *written* here but never *run or
verified* here, which would violate Constitution §3 (verification over
assertion) at the frontend layer entirely. Blazor WebAssembly is buildable
and runnable with only the .NET SDK already confirmed present.

**Why not SQL Server/Postgres**: no database server and no Docker on this
machine. SQLite keeps `git clone && dotnet run` working for anyone
evaluating this, with no environment setup beyond the .NET SDK. The
persistence layer is isolated behind repository/service interfaces
(§3 below) specifically so this is a one-line provider swap later, not a
rewrite.

**Known SQLite/EF Core limitation to design around up front** (learned the
hard way on the previous attempt in this session, now applied proactively
instead of discovered by a runtime crash): EF Core's SQLite provider cannot
translate `ORDER BY` over a `DateTimeOffset` column. All persisted timestamps
use `DateTime` (UTC), not `DateTimeOffset`, from the first migration. This is
exactly what Constitution §5 asks for — the trade-off and its reason are
recorded before writing the entity, not after debugging a stack trace.

## 3. Architecture

Layered, same shape on both projects in this session because it is the
shape that satisfies the spec's own requirements, not house style for its
own sake:

```
src/
  CustomerSupportCRM.Domain          Entities, enums. No dependencies.
  CustomerSupportCRM.Application     DTOs, service interfaces, validation,
                                      Result<T> (no EF Core / ASP.NET Core
                                      dependency -- keeps FR-002, FR-003,
                                      FR-010 unit-testable in isolation)
  CustomerSupportCRM.Infrastructure  EF Core + SQLite, ASP.NET Identity, JWT,
                                      service implementations
  CustomerSupportCRM.Api             Controllers, auth wiring
  CustomerSupportCRM.Web             Blazor WebAssembly client
tests/
  CustomerSupportCRM.Application.Tests   unit tests
  CustomerSupportCRM.Api.Tests           integration tests (WebApplicationFactory)
```

Dependency direction: `Api` → `Application` + `Infrastructure` → `Domain`.
`Application` depends only on `Domain`, never on `Infrastructure` — this is
what makes FR-010's transition rules and FR-002/FR-003's validation rules
testable without a database (Constitution §3: a unit test that needs no
database is faster to run and cheaper to trust than one that does).

## 4. Data model (outline — full schema is a task, not a plan decision)

- **Customer**: Id, FullName, Email (unique index), Phone?, Company?, Notes?,
  CreatedAt, UpdatedAt. → FR-001..FR-005.
- **Ticket**: Id, Subject, Description, CustomerId (FK), Category, Priority,
  Status, AssignedAgentId (FK, nullable), CreatedAt, UpdatedAt. → FR-006..FR-012.
- **TicketHistoryEntry**: Id, TicketId (FK), Type (Created/StatusChanged/
  Assigned), Description, ActorUserId, CreatedAt. → FR-011.
- **ApplicationUser** (Identity): Id, FullName, Email, Role (Administrator |
  Agent). → FR-013..FR-015.

Status transition table (resolves spec Clarification #2 into an explicit,
implementable rule — this is the "defined set of allowed status transitions"
FR-010 requires):

| From | Allowed next |
|---|---|
| Open | InProgress, Waiting, Closed |
| InProgress | Waiting, Resolved, Closed |
| Waiting | InProgress, Resolved, Closed |
| Resolved | Closed, Open (reopen) |
| Closed | Open (reopen) |

Enforced as a small pure function on the `Ticket` entity
(`IsValidTransition(from, to)`), not scattered `if` checks in a controller —
this is the concrete unit under test for acceptance scenario 5.

## 5. API surface (outline)

- `POST /api/auth/login`, `POST /api/auth/register-agent` (Administrator-only) → FR-013, FR-014
- `GET/POST/PUT/DELETE /api/customers[, /{id}]` → FR-001..FR-005
- `GET/POST/PUT /api/tickets[, /{id}]`, `POST /api/tickets/{id}/assign`,
  `POST /api/tickets/{id}/status`, `GET /api/tickets/{id}/history` → FR-006..FR-012
- `GET /api/dashboard/me` → FR-016..FR-018
- `GET /api/users/agents` (read-only directory, for the assignment picker —
  not full user management, which stays out of scope per spec §2)

Every non-2xx response carries a machine-readable error (validation field
errors for 400, a message for 404/409/403), because the frontend (FR-019)
needs to actually display *why* a request failed, not just that it did.

## 6. Testing strategy (Constitution §3)

- **Unit tests** (`Application.Tests`): the status-transition table above
  (all 5×5 combinations, not just the happy path), the assignment rule
  (FR-012), and customer validation (FR-002, FR-003). No database, no HTTP —
  fast enough to run on every change.
- **Integration tests** (`Api.Tests`, `WebApplicationFactory` + in-memory
  SQLite): the auth boundary (FR-013), the full customer CRUD round trip
  (FR-001, FR-004, FR-005), and the full ticket lifecycle including dashboard
  scoping (FR-016..FR-018) — these are the acceptance scenarios in `spec.md`
  §6, executed as code rather than left as prose.
- **Manual/scripted fallback**: if this machine's environment blocks the
  integration test *runner* specifically (as happened on the prior attempt —
  a Smart App Control / WDAC policy blocking `testhost.exe`), a
  `scripts/smoke-test.sh` re-runs the same acceptance scenarios over real
  HTTP as a documented, repeatable substitute — decided here, not
  improvised after hitting the problem again.

## 7. Explicit non-goals (repeating spec §2 so `tasks.md` can't silently drift)

No pagination/sorting/filtering UI beyond what's needed to demonstrate the
flow; no password-reset flow (no email channel in scope); no audit log
beyond `TicketHistoryEntry` (full audit logging is Security & Administration
bonus scope).

## Review checklist

- [ ] Every FR in `spec.md` is addressed by something in §3–§6 above.
- [ ] Every stack/architecture decision has a stated reason, not just a
      stated choice.
- [ ] Nothing here re-opens a question already closed in spec.md's
      Clarifications.

**Next step after this is approved:** `tasks.md` — the ordered, dependency-
aware, independently-verifiable task list derived from this plan.
