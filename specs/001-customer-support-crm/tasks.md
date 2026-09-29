# Tasks: Customer Support CRM

**Input**: `plan.md` (approved) · **Spec**: `spec.md` · **Constitution**: `.specify/memory/constitution.md`

Rules used below:
- `[P]` = touches files no other unstarted task touches — safe to do in any
  order relative to other `[P]` tasks in the same phase.
- Every task states its **Verify** step (Constitution §3) — a task is not
  checked off until that step actually passes, not when the code is written.
- Test-first within each capability: the test task for a rule is listed
  *before* the implementation task that makes it pass, and is expected to
  fail (red) until that implementation task lands (green).

## Phase 0 — Setup

- [ ] **T001** Create solution + 7 projects (`Domain`, `Application`,
      `Infrastructure`, `Api`, `Web`, `Application.Tests`, `Api.Tests`) with
      the reference graph from `plan.md` §3.
      **Verify**: `dotnet build` succeeds on the empty skeleton.
- [ ] **T002** Add packages: EF Core + SQLite + Design (`Infrastructure` *and*
      `Api`, per the prior session's lesson that EF tools need Design on the
      startup project too), ASP.NET Identity, JWT bearer
      (`Infrastructure` *and* `Api` — JwtTokenGenerator lives in
      Infrastructure and needs `System.IdentityModel.Tokens.Jwt` there
      directly, not just transitively via Api), xUnit, WebApplicationFactory.
      **Verify**: `dotnet build` still succeeds; `dotnet ef` commands resolve
      without a missing-package error.
- [ ] **T003** `.gitignore` includes `*.db`/`*.db-shm`/`*.db-wal` before any
      migration is ever applied locally.
      **Verify**: `git status` never shows `crm.db` as untracked after
      running the API.

## Phase 1 — Domain: ticket workflow rules (FR-010, FR-012)

- [ ] **T004 [P]** Write `TicketTransitionTests.cs` against the transition
      table in `plan.md` §4 — all 5×5 from/to combinations, not just the
      allowed ones. **Expected to fail**: `Ticket` doesn't exist yet.
- [ ] **T005 [P]** Write `TicketAssignmentTests.cs` for FR-012 (Administrator
      assigns anyone; Agent self-assigns only when unassigned; Agent cannot
      assign to another Agent or reassign someone else's ticket). **Expected
      to fail** for the same reason.
- [ ] **T006** Implement `Domain/Entities/Ticket.cs` (+ `Customer.cs`,
      `TicketHistoryEntry.cs`, enums) including `IsValidTransition`,
      `TransitionTo`, `CanBeAssignedTo` exactly matching the table T004/T005
      test against.
      **Verify**: `dotnet test tests/CustomerSupportCRM.Application.Tests`
      — T004 and T005 now pass (red → green).

## Phase 2 — Domain: customer validation (FR-002, FR-003)

- [ ] **T007 [P]** Write `CustomerValidatorTests.cs`: missing/short name,
      missing/malformed email, malformed phone, and the valid case.
      **Expected to fail**: validator doesn't exist yet.
- [ ] **T008** Implement `Application/Customers/CustomerValidator.cs`.
      **Verify**: T007 passes.

## Phase 3 — Persistence (FR-001–FR-012, data model)

- [ ] **T009** `Infrastructure/Identity/ApplicationUser.cs`,
      `Persistence/AppDbContext.cs`, fluent configurations (unique customer
      email index, enum-as-string columns, cascade rules) per `plan.md` §4.
      **Verify**: `dotnet build` succeeds.
- [ ] **T010** Generate the initial EF Core migration.
      **Verify**: `dotnet ef database update` produces a working `crm.db`;
      inspect the schema for the unique email index and the FK constraints.
- [ ] **T011** Dev-only seed (one Administrator, one Agent, one sample
      customer/ticket) so the app is demoable immediately after clone.
      **Verify**: running the API creates the seeded rows exactly once
      (re-running doesn't duplicate them).

## Phase 4 — Auth (FR-013–FR-015)

- [ ] **T012 [P]** Write `AuthApiTests.cs`: login with valid/invalid
      credentials, unauthenticated request to a protected endpoint returns
      401, non-Administrator calling `register-agent` returns 403.
      **Expected to fail**: no auth endpoints yet.
- [ ] **T013** Implement JWT configuration, `AuthService`, `AuthController`
      (`/api/auth/login`, `/api/auth/register-agent`), `[Authorize]` wiring.
      **Verify**: T012 passes; manually confirm via `curl` that no response
      body ever contains a password hash (FR-015).

## Phase 5 — Customers (FR-001–FR-005)

- [ ] **T014 [P]** Write `CustomerApiTests.cs`: create/get/update/delete round
      trip, duplicate-email rejected (409), invalid payload rejected (400),
      delete-with-tickets rejected. **Expected to fail**: no endpoint yet.
- [ ] **T015** Implement `ICustomerService`/`CustomerService` and
      `CustomersController`.
      **Verify**: T014 passes.

## Phase 6 — Tickets (FR-006–FR-012)

- [ ] **T016 [P]** Write `TicketApiTests.cs`: create (starts Open, history
      entry recorded), invalid transition rejected (409) with status
      unchanged, valid transition accepted, Administrator assigns to any
      Agent, Agent self-assigns an unassigned ticket, Agent blocked from
      assigning to/reassigning another Agent's ticket. **Expected to fail**.
- [ ] **T017** Implement `ITicketService`/`TicketService` and
      `TicketsController` (CRUD, `/assign`, `/status`, `/history`).
      **Verify**: T016 passes.

## Phase 7 — Agent dashboard (FR-016–FR-018)

- [ ] **T018 [P]** Write `DashboardApiTests.cs`: two Agents, two tickets each
      assigned to a different one — Agent A's dashboard shows only Agent A's
      ticket and correct per-status counts. **Expected to fail**.
- [ ] **T019** Implement `IDashboardService`/`DashboardService` and
      `DashboardController`.
      **Verify**: T018 passes.

## Phase 8 — End-to-end UI (FR-019)

- [ ] **T020** Blazor WebAssembly project: JWT storage, auth state provider,
      typed API client reusing `Application` DTOs directly (not duplicated).
- [ ] **T021** Pages: Login, Dashboard, Customers (list/create/edit/delete),
      Tickets (list/create, detail with status-change/assign/history).
- [ ] **T022** Manual full-cycle run in an actual browser: create a customer,
      open a ticket for them, work it through to Resolved, confirm it
      disappears from "active" views as expected, log in as a second Agent
      and confirm dashboard scoping holds in the real UI, not just in tests.
      **Verify**: this task's own description *is* its verification — it is
      not marked done from a code review, only from actually doing it.

## Phase 9 — Polish and delivery

- [ ] **T023** `README.md`: setup, run, seeded credentials, how to test,
      assumptions.
- [ ] **T024** `scripts/smoke-test.sh`: scripted re-run of `spec.md` §6's
      acceptance scenarios over real HTTP — the fallback plan.md §6 committed
      to in advance, ready before it's needed rather than improvised if the
      test runner is blocked again.
- [ ] **T025** Full regression pass: `dotnet build` (0 warnings/errors),
      `dotnet test` (all green, or documented + the T024 fallback run if the
      environment blocks the runner), the T022 browser walkthrough repeated
      once more against a clean database.

## Task ↔ Requirement traceability

| Requirement | Tasks |
|---|---|
| FR-001–FR-005 (Customers) | T007, T008, T014, T015 |
| FR-006–FR-012 (Tickets & workflow) | T004, T005, T006, T016, T017 |
| FR-013–FR-015 (Auth) | T012, T013 |
| FR-016–FR-018 (Dashboard) | T018, T019 |
| FR-019 (End-to-end UI) | T020, T021, T022 |

## Review checklist

- [ ] Every FR in `spec.md` §5 appears in the traceability table above.
- [ ] Every implementation task has a test task before it, and that test
      task is expected to fail until the implementation lands.
- [ ] No task's Verify step is "looks right" — each names a command or an
      observable behavior.

**Next step after this is approved:** `/implement` — execute T001 onward in
order, reporting each phase's actual verification result (not just "done"),
per Constitution §3.
