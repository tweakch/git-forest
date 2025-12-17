# 📚 Plans Catalog

This directory contains pre-defined plan templates that can be installed and used as organizational lenses for managing your git-forest.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Plan Categories](#plan-categories)
  - [🏗️ Engineering Excellence](#-engineering-excellence)
  - [✅ Quality & Reliability](#-quality--reliability)
  - [⚡ Performance & Scalability](#-performance--scalability)
  - [🔒 Security & Compliance](#-security--compliance)
  - [👥 Team & Process](#-team--process)
  - [📖 Documentation & Knowledge](#-documentation--knowledge)
  - [🔄 Evolution & Migration](#-evolution--migration)
  - [🤖 AI-Native](#-ai-native)
  - [🎯 Meta / Governance](#-meta--governance)
  - [🧪 Experimental](#-experimental)
- [Plan Structure](#plan-structure)
- [Using Plans](#using-plans)

---

## Overview

git-forest includes **49 pre-defined plans** organized into **10 categories**. Each plan targets a specific aspect of codebase health and team productivity.

| Category | Plans | Focus Area |
|----------|:-----:|------------|
| [🏗️ Engineering Excellence](#-engineering-excellence) | 6 | Code health, sustainability, long-term velocity |
| [✅ Quality & Reliability](#-quality--reliability) | 9 | Correctness, confidence, operability |
| [⚡ Performance & Scalability](#-performance--scalability) | 5 | Speed, predictability, scale |
| [🔒 Security & Compliance](#-security--compliance) | 5 | Preventative security, regulatory compliance |
| [👥 Team & Process](#-team--process) | 5 | Developer experience, workflow optimization |
| [📖 Documentation & Knowledge](#-documentation--knowledge) | 4 | Living documentation, knowledge management |
| [🔄 Evolution & Migration](#-evolution--migration) | 5 | Strategic change, system evolution |
| [🤖 AI-Native](#-ai-native) | 5 | AI-assisted development, automation |
| [🎯 Meta / Governance](#-meta--governance) | 5 | Plan management, resource governance |
| [🧪 Experimental](#-experimental) | 4 | Innovative approaches to code quality |

---

## Plan Categories

### 1. Engineering Excellence Plans
Focused on code health, sustainability, and long-term velocity.

- **architecture-hardening** - Enforce boundaries, detect layering violations, stabilize core domains
- **dependency-hygiene** - Reduce transitive deps, pin versions, remove unused packages
- **api-contract-stability** - Validate public APIs, semver discipline, breaking-change detection
- **refactor-hotspots** - Identify churn-heavy files and concentrate refactors there
- **cyclomatic-reduction** - Target complex methods, split responsibilities
- **dead-code-elimination** - Find unreachable, unused, or legacy paths

### 2. Quality & Reliability Plans
Focused on correctness, confidence, and operability.

- **test-pyramid-balance** - Shift tests toward unit / slice tests where appropriate
- **mutation-testing** - Strengthen weak tests by introducing mutations
- **flaky-test-eradication** - Detect and stabilize nondeterministic tests
- **chaos-readiness** - Inject failures (timeouts, retries, partial outages)
- **observability-boost** - Logs, metrics, traces completeness
- **unit-testing-discipline** - Raise unit test coverage and quality; identify gaps; strengthen assertions
- **tdd-enablement** - Reduce red/green/refactor loop friction (speed, determinism, ergonomics)
- **bdd-scenarios** - Define and automate behavior scenarios with shared domain language
- **integration-testing-harness** - Build stable integration harnesses and cover critical boundaries

### 3. Performance & Scalability Plans
Beyond raw speed — also predictability.

- **latency-budgeting** - Enforce response time SLOs per boundary
- **allocation-pressure-reduction** - Target GC hotspots, pooling, spans, struct usage
- **throughput-optimization** - Async pipelines, batching, parallelism
- **io-efficiency** - File, DB, HTTP usage patterns
- **orleans-readiness** - Actor-friendly refactors, stateless services, grain boundaries

### 4. Security & Compliance Plans
Preventative, not reactive.

- **threat-modeling** - Surface attack vectors and trust boundaries
- **secret-hygiene** - Remove hardcoded secrets, rotate keys
- **authz-consistency** - Ensure authorization is enforced uniformly
- **dependency-vulnerability-guard** - CVE scanning + mitigation plans
- **audit-trail-enforcement** - Ensure critical actions are traceable

### 5. Team & Process Plans
These shape how humans work with the forest.

- **developer-experience** - Faster builds, better errors, tooling cleanup
- **onboarding-acceleration** - Docs, examples, guided walkthroughs
- **scrum-signal** - Align backlog, PRs, and commits with Scrum goals
- **flow-efficiency** - Reduce WIP, PR size, cycle time
- **knowledge-radiation** - Turn tribal knowledge into docs/tests/diagrams

### 6. Documentation & Knowledge Plans
More than "improve-docs".

- **living-architecture** - Keep diagrams, ADRs, and code aligned
- **decision-recording** - Extract implicit decisions into ADRs
- **public-api-docs** - Consumer-oriented docs and examples
- **internal-playbooks** - Runbooks for ops, incidents, migrations

### 7. Evolution & Migration Plans
Strategic change over time.

- **monolith-modularization** - Gradual slicing without big-bang rewrites
- **legacy-extraction** - Isolate legacy zones behind interfaces
- **cloud-readiness** - Config, scaling, resilience for cloud targets
- **framework-upgrade** - e.g. .NET LTS migrations, breaking change handling
- **orleans-adoption** - Move coordination logic to virtual actors

### 8. AI-Native Plans
Plans that explicitly assume AI planters.

- **semantic-code-map** - Build a knowledge graph of the repo
- **intent-preservation** - Ensure refactors don't drift from original intent
- **regression-scout** - Continuously look for subtle behavior changes
- **memory-guard** - Prevent context loss across long refactor cycles
- **self-healing-forest** - Auto-detect issues and plant new work

### 9. Meta / Governance Plans
Plans that manage plans.

- **repository-overview** - Comprehensive repository status overview for team leads
- **forest-governance** - Rules, limits, quotas for planters
- **plan-composition** - Detect conflicting or overlapping plans
- **risk-aware-planning** - Rank plants by blast radius
- **harvest-discipline** - Define when "done" actually means done
- **forest-maintenance** - Self-inspection plan for git-forest to dogfood itself in CI

### 10. Experimental Plans
Fun but useful experimental plans.

- **code-archeology** - Discover why things exist
- **intent-drift-detection** - Compare current code vs original commits
- **complexity-budgeting** - Enforce global complexity limits
- **entropy-reduction** - Fight gradual degradation

## Plan Structure

Each plan is defined in a YAML file with the following structure:

```yaml
id: plan-id
name: Plan Name
version: 1.0.0
category: category-name
description: Detailed description of what this plan does
scopes:
  - scope1
  - scope2
planners:
  - planner-id
planters:
  - planter-id
policies:
  execution_mode: manual
  risk_level: low
```

> **Note:** See individual plan files for complete examples with additional fields like `metrics`, `plant_templates`, and `focus_areas`.

---

## Using Plans

### Installing a Plan

```bash
# Install a plan from the catalog
git-forest plans install config/plans/engineering-excellence/dependency-hygiene.yaml
```

### Listing Installed Plans

```bash
# List all installed plans
git-forest plans list
```

### Reconciling a Plan

```bash
# Reconcile a plan to generate plants (work items)
git-forest plan dependency-hygiene reconcile
```

### Example Workflow

```bash
# 1. Install the security hygiene plan
git-forest plans install config/plans/security-compliance/secret-hygiene.yaml

# 2. Reconcile to generate plants
git-forest plan secret-hygiene reconcile

# 3. List generated plants
git-forest plants list --plan secret-hygiene

# 4. Show details of a specific plant
git-forest plant secret-hygiene:remove-hardcoded-secret show
```
