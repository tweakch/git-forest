# Forest Maintenance Plan Contract

## Overview

The `forest-maintenance` plan is a special meta-governance plan that enables git-forest to dogfood itself in CI. This plan allows git-forest to run on its own codebase to continuously validate the core lifecycle (Plan → Planner → Plants → Planter) in a safe, deterministic way.

## Plan Metadata

- **ID**: `forest-maintenance`
- **Version**: `1.0.0`
- **Category**: `meta-governance`
- **Name**: Forest Maintenance
- **Description**: Self-inspection plan for git-forest to validate its own implementation and maintain code quality

## Safety Rules and Boundaries

### Allowed Operations

The forest-maintenance plan is restricted to **read-only and advisory operations** only:

1. **Code Analysis** - Read and analyze source code, configurations, and documentation
2. **Quality Metrics** - Calculate and report on code quality metrics
3. **Consistency Checks** - Verify consistency between CLI spec, docs, and implementation
4. **Pattern Detection** - Identify code patterns, anti-patterns, and improvement opportunities
5. **Validation** - Validate that the forest structure follows its own specifications
6. **Advisory Output** - Generate plant recommendations for human review

### Forbidden Operations

To ensure CI safety, the following operations are **strictly forbidden**:

1. **Auto-commit** - No automatic commits to the repository
2. **Force-push** - No force pushing or rewriting git history
3. **Branch Modification** - No automatic branch creation or modification
4. **File Modification** - No direct file writes without explicit human approval
5. **External Network Calls** - No outbound network requests (except for approved package sources)
6. **Destructive Operations** - No deletion of files, branches, or forest state
7. **Policy Override** - Cannot modify or bypass forest governance policies

### Execution Mode

- **Mode**: `propose` (never `apply`)
- **Risk Level**: `low`
- **Max Concurrent Plants**: `5`
- **Require Review**: `true`
- **Auto-apply**: `false`

## Planner Categories

The forest-maintenance plan defines planners organized by inspection domain:

### 1. Implementation Consistency Planner (`impl-consistency`)
- **Intent**: Verify that implementation matches CLI specification and documentation
- **Outputs**: Plants identifying inconsistencies between CLI.md, docs/, and src/
- **Examples**:
  - Missing CLI commands defined in spec but not implemented
  - Inconsistent exit codes between spec and implementation
  - Outdated documentation for changed commands

### 2. Code Health Planner (`code-health`)
- **Intent**: Assess overall code quality and identify improvement opportunities
- **Outputs**: Plants for refactoring, cleanup, and technical debt reduction
- **Examples**:
  - Complex methods requiring simplification
  - Duplicated code that could be extracted
  - Missing error handling or validation

### 3. Forest Structure Validator (`forest-validator`)
- **Intent**: Ensure the forest structure follows its own organizational principles
- **Outputs**: Plants for structural improvements to the .git-forest layout
- **Examples**:
  - Inconsistent naming conventions
  - Missing required metadata files
  - Schema validation failures

### 4. Plan Catalog Auditor (`plan-catalog-auditor`)
- **Intent**: Validate the integrity and completeness of the plan catalog
- **Outputs**: Plants for plan improvements, missing plans, or catalog inconsistencies
- **Examples**:
  - Plans missing required fields
  - Duplicate plan IDs or conflicting versions
  - Undocumented plans in catalog

### 5. Test Coverage Analyzer (`test-coverage`)
- **Intent**: Identify gaps in test coverage for core functionality
- **Outputs**: Plants for adding or improving tests
- **Examples**:
  - Untested CLI commands
  - Missing integration tests
  - Uncovered error paths

## Expected Outputs

The forest-maintenance plan generates **plants only** - it never executes changes directly.

### Plant Output Format

Each plant generated follows the standard plant structure:

```yaml
key: forest-maintenance:plant-slug
status: planned
title: Human-readable description of the work
plan_id: forest-maintenance
category: [impl-consistency|code-health|forest-validator|plan-catalog-auditor|test-coverage]
priority: [low|medium|high]
description: Detailed description of what needs to be done
context:
  planner: [planner-id]
  detected_at: [timestamp]
  files_affected: [list of files]
  rationale: Why this work is needed
assigned_planters: []
branches: []
created_at: [timestamp]
```

### Plant Lifecycle in CI

1. **Plan Phase**: CI runs `gf plan forest-maintenance reconcile`
2. **Generation**: Planners analyze the codebase and generate plants
3. **Output**: Plants are written to `.git-forest/plants/` directory
4. **Reporting**: CI job reports the number and categories of plants generated
5. **Human Review**: Developers review plants and decide which to act on
6. **Assignment**: Selected plants are assigned to planters for execution
7. **Execution**: Planters propose changes (PRs) for human approval

## Determinism Guarantees

The forest-maintenance plan ensures deterministic behavior:

1. **Stable Plant Keys**: Same codebase state always produces the same plant keys
2. **Idempotent Reconciliation**: Running `reconcile` multiple times doesn't create duplicate plants
3. **Reproducible Analysis**: Same inputs always produce the same analysis results
4. **No Side Effects**: Read-only operations with no external state modification

## CI Integration

### CI Workflow Steps

```yaml
- name: Run Forest Maintenance
  run: |
    gf init
    gf plans install config/plans/meta-governance/forest-maintenance.yaml
    gf plan forest-maintenance reconcile --json > plants-report.json

- name: Report Plants
  run: |
    gf plants list --plan forest-maintenance --json
    echo "Generated $(gf plants list --plan forest-maintenance | wc -l) maintenance plants"
```

### Success Criteria

CI passes if:
1. Plan installs successfully
2. Reconcile completes without errors
3. All generated plants are valid
4. No forbidden operations were attempted

## Security Considerations

The forest-maintenance plan adheres to strict security boundaries:

1. **No Secrets Access** - Cannot read or write secrets
2. **No Privilege Escalation** - Runs with standard user permissions
3. **Audit Trail** - All operations logged to `.git-forest/logs/`
4. **Resource Limits** - Bounded CPU/memory usage
5. **Timeout Protection** - Operations have configurable timeouts

## Version History

- **v1.0.0** (Initial) - Core contract with read-only, advisory-only operations
