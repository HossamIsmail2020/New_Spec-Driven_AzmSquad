#!/usr/bin/env bash
# Documented, repeatable end-to-end verification of spec.md §6's acceptance
# scenarios, run over the real HTTP API -- the fallback plan.md §6 committed
# to in advance (see docs/AI_USAGE.md for why dotnet test's own runner is
# unreliable on this machine).
#
# Usage: start the API first (dotnet run --project src/CustomerSupportCRM.Api
# --urls http://localhost:5192), then:
#   bash scripts/smoke-test.sh [base_url]
set -uo pipefail

BASE_URL="${1:-http://localhost:5192}"
FAILED=0
PASSED=0

pass() { PASSED=$((PASSED + 1)); echo "  PASS: $1"; }
fail() { FAILED=$((FAILED + 1)); echo "  FAIL: $1"; }

extract() { # extract <json> <field>
  echo "$1" | grep -o "\"$2\":\"[^\"]*\"" | head -1 | cut -d'"' -f4
}

echo "== 1. Login as seeded Administrator (FR-013) =="
LOGIN=$(curl -s -X POST "$BASE_URL/api/auth/login" -H "Content-Type: application/json" \
  -d '{"email":"admin@azmsquad-sdd.dev","password":"Passw0rd!"}')
ADMIN_TOKEN=$(extract "$LOGIN" token)
[ -n "$ADMIN_TOKEN" ] && pass "Admin login returned a token" || fail "Admin login did not return a token"

echo "== 2. Login as seeded Agent =="
AGENT_LOGIN=$(curl -s -X POST "$BASE_URL/api/auth/login" -H "Content-Type: application/json" \
  -d '{"email":"agent@azmsquad-sdd.dev","password":"Passw0rd!"}')
AGENT_TOKEN=$(extract "$AGENT_LOGIN" token)
AGENT_ID=$(extract "$AGENT_LOGIN" userId)
[ -n "$AGENT_TOKEN" ] && pass "Agent login returned a token" || fail "Agent login did not return a token"

echo "== 3. Unauthenticated request is rejected (FR-013) =="
CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/customers")
[ "$CODE" = "401" ] && pass "GET /api/customers without a token returns 401" || fail "Expected 401, got $CODE"

echo "== 4. Non-Administrator cannot register an agent (FR-014, Clarification #4) =="
CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/auth/register-agent" \
  -H "Authorization: Bearer $AGENT_TOKEN" -H "Content-Type: application/json" \
  -d '{"fullName":"X","email":"x@example.com","password":"Passw0rd!"}')
[ "$CODE" = "403" ] && pass "Agent blocked from register-agent with 403" || fail "Expected 403, got $CODE"

echo "== 5. Create a customer with validation (FR-001..FR-003) =="
EMAIL="smoke.$(date +%s)@example.com"
CUSTOMER=$(curl -s -X POST "$BASE_URL/api/customers" -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"fullName\":\"Smoke Test Customer\",\"email\":\"$EMAIL\",\"phone\":null,\"company\":null,\"notes\":null}")
CUSTOMER_ID=$(extract "$CUSTOMER" id)
[ -n "$CUSTOMER_ID" ] && pass "Customer created" || fail "Customer creation failed: $CUSTOMER"

DUP=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/customers" -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" -d "{\"fullName\":\"Dup\",\"email\":\"$EMAIL\"}")
[ "$DUP" = "409" ] && pass "Duplicate email rejected with 409" || fail "Expected 409, got $DUP"

BAD=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/customers" -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" -d '{"fullName":"X","email":"not-an-email"}')
[ "$BAD" = "400" ] && pass "Invalid email rejected with 400" || fail "Expected 400, got $BAD"

echo "== 6. Create a ticket and verify workflow (FR-006..FR-010) =="
TICKET=$(curl -s -X POST "$BASE_URL/api/tickets" -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"subject\":\"Smoke test ticket\",\"description\":\"Created by smoke-test.sh\",\"customerId\":\"$CUSTOMER_ID\",\"category\":\"Technical\",\"priority\":\"High\"}")
TICKET_ID=$(extract "$TICKET" id)
TICKET_STATUS=$(extract "$TICKET" status)
[ "$TICKET_STATUS" = "Open" ] && pass "New ticket starts in Open status" || fail "Expected Open, got $TICKET_STATUS"

INVALID=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/tickets/$TICKET_ID/status" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" -d '{"newStatus":"Resolved"}')
[ "$INVALID" = "409" ] && pass "Open -> Resolved (invalid) rejected with 409" || fail "Expected 409, got $INVALID"

VALID=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/tickets/$TICKET_ID/status" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" -d '{"newStatus":"InProgress"}')
[ "$VALID" = "200" ] && pass "Open -> InProgress (valid) accepted with 200" || fail "Expected 200, got $VALID"

echo "== 7. Assign the ticket and verify the agent's dashboard (FR-012, FR-016..FR-018) =="
ASSIGN=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/tickets/$TICKET_ID/assign" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" -d "{\"agentId\":\"$AGENT_ID\"}")
[ "$ASSIGN" = "200" ] && pass "Administrator assigns ticket to agent" || fail "Expected 200, got $ASSIGN"

DASHBOARD=$(curl -s "$BASE_URL/api/dashboard/me" -H "Authorization: Bearer $AGENT_TOKEN")
if echo "$DASHBOARD" | grep -q "$TICKET_ID"; then
  pass "Agent dashboard shows the assigned ticket"
else
  fail "Agent dashboard did not contain the assigned ticket: $DASHBOARD"
fi

echo "== 8. Ticket history is recorded (FR-011) =="
HISTORY=$(curl -s "$BASE_URL/api/tickets/$TICKET_ID/history" -H "Authorization: Bearer $ADMIN_TOKEN")
COUNT=$(echo "$HISTORY" | grep -o "\"id\":" | wc -l)
[ "$COUNT" -ge 3 ] && pass "History has $COUNT entries (created, status change, assigned)" || fail "Expected >= 3 entries, got $COUNT"

echo ""
echo "================================"
echo "Passed: $PASSED  Failed: $FAILED"
echo "================================"
[ "$FAILED" -eq 0 ]
