# Project Constitution — Customer Support CRM (SDD attempt)

Non-negotiable rules that every `spec.md`, `plan.md`, `tasks.md`, and line of
code in this repository must respect. If a later artifact conflicts with this
file, the conflict is resolved by fixing the later artifact, not by silently
ignoring the constitution.

## 1. Spec before plan, plan before tasks, tasks before code

Work happens in this order, and each stage requires explicit human sign-off
before the next stage starts:

`spec.md` (what & why) → **review gate** → `plan.md` (how) → **review gate**
→ `tasks.md` (ordered, testable units of work) → **review gate** → implementation.

No production code is written before `tasks.md` is approved. If implementation
surfaces a gap in the spec or plan, the agent stops, updates the relevant
artifact, and flags the change — it does not silently improvise around it.

## 2. Scope discipline

The approved scope for this feature is **Task Scope v1.0** (the assessment's
Mandatory capability list), not the full original feature brief. `spec.md`
must state explicitly what is in scope and what is deliberately deferred, and
`plan.md`/`tasks.md` may not silently expand scope. A "nice idea" found during
implementation becomes a note for later, not an unplanned addition.

## 3. Verification over assertion

A task is not "done" because the code was written; it is done because it was
run and its behavior was checked against the spec's acceptance criteria.
Every task in `tasks.md` states how it will be verified (a test, a command, a
manual check) before it is marked complete. "It should work" is not a
verification method.

## 4. Environment truth over assumption

Technology choices are constrained by what this machine can actually build
and run, checked directly (tool versions, available runtimes) rather than
assumed from what's conventional. A choice made for this reason is recorded
in `plan.md` with the evidence (e.g. `node --version` failing), not asserted
without support.

## 5. Simplicity and honesty about trade-offs

No abstraction, package, or pattern is introduced without a concrete need
stated in `plan.md`. Every non-obvious decision (a library choice, a workflow
rule, a deferred feature) is written down with its reason at the point it is
made, so a reviewer can evaluate the trade-off instead of just the outcome.

## 6. Traceability

Every task in `tasks.md` references the spec requirement(s) it satisfies.
Every commit references the task(s) it completes. A reviewer should be able
to go from a line of code back to the requirement that justified it.
