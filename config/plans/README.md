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

### 🏗️ Engineering Excellence

> *Focused on code health, sustainability, and long-term velocity.*

| Plan ID | Description |
|---------|-------------|
| `architecture-hardening` | Enforce boundaries, detect layering violations, stabilize core domains |
| `dependency-hygiene` | Reduce transitive deps, pin versions, remove unused packages |
| `api-contract-stability` | Validate public APIs, semver discipline, breaking-change detection |
| `refactor-hotspots` | Identify churn-heavy files and concentrate refactors there |
| `cyclomatic-reduction` | Target complex methods, split responsibilities |
| `dead-code-elimination` | Find unreachable, unused, or legacy paths |

---

### ✅ Quality & Reliability

> *Focused on correctness, confidence, and operability.*

| Plan ID | Description |
|---------|-------------|
| `test-pyramid-balance` | Shift tests toward unit/slice tests where appropriate |
| `mutation-testing` | Strengthen weak tests by introducing mutations |
| `flaky-test-eradication` | Detect and stabilize nondeterministic tests |
| `chaos-readiness` | Inject failures (timeouts, retries, partial outages) |
| `observability-boost` | Logs, metrics, traces completeness |
| `unit-testing-discipline` | Raise unit test coverage and quality; identify gaps; strengthen assertions |
| `tdd-enablement` | Reduce red/green/refactor loop friction (speed, determinism, ergonomics) |
| `bdd-scenarios` | Define and automate behavior scenarios with shared domain language |
| `integration-testing-harness` | Build stable integration harnesses and cover critical boundaries |

---

### ⚡ Performance & Scalability

> *Beyond raw speed — also predictability.*

| Plan ID | Description |
|---------|-------------|
| `latency-budgeting` | Enforce response time SLOs per boundary |
| `allocation-pressure-reduction` | Target GC hotspots, pooling, spans, struct usage |
| `throughput-optimization` | Async pipelines, batching, parallelism |
| `io-efficiency` | File, DB, HTTP usage patterns |
| `orleans-readiness` | Actor-friendly refactors, stateless services, grain boundaries |

---

### 🔒 Security & Compliance

> *Preventative, not reactive.*

| Plan ID | Description |
|---------|-------------|
| `threat-modeling` | Surface attack vectors and trust boundaries |
| `secret-hygiene` | Remove hardcoded secrets, rotate keys |
| `authz-consistency` | Ensure authorization is enforced uniformly |
| `dependency-vulnerability-guard` | CVE scanning + mitigation plans |
| `audit-trail-enforcement` | Ensure critical actions are traceable |

---

### 👥 Team & Process

> *These shape how humans work with the forest.*

| Plan ID | Description |
|---------|-------------|
| `developer-experience` | Faster builds, better errors, tooling cleanup |
| `onboarding-acceleration` | Docs, examples, guided walkthroughs |
| `scrum-signal` | Align backlog, PRs, and commits with Scrum goals |
| `flow-efficiency` | Reduce WIP, PR size, cycle time |
| `knowledge-radiation` | Turn tribal knowledge into docs/tests/diagrams |

---

### 📖 Documentation & Knowledge

> *More than "improve-docs".*

| Plan ID | Description |
|---------|-------------|
| `living-architecture` | Keep diagrams, ADRs, and code aligned |
| `decision-recording` | Extract implicit decisions into ADRs |
| `public-api-docs` | Consumer-oriented docs and examples |
| `internal-playbooks` | Runbooks for ops, incidents, migrations |

---

### 🔄 Evolution & Migration

> *Strategic change over time.*

| Plan ID | Description |
|---------|-------------|
| `monolith-modularization` | Gradual slicing without big-bang rewrites |
| `legacy-extraction` | Isolate legacy zones behind interfaces |
| `cloud-readiness` | Config, scaling, resilience for cloud targets |
| `framework-upgrade` | e.g., .NET LTS migrations, breaking change handling |
| `orleans-adoption` | Move coordination logic to virtual actors |

---

### 🤖 AI-Native

> *Plans that explicitly assume AI planters.*

| Plan ID | Description |
|---------|-------------|
| `semantic-code-map` | Build a knowledge graph of the repo |
| `intent-preservation` | Ensure refactors don't drift from original intent |
| `regression-scout` | Continuously look for subtle behavior changes |
| `memory-guard` | Prevent context loss across long refactor cycles |
| `self-healing-forest` | Auto-detect issues and plant new work |

---

### 🎯 Meta / Governance

> *Plans that manage plans.*

| Plan ID | Description |
|---------|-------------|
| `forest-governance` | Rules, limits, quotas for planters |
| `plan-composition` | Detect conflicting or overlapping plans |
| `risk-aware-planning` | Rank plants by blast radius |
| `harvest-discipline` | Define when "done" actually means done |
| `forest-maintenance` | Self-inspection plan for git-forest to dogfood itself in CI |

---

### 🧪 Experimental

> *Fun but useful experimental plans.*

| Plan ID | Description |
|---------|-------------|
| `code-archeology` | Discover why things exist |
| `intent-drift-detection` | Compare current code vs original commits |
| `complexity-budgeting` | Enforce global complexity limits |
| `entropy-reduction` | Fight gradual degradation |

---

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
