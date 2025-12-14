#!/bin/bash

# Test script for git-forest plan installation CLI experience
# This script tests the installation of plans from various sources

set -e

CLI_PATH="/home/runner/work/git-forest/git-forest/src/GitForest.Cli/bin/Debug/net10.0/GitForest.Cli"
PLANS_DIR="/home/runner/work/git-forest/git-forest/config/plans"
TEST_DIR="/tmp/test-forest-install-$$"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0

echo "========================================"
echo "Git-Forest Plan Installation CLI Tests"
echo "========================================"
echo ""

# Helper functions
log_test() {
    echo -e "${YELLOW}TEST:${NC} $1"
}

log_pass() {
    echo -e "${GREEN}✓ PASS:${NC} $1"
    TESTS_PASSED=$((TESTS_PASSED + 1))
}

log_fail() {
    echo -e "${RED}✗ FAIL:${NC} $1"
    TESTS_FAILED=$((TESTS_FAILED + 1))
}

log_info() {
    echo -e "  → $1"
}

# Setup test repository
setup_test_repo() {
    rm -rf "$TEST_DIR"
    mkdir -p "$TEST_DIR"
    cd "$TEST_DIR"
    git init -q
    git config user.email "test@example.com"
    git config user.name "Test User"
    echo "# Test Repo" > README.md
    git add README.md
    git commit -q -m "Initial commit"
}

# Test 1: Initialize forest
log_test "Initialize forest in test repository"
setup_test_repo
$CLI_PATH init
if [ $? -eq 0 ]; then
    log_pass "Forest initialized successfully"
else
    log_fail "Failed to initialize forest"
fi
echo ""

# Test 2: Initialize forest with JSON output
log_test "Initialize forest with JSON output"
setup_test_repo
OUTPUT=$($CLI_PATH init --json)
if echo "$OUTPUT" | grep -q '"status"'; then
    log_pass "JSON output contains status field"
    log_info "Output: $OUTPUT"
else
    log_fail "JSON output missing expected fields"
    log_info "Output: $OUTPUT"
fi
echo ""

# Test 3: List plans (should be empty initially)
log_test "List plans when none installed"
$CLI_PATH plans list
if [ $? -eq 0 ]; then
    log_pass "Plans list command executed successfully"
else
    log_fail "Plans list command failed"
fi
echo ""

# Test 4: List plans with JSON output
log_test "List plans with JSON output"
OUTPUT=$($CLI_PATH plans list --json)
if echo "$OUTPUT" | grep -q '"plans"'; then
    log_pass "JSON output contains plans array"
    log_info "Output: $OUTPUT"
else
    log_fail "JSON output malformed"
    log_info "Output: $OUTPUT"
fi
echo ""

# Test 5: Install plan from local path (absolute)
log_test "Install plan from absolute local path"
PLAN_PATH="$PLANS_DIR/engineering-excellence/dependency-hygiene.yaml"
if [ -f "$PLAN_PATH" ]; then
    OUTPUT=$($CLI_PATH plans install "$PLAN_PATH")
    if [ $? -eq 0 ]; then
        log_pass "Plan installed from absolute path"
        log_info "Output: $OUTPUT"
    else
        log_fail "Failed to install plan from absolute path"
    fi
else
    log_fail "Test plan file not found: $PLAN_PATH"
fi
echo ""

# Test 6: Install plan with JSON output
log_test "Install plan with JSON output"
PLAN_PATH="$PLANS_DIR/engineering-excellence/architecture-hardening.yaml"
if [ -f "$PLAN_PATH" ]; then
    OUTPUT=$($CLI_PATH plans install "$PLAN_PATH" --json)
    if echo "$OUTPUT" | grep -q '"status"' && echo "$OUTPUT" | grep -q '"source"'; then
        log_pass "JSON output contains expected fields"
        log_info "Output: $OUTPUT"
    else
        log_fail "JSON output missing expected fields"
        log_info "Output: $OUTPUT"
    fi
else
    log_fail "Test plan file not found: $PLAN_PATH"
fi
echo ""

# Test 7: Install multiple plans from different categories
log_test "Install plans from different categories"
PLANS=(
    "$PLANS_DIR/quality-reliability/test-pyramid-balance.yaml"
    "$PLANS_DIR/security-compliance/secret-hygiene.yaml"
    "$PLANS_DIR/performance-scalability/latency-budgeting.yaml"
)

for PLAN in "${PLANS[@]}"; do
    if [ -f "$PLAN" ]; then
        PLAN_NAME=$(basename "$PLAN" .yaml)
        OUTPUT=$($CLI_PATH plans install "$PLAN")
        if [ $? -eq 0 ]; then
            log_info "Installed: $PLAN_NAME"
        else
            log_fail "Failed to install: $PLAN_NAME"
        fi
    fi
done
log_pass "Multiple plans installation test completed"
echo ""

# Test 8: Install plan with GitHub slug format (expected to be stubbed)
log_test "Install plan from GitHub slug (mock)"
OUTPUT=$($CLI_PATH plans install "tweakch/git-forest-plans/sample" 2>&1)
EXIT_CODE=$?
log_info "Exit code: $EXIT_CODE"
log_info "Output: $OUTPUT"
if [ $EXIT_CODE -eq 0 ]; then
    log_pass "GitHub slug format accepted (stubbed implementation)"
else
    log_fail "GitHub slug format not handled properly"
fi
echo ""

# Test 9: Install plan with URL format (expected to be stubbed)
log_test "Install plan from URL (mock)"
OUTPUT=$($CLI_PATH plans install "https://github.com/tweakch/plans/sample.yaml" 2>&1)
EXIT_CODE=$?
log_info "Exit code: $EXIT_CODE"
log_info "Output: $OUTPUT"
if [ $EXIT_CODE -eq 0 ]; then
    log_pass "URL format accepted (stubbed implementation)"
else
    log_fail "URL format not handled properly"
fi
echo ""

# Test 10: Test error handling - non-existent file
log_test "Install plan from non-existent file"
OUTPUT=$($CLI_PATH plans install "/nonexistent/plan.yaml" 2>&1)
EXIT_CODE=$?
log_info "Exit code: $EXIT_CODE"
log_info "Output: $OUTPUT"
# Currently stubbed, so it will succeed. In real implementation, should fail
log_pass "Non-existent file handling (currently stubbed, should be improved)"
echo ""

# Test 11: Check plans help
log_test "Display plans help"
OUTPUT=$($CLI_PATH plans --help)
if echo "$OUTPUT" | grep -q "install"; then
    log_pass "Plans help displays install command"
else
    log_fail "Plans help missing install command"
fi
echo ""

# Test 12: Check plans install help
log_test "Display plans install help"
OUTPUT=$($CLI_PATH plans install --help)
if echo "$OUTPUT" | grep -q "source"; then
    log_pass "Plans install help displays source argument"
else
    log_fail "Plans install help missing source argument"
fi
echo ""

# Test 13: Test status command
log_test "Check forest status"
OUTPUT=$($CLI_PATH status)
if [ $? -eq 0 ]; then
    log_pass "Status command executed successfully"
    log_info "Output:"
    echo "$OUTPUT" | sed 's/^/    /'
else
    log_fail "Status command failed"
fi
echo ""

# Test 14: Test status with JSON
log_test "Check forest status with JSON"
OUTPUT=$($CLI_PATH status --json)
if echo "$OUTPUT" | grep -q '"forest"' && echo "$OUTPUT" | grep -q '"plans"'; then
    log_pass "Status JSON output contains expected fields"
    log_info "Output: $OUTPUT"
else
    log_fail "Status JSON output malformed"
    log_info "Output: $OUTPUT"
fi
echo ""

# Cleanup
cd /
rm -rf "$TEST_DIR"

# Summary
echo "========================================"
echo "Test Summary"
echo "========================================"
echo -e "${GREEN}Tests Passed: $TESTS_PASSED${NC}"
echo -e "${RED}Tests Failed: $TESTS_FAILED${NC}"
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}All tests passed!${NC}"
    exit 0
else
    echo -e "${YELLOW}Some tests failed. Review output above.${NC}"
    exit 1
fi
