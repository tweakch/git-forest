# git-forest

A .NET Aspire-based CLI for managing collections of git repositories as a unified forest.

## Overview

git-forest is a powerful command-line tool that helps you manage multiple git repositories as a cohesive unit called a "forest". It provides an intuitive way to organize, track, and coordinate work across multiple related repositories.

Design goals: **idempotent**, **deterministic IDs**, **reconcile desired state**, **automation-friendly output**, **clear ownership**, **safe concurrency**.

## Core Concepts

### 📦 Plan
**Plans** are versioned packages that define the desired forest intent. A plan includes:
- Planners (generators)
- Planters (executors)
- Plant templates
- Scopes and policies

Plans can be sourced from GitHub, URLs, or local paths.

#### Pre-defined Plans
git-forest includes a comprehensive catalog of pre-defined plans organized by category:
- **Engineering Excellence** - Code health, sustainability, and long-term velocity
- **Quality & Reliability** - Correctness, confidence, and operability
- **Performance & Scalability** - Speed, predictability, and scale
- **Security & Compliance** - Preventative security and regulatory compliance
- **Team & Process** - Developer experience and workflow optimization
- **Documentation & Knowledge** - Living documentation and knowledge management
- **Evolution & Migration** - Strategic change and system evolution
- **AI-Native** - AI-assisted development and automation
- **Meta/Governance** - Plan management and resource governance
- **Experimental** - Innovative approaches to code quality

See [config/plans/README.md](config/plans/README.md) for the complete catalog with 54 pre-defined plans.

#### Team Lead Quick Start
For team leads wanting a comprehensive overview of their repository status:

```bash
# Install the repository overview plan
git-forest plans install config/plans/meta-governance/repository-overview.yaml

# Generate comprehensive overview
git-forest plan repository-overview reconcile

# View the overview report
git-forest plants list --plan repository-overview
```

The repository-overview plan provides:
- Repository structure and organization analysis
- Code health and quality metrics
- Plan catalog status and coverage
- Plant lifecycle and status distribution
- Planter capabilities and workload
- Documentation completeness assessment
- CI/CD pipeline health
- Test coverage overview
- Dependency health summary
- Technical debt inventory

For a complete guide, see [docs/REPOSITORY_OVERVIEW_GUIDE.md](docs/REPOSITORY_OVERVIEW_GUIDE.md).

### 🌱 Plant
**Plants** are concrete work items with stable keys and lifecycle facts. Each plant has:
- Stable key format: `planId:plantSlug` (e.g., `sample:backend-memory-hygiene`)
- Status lifecycle: planned → planted → growing → harvestable → harvested
- Assignments to planters
- Branch tracking
- Candidate diffs and harvest results

### 🤖 Planter
**Planters** are executor personas (agents) that propose diffs/PRs for plants under policies. They can be:
- Built-in planters (provided by plans)
- Custom planters (user-defined)

Planters operate with capacity limits and follow execution modes (propose vs apply).

### 🧠 Planner
**Planners** are deterministic generators that produce a **desired set** of Plants from a Plan + repo context. Same plan + repo context always produces the same plant keys.

### 🌲 Forest
The **Forest** is the repo-local state stored under `.git-forest/` with optional user config.

## Command Alias

The CLI can be invoked as `git-forest` (default) or `gf` (alias).

## Installation

Install as a .NET Global Tool:

```bash
dotnet tool install --global git-forest
```

Verify:

```bash
git-forest --version
```

### Web UI (Optional)

For a browser-based interface, you can run the Blazor Web App:

```bash
cd src/GitForest.Web
dotnet run
```

Then open `http://localhost:5000` in your browser. The web UI provides a visual interface for browsing plans, installing them, and managing your forest. See [src/GitForest.Web/README.md](src/GitForest.Web/README.md) for more details.

### `gf` alias (optional)

`.NET tools` expose a single command name (`git-forest`). If you want `gf`, add a shell alias:

**PowerShell (current session):**

```powershell
Set-Alias gf git-forest
```

**bash/zsh:**

```bash
alias gf='git-forest'
```

## Quick Start

```bash
# Initialize a forest in current git repo
git-forest init

# Check forest status
git-forest status

# Get status in JSON format
git-forest status --json

# Install a plan
git-forest plans install tweakch/git-forest-plans/sample

# List installed plans
git-forest plans list

# Reconcile a plan (create plants from plan)
git-forest plan sample reconcile

# List all plants
git-forest plants list

# Show specific plant details
git-forest plant sample:backend-hygiene show

# List planters
git-forest planters list

# Assign planter to plant
git-forest plant sample:backend-hygiene assign backend-planter

# Run a planner
git-forest planner code-analyzer run --plan sample
```

## Plans Catalog

git-forest includes 54 pre-defined plans across 10 categories to help you improve your codebase systematically:

### Using Pre-defined Plans

```bash
# Install a pre-defined plan from the catalog
git-forest plans install config/plans/engineering-excellence/dependency-hygiene.yaml

# Or install from a specific category
git-forest plans install config/plans/security-compliance/secret-hygiene.yaml

# Reconcile the plan to generate plants
git-forest plan dependency-hygiene reconcile

# List plants created by the plan
git-forest plants list --plan dependency-hygiene
```

### Plan Categories

1. **Engineering Excellence** (6 plans) - architecture-hardening, dependency-hygiene, api-contract-stability, refactor-hotspots, cyclomatic-reduction, dead-code-elimination
2. **Quality & Reliability** (9 plans) - test-pyramid-balance, mutation-testing, flaky-test-eradication, chaos-readiness, observability-boost, unit-testing-discipline, tdd-enablement, bdd-scenarios, integration-testing-harness
3. **Performance & Scalability** (5 plans) - latency-budgeting, allocation-pressure-reduction, throughput-optimization, io-efficiency, orleans-readiness
4. **Security & Compliance** (5 plans) - threat-modeling, secret-hygiene, authz-consistency, dependency-vulnerability-guard, audit-trail-enforcement
5. **Team & Process** (5 plans) - developer-experience, onboarding-acceleration, scrum-signal, flow-efficiency, knowledge-radiation
6. **Documentation & Knowledge** (4 plans) - living-architecture, decision-recording, public-api-docs, internal-playbooks
7. **Evolution & Migration** (5 plans) - monolith-modularization, legacy-extraction, cloud-readiness, framework-upgrade, orleans-adoption
8. **AI-Native** (5 plans) - semantic-code-map, intent-preservation, regression-scout, memory-guard, self-healing-forest
9. **Meta/Governance** (6 plans) - repository-overview, forest-governance, plan-composition, risk-aware-planning, harvest-discipline, forest-maintenance
10. **Experimental** (4 plans) - code-archeology, intent-drift-detection, complexity-budgeting, entropy-reduction

For detailed descriptions of each plan, see [config/plans/README.md](config/plans/README.md).

## Command Structure

The CLI follows this layout:

```text
git-forest init                    # Initialize forest
git-forest status                  # Show status
git-forest config show             # Configuration

git-forest plans list              # List plans
git-forest plans install <source>  # Install plan
git-forest plan <id> reconcile     # Reconcile plan

git-forest plants list             # List plants
git-forest plant <selector> show   # Show plant

git-forest planters list           # List planters
git-forest planter <id> show       # Show planter

git-forest planners list           # List planners
git-forest planner <id> run        # Run planner
```

### Global Options

All commands support:
- `--json` - Output in JSON format for automation

For detailed documentation on each command, see the [CLI specification](./CLI.md).

## Project Structure

```
git-forest/
├── src/
│   ├── GitForest.Core/      # Core domain models and services
│   └── GitForest.Cli/        # Command-line interface
├── config/                   # Configuration files
├── docs/
│   └── cli/                  # CLI command documentation (legacy)
├── .github/
│   ├── copilot-instructions.md  # GitHub Copilot instructions
│   └── workflows/            # GitHub Actions CI/CD
├── CLI.md                    # CLI specification (v0.2)
└── GitForest.sln            # Solution file
```

## On-Disk Layout

When initialized, git-forest creates a `.git-forest/` directory:

```text
.git-forest/
  forest.yaml              # Forest metadata
  config.yaml              # Configuration
  lock                     # Concurrency lock
  plans/<plan-id>/         # Installed plans
  plants/<planId__slug>/   # Plant state and history
  planters/<planter-id>/   # Planter state
  planners/<planner-id>/   # Planner definitions
  logs/                    # Activity logs
```

## Technology Stack

- **.NET 10.0** - Latest .NET runtime
- **.NET Aspire** - Cloud-native application framework (via NuGet)
- **System.CommandLine** - Modern CLI framework
- **C#** - Primary language

## Exit Codes

For automation, the CLI provides stable exit codes:

- `0` - Success
- `2` - Invalid arguments / parse error
- `10` - Forest not initialized
- `11` - Plan not found
- `12` - Plant not found / ambiguous selector
- `13` - Planter not found
- `20` - Schema validation failed
- `23` - Lock timeout / busy
- `30` - Git operation failed
- `40` - Execution not permitted by policy

## GitHub Actions Integration

git-forest includes automated workflows for continuous plan reconciliation:

### Developer Experience Plan

The `developer-experience-plan.yml` workflow automatically installs and reconciles the developer-experience plan to optimize build times, improve error messages, and streamline development workflows.

**Triggers:**
- **Manual**: Use "Run workflow" in the Actions tab
- **Automatic**: Triggers on changes to `config/plans/team-process/developer-experience.yaml`

**What it does:**
1. Builds the git-forest CLI from source
2. Installs the developer-experience plan from `config/plans/team-process/developer-experience.yaml`
3. Reconciles the plan to generate plants (work items)
4. Uploads a report artifact with generated plants

**To run manually:**
1. Go to the "Actions" tab in GitHub
2. Select "Developer Experience Plan" workflow
3. Click "Run workflow"

The workflow is idempotent and safe to run multiple times. It uses concurrency controls to prevent parallel reconciliation runs.

### Other Automated Plans

- **Forest Self-Inspection** - Runs the forest-maintenance plan weekly to ensure git-forest dogfoods itself

## Contributing

Please follow the guidelines in [.github/copilot-instructions.md](.github/copilot-instructions.md) when contributing.

1. Review existing code patterns
2. Keep changes minimal and focused
3. Follow the CLI specification in CLI.md
4. Update documentation for functional changes
5. Submit a pull request

## License

[Your License Here]

## Support

For issues and questions, please use the GitHub issue tracker.