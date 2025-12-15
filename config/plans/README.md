# Plans Catalog

This directory contains pre-defined plan templates that can be installed and used as organizational lenses for managing your git-forest.

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

## Using Plans

```bash
# Install a plan
git-forest plans install config/plans/engineering-excellence/dependency-hygiene.yaml

# List installed plans
git-forest plans list

# Reconcile a plan (generate plants)
git-forest plan dependency-hygiene reconcile
```
