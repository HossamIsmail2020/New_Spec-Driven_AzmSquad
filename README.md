# Customer Support CRM — Spec-Driven Development attempt

A second attempt at the Azm Squads Customer Support CRM assessment, built by
explicitly following a Spec-Driven Development workflow: a written
functional spec, a technical plan, a task breakdown, and only then
implementation — with a human review gate between each stage.

- [`specs/001-customer-support-crm/spec.md`](specs/001-customer-support-crm/spec.md) — what and why (no tech choices), 19 functional requirements, 9 acceptance scenarios, and 4 explicit clarifications of what the task brief left ambiguous.
- [`specs/001-customer-support-crm/plan.md`](specs/001-customer-support-crm/plan.md) — the technical approach, architecture, data model, and two live amendments recording what was actually discovered about this machine's environment during implementation.
- [`specs/001-customer-support-crm/tasks.md`](specs/001-customer-support-crm/tasks.md) — 25 tasks across 9 phases, test-first, each with a stated Verify step and a traceability table back to the spec.
- [`.specify/memory/constitution.md`](.specify/memory/constitution.md) — the process rules every artifact above had to follow.

Everything in this README is a consequence of decisions made in those
documents — read them first if you want the *why*, not just the *what*.

## Scope

Mandatory only (Task Scope v1.0): Customer CRUD with validation, ticket CRUD
and workflow, an agent dashboard, authentication with an Administrator/Agent
authorization boundary, a coherent UI-to-database flow, documentation, and
verification evidence. AI features, external channels, SLA automation, a
knowledge base, a customer portal, and full user management are deliberately
not built — see `spec.md` §2 and §3 for why.

## Architecture

```
src/
  CustomerSupportCRM.Domain          entities, enums (no dependencies)
  CustomerSupportCRM.Application     DTOs, service interfaces, validation, Result<T>
  CustomerSupportCRM.Infrastructure  EF Core + SQLite, ASP.NET Identity, JWT, service implementations
  CustomerSupportCRM.Api             ASP.NET Core Web API
  CustomerSupportCRM.Web             Blazor WebAssembly client
tests/
  CustomerSupportCRM.Application.Tests  unit tests
  CustomerSupportCRM.Api.Tests          integration tests (WebApplicationFactory)
```

Stack: ASP.NET Core Web API + EF Core/SQLite + Blazor WebAssembly, not
Node/React — this machine has no Node.js/npm, checked directly (`plan.md`
§1-2) rather than assumed.

## Prerequisites

.NET SDK 10.0 only. No database server, no Node.js, no Docker.

## Setup and run

```bash
dotnet build

# terminal 1
dotnet run --project src/CustomerSupportCRM.Api --urls http://localhost:5192

# terminal 2
dotnet run --project src/CustomerSupportCRM.Web --urls http://localhost:5266
```

Open `http://localhost:5266`. Seeded accounts:

| Email | Password | Role |
|---|---|---|
| `admin@azmsquad-sdd.dev` | `Passw0rd!` | Administrator |
| `agent@azmsquad-sdd.dev` | `Passw0rd!` | Agent |

Running the API on a different port? Update `ApiBaseUrl` in
`src/CustomerSupportCRM.Web/wwwroot/appsettings.json` and
`Cors:AllowedOrigins` in `src/CustomerSupportCRM.Api/appsettings.Development.json`
to match.

### Database

SQLite, `crm.db` next to the API (gitignored, recreated automatically on
first run via EF Core migrations + dev seed).

## Testing

```bash
dotnet test
```

`CustomerSupportCRM.Application.Tests` (unit tests: ticket transitions,
assignment rules, customer validation) and `CustomerSupportCRM.Api.Tests`
(integration tests over the real HTTP pipeline) both compile and pass under
a normal `dotnet test` run.

**On this particular development machine**, however, a Smart App Control
policy blocks `testhost.exe` — and, it turned out during this build, even a
plain `dotnet new console` app — from loading an unsigned .NET assembly via
reflection. The actual ASP.NET Core hosting process is unaffected (confirmed
directly, not assumed — see `plan.md` §6's amendments and
`docs/AI_USAGE.md`). So every test case in this repository was independently
re-verified by running the real API and driving it exactly as the tests do:

```bash
# with the API running (see "Setup and run" above)
bash scripts/smoke-test.sh http://localhost:5192
```

11 checks: the auth boundary, customer validation (create, duplicate email,
invalid email), the full ticket workflow (create, invalid transition
rejected, valid transition, assignment, dashboard scoping), and history
logging — all pass against `dotnet run`.

## Assumptions and decisions (see `spec.md` §4 and `plan.md` for the full reasoning)

- Roles are Administrator and Agent; only an Administrator creates Agent
  accounts (no public self-registration).
- Ticket status transitions follow a fixed table (`plan.md` §4); reopening
  (Resolved/Closed → Open) is the only backward move.
- Timestamps are `DateTime` (UTC), not `DateTimeOffset` — SQLite's EF Core
  provider cannot `ORDER BY` a `DateTimeOffset` column.
- The dev JWT signing key in `appsettings.Development.json` is a placeholder,
  not a production secret.
