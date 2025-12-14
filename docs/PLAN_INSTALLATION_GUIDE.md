# Plan Installation Quick Start Guide

This guide shows you how to install and use plans in git-forest.

## Installation Methods

### Method 1: Install from Local Path

Install a plan from the pre-defined catalog:

```bash
# Install a specific plan by path
git-forest plans install config/plans/engineering-excellence/dependency-hygiene.yaml

# Or use an absolute path
git-forest plans install /full/path/to/plan.yaml
```

### Method 2: Install from GitHub (Coming Soon)

Install a plan directly from a GitHub repository:

```bash
# Using GitHub slug notation
git-forest plans install tweakch/git-forest-plans/sample

# With specific ref (tag, branch, or SHA)
git-forest plans install tweakch/git-forest-plans/sample --ref v1.0.0
```

### Method 3: Install from URL (Coming Soon)

Install a plan from any HTTPS URL:

```bash
git-forest plans install https://example.com/plans/my-plan.yaml
```

## Available Plan Categories

git-forest includes 48 pre-defined plans across 10 categories:

### 1. Engineering Excellence (6 plans)
```bash
git-forest plans install config/plans/engineering-excellence/dependency-hygiene.yaml
git-forest plans install config/plans/engineering-excellence/architecture-hardening.yaml
git-forest plans install config/plans/engineering-excellence/api-contract-stability.yaml
git-forest plans install config/plans/engineering-excellence/refactor-hotspots.yaml
git-forest plans install config/plans/engineering-excellence/cyclomatic-reduction.yaml
git-forest plans install config/plans/engineering-excellence/dead-code-elimination.yaml
```

### 2. Quality & Reliability (5 plans)
```bash
git-forest plans install config/plans/quality-reliability/test-pyramid-balance.yaml
git-forest plans install config/plans/quality-reliability/mutation-testing.yaml
git-forest plans install config/plans/quality-reliability/flaky-test-eradication.yaml
git-forest plans install config/plans/quality-reliability/chaos-readiness.yaml
git-forest plans install config/plans/quality-reliability/observability-boost.yaml
```

### 3. Security & Compliance (5 plans)
```bash
git-forest plans install config/plans/security-compliance/secret-hygiene.yaml
git-forest plans install config/plans/security-compliance/threat-modeling.yaml
git-forest plans install config/plans/security-compliance/authz-consistency.yaml
git-forest plans install config/plans/security-compliance/dependency-vulnerability-guard.yaml
git-forest plans install config/plans/security-compliance/audit-trail-enforcement.yaml
```

### 4. Performance & Scalability (5 plans)
```bash
git-forest plans install config/plans/performance-scalability/latency-budgeting.yaml
git-forest plans install config/plans/performance-scalability/allocation-pressure-reduction.yaml
git-forest plans install config/plans/performance-scalability/throughput-optimization.yaml
git-forest plans install config/plans/performance-scalability/io-efficiency.yaml
git-forest plans install config/plans/performance-scalability/orleans-readiness.yaml
```

### 5. Documentation & Knowledge (4 plans)
```bash
git-forest plans install config/plans/documentation-knowledge/living-architecture.yaml
git-forest plans install config/plans/documentation-knowledge/decision-recording.yaml
git-forest plans install config/plans/documentation-knowledge/public-api-docs.yaml
git-forest plans install config/plans/documentation-knowledge/internal-playbooks.yaml
```

See [config/plans/README.md](../config/plans/README.md) for all 48 plans.

## Working with Plans

### List Installed Plans

```bash
# Human-readable format
git-forest plans list

# JSON format for automation
git-forest plans list --json
```

### Check Forest Status

```bash
# See overview of your forest
git-forest status

# JSON format
git-forest status --json
```

## Workflow Example

Here's a typical workflow for getting started with git-forest plans:

```bash
# 1. Initialize forest in your git repository
cd /path/to/your/repo
git-forest init

# 2. Check initial status
git-forest status

# 3. Install a plan
git-forest plans install config/plans/engineering-excellence/dependency-hygiene.yaml

# 4. List installed plans
git-forest plans list

# 5. Reconcile the plan (generate plants)
git-forest plan dependency-hygiene reconcile

# 6. List generated plants
git-forest plants list

# 7. Check updated status
git-forest status
```

## JSON Output for Automation

All commands support `--json` flag for machine-readable output:

```bash
# Initialize
git-forest init --json
# Output: {"status":"initialized","directory":".git-forest"}

# Status
git-forest status --json
# Output: {"forest":"initialized","repo":"origin/main","plans":0,"plants":0,"planters":0,"lock":"free"}

# Install
git-forest plans install config/plans/engineering-excellence/dependency-hygiene.yaml --json
# Output: {"status":"installed","source":"config/plans/engineering-excellence/dependency-hygiene.yaml"}

# List
git-forest plans list --json
# Output: {"plans":[]}
```

## Getting Help

The CLI includes comprehensive help:

```bash
# General help
git-forest --help

# Plans command help
git-forest plans --help

# Install command help
git-forest plans install --help
```

## Next Steps

After installing plans:

1. **Reconcile Plans**: Generate plants from your installed plans
   ```bash
   git-forest plan <plan-id> reconcile
   ```

2. **List Plants**: See what work items were generated
   ```bash
   git-forest plants list
   ```

3. **Assign Planters**: Assign agents to work on plants
   ```bash
   git-forest plant <plant-key> assign <planter-id>
   ```

4. **Track Progress**: Monitor the lifecycle of your plants
   ```bash
   git-forest plant <plant-key> show
   ```

## Testing

To test the plan installation CLI experience:

```bash
# Run automated tests
./tests/manual/test-plan-installation.sh

# Run interactive demo
./tests/manual/test-plan-installation-interactive.sh

# View test report
cat tests/manual/PLAN_INSTALLATION_TEST_REPORT.md
```

See [tests/manual/README.md](../tests/manual/README.md) for more details.

## Troubleshooting

### Forest not initialized
```
Error: forest not initialized
```
**Solution**: Run `git-forest init` in your git repository

### Plan not found
```
Error: plan file not found
```
**Solution**: Check that the path to the plan file is correct

### Invalid plan format
```
Error: invalid plan YAML
```
**Solution**: Verify the plan file follows the correct schema (see config/plans/README.md)

## Learn More

- [CLI Specification](../CLI.md) - Complete CLI documentation
- [README](../README.md) - Project overview and concepts
- [Plan Catalog](../config/plans/README.md) - All available plans
- [Test Report](../tests/manual/PLAN_INSTALLATION_TEST_REPORT.md) - Detailed test results
