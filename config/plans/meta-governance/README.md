# Forest Maintenance Plan - Quick Start Guide

## Overview

The `forest-maintenance` plan enables git-forest to inspect and maintain its own codebase in a safe, deterministic way. This guide shows you how to use it.

## Installation

```bash
# From within the git-forest repository
gf init
gf plans install config/plans/meta-governance/forest-maintenance.yaml
```

## Basic Usage

### List Installed Plans

```bash
gf plans list
```

Expected output:
```
ID                   Version   Category          Installed
forest-maintenance   1.0.0     meta-governance   2024-01-15
```

### Reconcile the Plan

This generates plants based on the current state of the codebase:

```bash
gf plan forest-maintenance reconcile
```

Expected output:
```
Reconciling plan 'forest-maintenance@1.0.0'...
Planners: +5 ~0 -0
Planters: +2 ~0 -0
Plants:   +12 ~0 -0 (archived 0)
done
```

### View Generated Plants

```bash
# List all plants from this plan
gf plants list --plan forest-maintenance

# Filter by category
gf plants list --plan forest-maintenance --scope impl-consistency
gf plants list --plan forest-maintenance --scope code-health

# Filter by priority
gf plants list --plan forest-maintenance --priority high
```

### View Plant Details

```bash
# View a specific plant
gf plant forest-maintenance:impl-spec-mismatch show

# View plant history
gf plant forest-maintenance:impl-spec-mismatch history
```

### Get JSON Output

For automation and CI integration:

```bash
gf plants list --plan forest-maintenance --json > plants.json
gf plan forest-maintenance reconcile --json > reconcile-result.json
```

## CI Integration

The forest-maintenance plan runs automatically in CI via the `forest-self-inspection.yml` workflow:

- **Triggers**: Push to main, Pull requests, Weekly schedule, Manual dispatch
- **What it does**: 
  1. Builds git-forest
  2. Initializes a forest
  3. Installs the forest-maintenance plan
  4. Reconciles the plan to generate plants
  5. Reports generated plants as artifacts

### View CI Results

1. Go to the Actions tab in GitHub
2. Select "Forest Self-Inspection" workflow
3. View the latest run
4. Download the "forest-maintenance-report" artifact

## Safety Guarantees

The forest-maintenance plan is designed to be **completely safe** in CI:

✅ **Read-only operations** - Only analyzes code, never modifies files  
✅ **Advisory output** - Generates plants for human review  
✅ **No auto-commits** - Never commits changes automatically  
✅ **Deterministic** - Same codebase always produces same results  
✅ **Idempotent** - Running multiple times is safe  

## Plant Categories

Plants are organized into 5 categories:

1. **impl-consistency** - Implementation vs. specification mismatches
2. **code-health** - Code quality and refactoring opportunities
3. **forest-validator** - Forest structure validation issues
4. **plan-catalog-auditor** - Plan catalog integrity problems
5. **test-coverage** - Test coverage gaps

## Example Workflow

```bash
# 1. Install and reconcile the plan
gf plans install config/plans/meta-governance/forest-maintenance.yaml
gf plan forest-maintenance reconcile

# 2. Review high-priority plants
gf plants list --plan forest-maintenance --priority high

# 3. Choose a plant to work on
gf plant forest-maintenance:impl-spec-mismatch show

# 4. Assign a planter to the plant
gf plant forest-maintenance:impl-spec-mismatch assign forest-maintainer

# 5. Let the planter propose changes (human review required)
gf planter forest-maintainer grow forest-maintenance:impl-spec-mismatch --mode propose

# 6. Review the proposed changes and create a PR manually
```

## Metrics

The plan tracks several quality metrics:

- **implementation_consistency_score** - Target: 95%
- **documentation_drift_count** - Target: 0
- **plan_catalog_errors** - Target: 0
- **test_coverage_gaps** - Target: 0

View metrics:

```bash
gf plan forest-maintenance show --json | jq '.metrics'
```

## Planners

The plan includes 5 planners:

1. **impl-consistency** - Compares CLI.md with implementation
2. **code-health** - Analyzes code quality
3. **forest-validator** - Validates forest structure
4. **plan-catalog-auditor** - Audits plan catalog
5. **test-coverage** - Identifies test gaps

Each planner can be run independently:

```bash
gf planner impl-consistency run --plan forest-maintenance
```

## Troubleshooting

### Plan won't install

```bash
# Check if forest is initialized
gf status

# If not, initialize it
gf init
```

### No plants generated

This is normal if the codebase is in good shape! The plan only generates plants when it detects issues.

### Plants seem incorrect

The planners are deterministic but may need tuning. To provide feedback:

1. Note the plant key
2. Run `gf plant <key> show --json`
3. File an issue with the plant details

## Advanced Usage

### Dry Run

Preview what reconcile would do without making changes:

```bash
gf plan forest-maintenance reconcile --dry-run
```

### Scoped Reconciliation

Only run specific planners:

```bash
gf plan forest-maintenance reconcile --only impl-consistency
gf plan forest-maintenance reconcile --only code-health,test-coverage
```

### Custom Configuration

Override plan defaults in `.git-forest/config.yaml`:

```yaml
plans:
  forest-maintenance:
    policies:
      max_concurrent_plants: 10
    metrics:
      implementation_consistency_score:
        target: 98
```

## Documentation

- [Forest Maintenance Contract](../docs/forest-maintenance-contract.md) - Detailed contract specification
- [CLI Documentation](../CLI.md) - Complete CLI reference
- [Plan Catalog](./README.md) - All available plans

## Support

For issues with the forest-maintenance plan:

1. Check the [contract documentation](../docs/forest-maintenance-contract.md)
2. Review the [CI workflow](.github/workflows/forest-self-inspection.yml)
3. File an issue with:
   - Output of `gf status --json`
   - Output of `gf plan forest-maintenance show --json`
   - Description of unexpected behavior
