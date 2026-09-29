# AI Usage & Verification

This build was done with Claude Code (Claude Sonnet 5) following a
Spec-Driven Development workflow: `spec.md` → review gate → `plan.md` →
review gate → `tasks.md` → review gate → implementation. This document is an
honest account of how AI was used, what was reviewed, and what was actually
*verified to work* — the same discipline the spec/plan/tasks/code chain
itself is built on (Constitution §3).

## How the SDD workflow was actually followed

- **Constitution written first**, before the spec, so "spec before plan
  before tasks before code" and "verification over assertion" were rules to
  be checked against, not retrofitted.
- **Three human review gates**, not implied ones: after `spec.md` (approving
  four explicit Clarifications where the task brief was ambiguous), after
  `plan.md` (approving the stack decisions and their environment evidence),
  and after `tasks.md` (approving the 25-task, test-first breakdown) —
  before any implementation code was written.
- **Test-first within every capability**: for each rule (ticket transitions,
  assignment, customer validation, the auth boundary, dashboard scoping,
  customer CRUD, ticket workflow), the test file was written and confirmed to
  fail to compile *before* the implementation existed, then implementation
  was added until it passed. This is visible directly in the git history —
  each phase's commit says what failed first and what made it pass, not just
  what was added.
- **Plan amended in place, twice, live**, rather than silently worked
  around, when reality contradicted it (Constitution §1) — see below.

## What was actually verified (not just "should work")

1. `dotnet build` on the empty scaffold before any feature code.
2. Every phase built and, where the test runner allowed it, tested before
   moving to the next.
3. Once the test runner was confirmed unreliable on this machine (below),
   every phase's behavior was re-verified by running the real API with
   `dotnet run` and hitting it with `curl` — reading actual HTTP status
   codes and response bodies, not assumed behavior.
4. A full browser session at the end: logged in as the seeded Administrator,
   created a customer, created a ticket, walked it Open → InProgress →
   Resolved, assigned it to the seeded Agent, confirmed the history log;
   logged out, logged back in as that Agent, and confirmed the dashboard
   showed exactly that Agent's 2 tickets with correct per-status counts —
   not the Administrator's view.
5. `scripts/smoke-test.sh`, an 11-check scripted version of the same
   acceptance scenarios, kept in the repo as a repeatable artifact.

## The environment investigation: what was found and how the plan was corrected

This is the part most worth reading honestly, because it's real evidence of
verification catching a wrong first guess, not just a list of successes.

**Finding #1** — `dotnet test` failed with `System.IO.FileLoadException: ...
An Application Control policy has blocked this file`, for
`CustomerSupportCRM.Domain.dll`, loaded by `testhost.exe`. Checked directly
via `Get-WinEvent -LogName "Microsoft-Windows-CodeIntegrity/Operational"`
rather than guessed from the exception message: event 3077, Smart App
Control, Policy ID `{0283ac0f-fff1-49ae-ada1-8a933130cad6}` — the same
policy from an earlier attempt at this same assessment task in this session,
which had (at the time) only blocked the *integration* test project.

**First hypothesis** (recorded in `plan.md`, later corrected): assumed a
plain `dotnet new console` app run via `dotnet run` would sidestep the
block, since that execution path had run ASP.NET Core apps all session
without issue.

**Finding #2** — that hypothesis was tested, not just asserted: a
from-scratch console project referencing `Domain` hit the *identical*
`FileLoadException` on `dotnet run`, including after a full clean rebuild
(ruling out "stale evaluation" as the cause). But running the real
`CustomerSupportCRM.Api` project loaded the same `Domain.dll` with no issue
at all.

**Conclusion, written into `plan.md` as a second amendment**: the block is
neither "testhost.exe" nor "any unsigned DLL" — it is specifically generic
console-app process hosts (rare, low-reputation to Smart App Control) and
`testhost.exe` (a known strict target), while the common ASP.NET Core
hosting pattern is trusted. The console-app workaround was deleted (keeping
broken, misleading code around would have been worse than admitting the
first idea didn't pan out), and verification shifted to what was actually
proven reliable: the running API, driven over real HTTP.

This is exactly the failure mode Constitution §1 exists for — plans are
allowed to be wrong on the first pass, but the artifact gets corrected in
place with what was actually found, not silently patched around in code
while the plan keeps describing something that turned out not to be true.

## What a human should still review

- The dev JWT signing key in `appsettings.Development.json` is a placeholder
  and must be replaced before any real deployment.
- No rate limiting or lockout-after-failed-attempts on `/api/auth/login`
  beyond ASP.NET Identity's defaults.
- `JwtAuthenticationStateProvider` on the Blazor client decodes the JWT
  purely to drive UI state; it is not a security boundary — the API
  independently validates and enforces the token on every request.
- The four Clarifications in `spec.md` §4 are this build's resolution of real
  ambiguity in the task brief; a different reviewer might resolve them
  differently, which is exactly why they're written down rather than buried
  in code.
