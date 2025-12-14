#!/bin/bash

# Interactive test for git-forest plan installation CLI experience
# This script demonstrates the user experience of installing plans

set -e

# Detect script directory and repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

CLI_PATH="${CLI_PATH:-$REPO_ROOT/src/GitForest.Cli/bin/Debug/net10.0/GitForest.Cli}"
PLANS_DIR="${PLANS_DIR:-$REPO_ROOT/config/plans}"
TEST_DIR="/tmp/test-forest-interactive-$$"

# Colors
BLUE='\033[0;34m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

heading() {
    echo ""
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}  $1${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo ""
}

step() {
    echo -e "${CYAN}➜${NC} $1"
}

command_demo() {
    echo -e "${YELLOW}\$ $1${NC}"
    eval "$1"
    echo ""
}

# Setup
heading "Git-Forest Plan Installation CLI Experience Test"

step "Setting up test repository"
rm -rf "$TEST_DIR"
mkdir -p "$TEST_DIR"
cd "$TEST_DIR"
git init -q
git config user.email "test@example.com"
git config user.name "Test User"
echo "# Test Repository for git-forest" > README.md
git add README.md
git commit -q -m "Initial commit"
echo "Test repository created at: $TEST_DIR"
echo ""

# Test 1: Initialize forest
heading "1. Initialize a Forest"
step "Initialize git-forest in the repository"
command_demo "$CLI_PATH init"

step "Initialize with JSON output"
command_demo "$CLI_PATH init --json"

# Test 2: Check status
heading "2. Check Forest Status"
step "View human-readable status"
command_demo "$CLI_PATH status"

step "View JSON status"
command_demo "$CLI_PATH status --json"

# Test 3: List plans (empty)
heading "3. List Installed Plans (Initially Empty)"
step "List plans"
command_demo "$CLI_PATH plans list"

step "List plans with JSON"
command_demo "$CLI_PATH plans list --json"

# Test 4: Install first plan
heading "4. Install a Plan from Local Path"
step "Install dependency-hygiene plan"
PLAN1="$PLANS_DIR/engineering-excellence/dependency-hygiene.yaml"
command_demo "$CLI_PATH plans install $PLAN1"

step "Install with JSON output"
command_demo "$CLI_PATH plans install $PLAN1 --json"

# Test 5: Install multiple plans
heading "5. Install Multiple Plans from Different Categories"

step "Install test-pyramid-balance (Quality & Reliability)"
PLAN2="$PLANS_DIR/quality-reliability/test-pyramid-balance.yaml"
command_demo "$CLI_PATH plans install $PLAN2"

step "Install secret-hygiene (Security & Compliance)"
PLAN3="$PLANS_DIR/security-compliance/secret-hygiene.yaml"
command_demo "$CLI_PATH plans install $PLAN3"

step "Install latency-budgeting (Performance & Scalability)"
PLAN4="$PLANS_DIR/performance-scalability/latency-budgeting.yaml"
command_demo "$CLI_PATH plans install $PLAN4"

step "Install living-architecture (Documentation & Knowledge)"
PLAN5="$PLANS_DIR/documentation-knowledge/living-architecture.yaml"
command_demo "$CLI_PATH plans install $PLAN5"

# Test 6: Different source formats
heading "6. Test Different Source Formats"

step "Install from GitHub slug (stubbed)"
command_demo "$CLI_PATH plans install tweakch/git-forest-plans/sample"

step "Install from URL (stubbed)"
command_demo "$CLI_PATH plans install https://github.com/tweakch/plans/sample.yaml"

# Test 7: Help commands
heading "7. Help and Documentation"

step "General help"
command_demo "$CLI_PATH --help"

step "Plans command help"
command_demo "$CLI_PATH plans --help"

step "Plans install help"
command_demo "$CLI_PATH plans install --help"

# Test 8: After installation
heading "8. After Installation"

step "Check status after installing plans"
command_demo "$CLI_PATH status"

step "List installed plans"
command_demo "$CLI_PATH plans list"

# Summary
heading "Test Summary"
echo "✓ Successfully tested plan installation CLI experience"
echo "✓ Tested human-readable output format"
echo "✓ Tested JSON output format for automation"
echo "✓ Tested multiple plan sources (local, GitHub, URL)"
echo "✓ Tested help and documentation"
echo ""
echo "The CLI provides a clean and intuitive experience for:"
echo "  • Initializing forests"
echo "  • Checking status"
echo "  • Installing plans from various sources"
echo "  • Getting help and documentation"
echo ""
echo -e "${GREEN}All tests completed successfully!${NC}"
echo ""

# Cleanup
cd /
rm -rf "$TEST_DIR"
