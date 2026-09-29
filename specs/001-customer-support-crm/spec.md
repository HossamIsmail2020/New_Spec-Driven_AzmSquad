# Feature Specification: Customer Support CRM (Mandatory Scope)

**Feature branch**: `001-customer-support-crm`
**Status**: Draft — awaiting review before `plan.md`
**Input**: Azm Squads AI-Assisted Full-Stack Engineering Assessment — Task Scope v1.0 (Mandatory capabilities)

This document describes **what** the system must do and **why**, for a
non-technical reviewer as much as an engineer. It contains no technology,
framework, or database choices — those belong in `plan.md`, written only
after this spec is approved (Constitution §1).

## 1. Purpose

Support agents need one place to see who a customer is, what they've asked
for, and what still needs doing — without hunting across email threads or
spreadsheets. This feature delivers the smallest complete slice of a support
CRM that makes that true: customers, tickets, an agent's own queue, and the
access control to make it safe for multiple agents to share.

## 2. Scope

### In scope (Mandatory — Task Scope v1.0)

| # | Capability |
|---|---|
| 1 | Customer records: create, view, update, delete, with validation |
| 2 | Ticket records with a defined workflow: category, priority, status, assignment |
| 3 | An agent-facing dashboard of their own work |
| 4 | A coherent flow from the user interface through to stored data |
| 5 | Authentication, and at least one authorization boundary between roles |
| 6 | Documentation a new developer can follow to run and understand the system |
| 7 | Evidence the system behaves as specified — automated tests or an equivalent repeatable, documented check |

### Explicitly out of scope for this feature

AI-assisted features (summarization, suggested replies, chatbot), external
communication channels (email/WhatsApp/SMS/live chat), SLA timers and
automated escalation, a knowledge base, a customer-facing self-service portal,
management reporting beyond the agent's own dashboard, external system
integrations, multi-branch/multi-department structure, localization, and
custom branding. These are real, valuable capabilities — they are excluded
here so the mandatory slice above is built solidly rather than all of them
shallowly. A future spec can add any of them without changing what's built
here.

## 3. Actors

- **Agent** — the primary user. Manages customers, works tickets, sees their
  own dashboard.
- **Administrator** — a superset of Agent. Additionally manages which agent an
  account belongs to and can reassign any ticket to any agent.
- **Customer** — appears only as a record other actors manage; customers do
  not use this system directly in this feature (a self-service portal is
  out of scope, §2).

## 4. Clarifications

Points where the task brief was ambiguous, the decision taken, and why —
flagged here for review rather than silently assumed, per Constitution §5.
**If any of these resolutions don't match your intent, say so before this
moves to `plan.md`.**

1. **Q: What are the roles for the required authorization boundary?**
   **Decision:** Administrator and Agent, as described in §3. Rationale: the
   task brief requires "at least one role/permission boundary" without naming
   roles; Admin/Agent is the minimum pair that makes ticket assignment
   (capability #2) meaningfully restricted rather than a formality.
2. **Q: What ticket categories, priorities, and statuses exist?**
   **Decision:** Category: General, Billing, Technical, Account, Other.
   Priority: Low, Medium, High, Urgent. Status: Open, In Progress, Waiting,
   Resolved, Closed, with Resolved/Closed reachable only from an active state
   and reopening (Closed/Resolved → Open) as the sole backward move (see
   FR-010). Rationale: these are the smallest sets that make the acceptance
   scenarios in §6 meaningfully testable; the brief did not enumerate values.
3. **Q: Does "tests or verification evidence" require automated tests
   specifically?**
   **Decision:** No — either is acceptable, but whichever is used must be
   repeatable by someone other than the person who built it, not a one-time
   manual click-through. `plan.md` will state which is used and why.
4. **Q: Can an Agent create another Agent account?**
   **Decision:** No — only an Administrator creates Agent accounts. Rationale:
   this is an internal tool with no public self-registration; letting any
   Agent mint new accounts would make the authorization boundary (FR-013)
   meaningless.

## 5. Functional Requirements

Each requirement is independently testable and traces to a capability in §2.

**Customers (capability #1)**

- **FR-001**: Users MUST be able to create a customer record with, at
  minimum, a full name and an email address.
- **FR-002**: The system MUST reject a customer record with a missing or
  malformed name or email, and MUST NOT silently accept it with the field
  blanked out.
- **FR-003**: The system MUST reject creating a customer whose email matches
  an existing customer's email exactly.
- **FR-004**: Users MUST be able to view, update, and delete an existing
  customer record.
- **FR-005**: The system MUST prevent deleting a customer who has any ticket
  on record, rather than silently orphaning that ticket's customer reference.

**Tickets (capability #2)**

- **FR-006**: Users MUST be able to create a ticket for an existing customer,
  with a subject, a description, a category, and a priority (see Clarification
  #2 for the value sets).
- **FR-007**: The system MUST reject a ticket referencing a customer that does
  not exist.
- **FR-008**: A new ticket MUST start in the Open status.
- **FR-009**: Users MUST be able to update a ticket's subject, description,
  category, and priority after creation.
- **FR-010**: The system MUST enforce a defined set of allowed status
  transitions and reject any transition outside it (e.g. a ticket cannot jump
  from Open directly to Resolved, skipping the work in between; see
  Clarification #2 for the exact transition table). Rejecting an invalid
  transition MUST NOT change the ticket's status.
- **FR-011**: The system MUST record a timestamped history entry every time a
  ticket is created, has its status changed, or is (re)assigned, visible to
  users viewing that ticket.
- **FR-012**: An Administrator MUST be able to assign any ticket to any
  Agent. An Agent MUST be able to assign an unassigned ticket to themself, and
  MUST NOT be able to assign a ticket to a different Agent or reassign a
  ticket already assigned to someone else.

**Authentication & authorization (capability #5)**

- **FR-013**: Users MUST authenticate (e.g. email + password) before accessing
  any customer or ticket data; unauthenticated requests MUST be rejected.
- **FR-014**: The system MUST distinguish at least two roles (Administrator,
  Agent) and enforce at least one action that only Administrators can perform
  (creating an Agent account, per Clarification #4; reassigning tickets across
  Agents, per FR-012).
- **FR-015**: No API response MUST ever include a user's password or password
  hash.

**Agent dashboard (capability #3)**

- **FR-016**: An Agent MUST be able to see a summary of tickets currently
  assigned to them, including a count broken down by status.
- **FR-017**: The dashboard MUST show, for each assigned ticket, enough
  customer context (at minimum the customer's name) that the Agent does not
  need to open the ticket to know who it's for.
- **FR-018**: An Agent's dashboard MUST NOT show another Agent's assigned
  tickets.

**End-to-end coherence (capability #4)**

- **FR-019**: Every capability above MUST be reachable and usable through the
  user interface, not only through direct API calls — i.e. a user can
  complete the full cycle (create a customer, open a ticket for them, work
  it to resolution) without leaving the UI.

## 6. Acceptance Scenarios

Written so a reviewer can literally step through them against the running
system.

1. **Given** no existing customer with that email, **when** a user submits a
   valid name and email, **then** a new customer record exists and is
   retrievable. (FR-001)
2. **Given** a customer already exists with email `x@example.com`, **when** a
   user tries to create another customer with that same email, **then** the
   system refuses and no second record is created. (FR-003)
3. **Given** a customer has at least one ticket, **when** a user tries to
   delete that customer, **then** the system refuses and the customer record
   still exists. (FR-005)
4. **Given** a valid customer, **when** a user creates a ticket for them,
   **then** the ticket exists with status Open and a "created" history entry.
   (FR-006, FR-008, FR-011)
5. **Given** an Open ticket, **when** a user attempts to move it directly to
   Resolved, **then** the system refuses and the ticket remains Open.
   (FR-010)
6. **Given** an unassigned ticket, **when** an Agent assigns it to themself,
   **then** the assignment succeeds; **when** a different Agent then tries to
   reassign that same ticket to themself, **then** the system refuses.
   (FR-012)
7. **Given** two Agents each have a different ticket assigned to them,
   **when** Agent A views their dashboard, **then** they see only their own
   ticket, not Agent B's. (FR-016, FR-018)
8. **Given** no authentication token, **when** any customer or ticket
   endpoint is requested, **then** the system refuses the request. (FR-013)
9. **Given** an Agent (not an Administrator) is signed in, **when** they
   attempt to create another Agent account, **then** the system refuses.
   (FR-014, Clarification #4)

## 7. Key Entities (conceptual — no schema yet)

- **Customer** — a person or organization the support desk serves. Has a
  name, an email (unique), and optional contact/notes fields.
- **Ticket** — a single support request. Belongs to exactly one Customer, has
  a category, a priority, a status that moves through a defined workflow, and
  is optionally assigned to one Agent.
- **Ticket History Entry** — an immutable record of something that happened
  to a Ticket (created, status changed, assigned), who did it, and when.
- **User Account** — a person who can sign in, with a role of Administrator
  or Agent.

## Review & Acceptance Checklist

- [ ] No implementation detail (language, framework, database, API shape)
      appears above.
- [ ] Every functional requirement is testable as written.
- [ ] Every acceptance scenario in §6 traces to at least one FR.
- [ ] Every out-of-scope item in §2 is a deliberate exclusion, not an
      oversight.
- [ ] The four Clarifications in §4 reflect decisions the reviewer actually
      agrees with — not just decisions that were made.

**Next step after this is approved:** `plan.md` — technical approach,
architecture, and stack, evaluated against what this development machine can
actually build and run (Constitution §4).
