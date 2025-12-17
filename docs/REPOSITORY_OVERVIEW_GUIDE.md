# Repository Overview Plan - Team Lead Guide

## Overview

The **repository-overview** plan is a comprehensive meta-governance plan designed specifically for team leads who need a complete understanding of their git-forest repository's current state. This plan provides a holistic view across all dimensions of the forest by running multiple specialized planners.

## When to Use This Plan

Use the repository-overview plan when you need to:

- **Prepare for Sprint Planning** - Understand current state before planning the next sprint
- **Generate Status Reports** - Create comprehensive reports for team reviews or stakeholders
- **Conduct Team Reviews** - Share a complete picture during standups or retrospectives
- **Monitor Project Health** - Set up continuous monitoring in CI/CD pipelines
- **Onboard New Team Members** - Give new team members a complete picture of the codebase
- **Make Strategic Decisions** - Have all the data you need for technical debt prioritization

## What This Plan Provides

The repository-overview plan analyzes **10 key dimensions** of your repository:

### 1. Repository Structure & Organization
- Directory organization and layout
- File naming conventions
- Module boundaries and separation
- Configuration consistency
- Build system structure

### 2. Code Health & Quality
- Code complexity metrics
- Duplication levels
- Maintainability index
- Technical debt hotspots
- Code smells and anti-patterns

### 3. Plan Catalog Status
- Plan category coverage
- Plan completeness
- Plan metadata quality
- Plan interdependencies
- Missing plan areas

### 4. Plant Lifecycle Analysis
- Plants by status (planned, planted, growing, harvestable, harvested)
- Age distribution
- Completion trends
- Stuck or blocked plants
- Success rate metrics

### 5. Planter Capacity & Workload
- Active planters
- Workload distribution per planter
- Capability coverage
- Performance metrics
- Capacity constraints

### 6. Documentation Completeness
- API documentation coverage
- User guides availability
- Architecture documentation
- Code comment quality
- Documentation consistency with code

### 7. CI/CD Pipeline Health
- Build success rate
- Test pass rate
- Pipeline duration trends
- Failure frequency
- Reliability metrics

### 8. Test Coverage
- Line coverage percentages
- Branch coverage
- Critical path coverage
- Test distribution (unit, integration, e2e)
- Test quality metrics

### 9. Dependency Health
- Outdated dependencies
- Security vulnerabilities
- License compliance
- Dependency tree health
- Update urgency assessment

### 10. Technical Debt Inventory
- Debt types and categories
- Debt locations in codebase
- Impact assessment
- Resolution effort estimates
- Debt trends over time

## Installation & Usage

### Quick Start

```bash
# 1. Initialize forest (if not already done)
git-forest init

# 2. Install the repository overview plan
git-forest plans install config/plans/meta-governance/repository-overview.yaml

# 3. Reconcile the plan to generate analysis plants
git-forest plan repository-overview reconcile

# 4. View all generated plants
git-forest plants list --plan repository-overview

# 5. View detailed status
git-forest status
```

### Generate a Comprehensive Overview Report

```bash
# Reconcile to ensure latest analysis
git-forest plan repository-overview reconcile

# Generate the overview report plant
git-forest plant repository-overview:generate-repository-overview show

# Export to JSON for processing
git-forest plant repository-overview:generate-repository-overview show --json > overview-report.json
```

### Update Status Dashboard

```bash
# Generate interactive dashboard
git-forest plant repository-overview:update-status-dashboard show

# Export dashboard data
git-forest plant repository-overview:update-status-dashboard show --json > dashboard.json
```

### View Specific Analysis Dimensions

```bash
# View repository structure analysis
git-forest plant repository-overview:analyze-repository-structure show

# View code health assessment
git-forest plant repository-overview:assess-code-health show

# View plan catalog evaluation
git-forest plant repository-overview:evaluate-plan-catalog show

# View plant lifecycle analysis
git-forest plant repository-overview:analyze-plant-lifecycle show

# View planter capacity evaluation
git-forest plant repository-overview:evaluate-planter-capacity show

# View documentation completeness
git-forest plant repository-overview:check-documentation-completeness show

# View CI pipeline health
git-forest plant repository-overview:assess-ci-pipeline-health show

# View test coverage
git-forest plant repository-overview:evaluate-test-coverage show

# View dependency health
git-forest plant repository-overview:analyze-dependency-health show

# View technical debt inventory
git-forest plant repository-overview:inventory-technical-debt show
```

## Usage Scenarios

### Scenario 1: Sprint Planning Preparation

As a team lead, you need to prepare for the upcoming sprint planning session:

```bash
# Run the overview plan
git-forest plans install config/plans/meta-governance/repository-overview.yaml
git-forest plan repository-overview reconcile

# Get metrics summary
git-forest plants list --plan repository-overview --json > sprint-prep-metrics.json

# Review key areas
git-forest plant repository-overview:analyze-plant-lifecycle show
git-forest plant repository-overview:inventory-technical-debt show
```

**Expected Outcome:** You'll have a complete picture of:
- What work is in progress (plants by status)
- Where technical debt is concentrated
- Which areas need attention in the next sprint
- Planter capacity for new work

### Scenario 2: Team Status Review

You need to provide a status update during a team standup or review:

```bash
# Generate latest overview
git-forest plan repository-overview reconcile

# Create status report
git-forest plant repository-overview:generate-repository-overview show

# Get specific metrics
git-forest plant repository-overview:assess-code-health show
git-forest plant repository-overview:assess-ci-pipeline-health show
```

**Expected Outcome:** A comprehensive status report covering all key areas that you can share with the team.

### Scenario 3: Executive Summary for Stakeholders

You need to create a high-level summary for stakeholders or management:

```bash
# Generate overview with focus on metrics
git-forest plan repository-overview reconcile
git-forest plant repository-overview:update-status-dashboard show --json

# Extract key metrics
# - repository_health_score
# - code_quality_score
# - test_coverage_percentage
# - ci_success_rate
# - technical_debt_ratio
```

**Expected Outcome:** A data-driven executive summary with key health indicators.

### Scenario 4: Continuous Monitoring in CI

Set up the repository-overview plan to run continuously in your CI/CD pipeline:

```yaml
# Example GitHub Actions workflow
name: Forest Overview

on:
  push:
    branches: [ main ]
  schedule:
    - cron: '0 0 * * *'  # Daily at midnight

jobs:
  overview:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup git-forest
        run: dotnet tool install --global git-forest
      
      - name: Initialize and run overview
        run: |
          git-forest init
          git-forest plans install config/plans/meta-governance/repository-overview.yaml
          git-forest plan repository-overview reconcile
          git-forest plants list --json > overview-status.json
      
      - name: Upload results
        uses: actions/upload-artifact@v3
        with:
          name: overview-report
          path: overview-status.json
```

**Expected Outcome:** Daily automated overview reports tracking repository health over time.

## Key Planners in Detail

### repository-structure-analyzer
Analyzes the overall structure and organization of your repository. Identifies:
- Directory organization issues
- File naming inconsistencies
- Module boundary violations
- Configuration drift
- Build system complexity

### code-health-overview
Provides high-level assessment of code quality. Measures:
- Cyclomatic complexity
- Code duplication
- Maintainability index
- Technical debt concentration
- Code smell frequency

### plan-catalog-status
Evaluates your plan catalog for completeness. Checks:
- Coverage across all categories
- Plan metadata completeness
- Plan version consistency
- Missing plan opportunities
- Plan interdependency conflicts

### plant-lifecycle-analyzer
Analyzes the health of your plant ecosystem. Tracks:
- Status distribution
- Age of plants in each status
- Completion velocity
- Blocked plants
- Success rate trends

### planter-capacity-analyzer
Evaluates planter workload and capabilities. Monitors:
- Active planters
- Work distribution
- Capability coverage
- Performance metrics
- Capacity bottlenecks

### documentation-completeness
Assesses documentation coverage and quality. Evaluates:
- API documentation
- User guides
- Architecture docs
- Code comments
- Doc-code consistency

### ci-pipeline-health
Monitors CI/CD pipeline reliability. Tracks:
- Build success rate
- Test pass rate
- Pipeline duration
- Failure patterns
- Stability trends

### test-coverage-overview
Comprehensive test coverage analysis. Measures:
- Line coverage
- Branch coverage
- Critical path coverage
- Test distribution
- Test effectiveness

### dependency-health-summary
Summarizes dependency status and security. Identifies:
- Outdated dependencies
- Security vulnerabilities
- License issues
- Dependency tree complexity
- Update priorities

### technical-debt-inventory
Inventories and categorizes technical debt. Maps:
- Debt types
- Debt locations
- Impact assessment
- Resolution effort
- Debt accumulation trends

## Key Planters in Detail

### overview-reporter
Generates comprehensive overview reports from all planner outputs. Creates:
- Markdown reports for human reading
- JSON metrics for automation
- HTML dashboards for visualization
- Trend analysis over time

### status-dashboard-generator
Creates interactive status dashboards with real-time metrics:
- Health score gauges
- Plant status charts
- Code quality trends
- Planter workload visualizations
- CI pipeline timelines
- Test coverage gauges
- Dependency security alerts
- Technical debt heatmaps

### metrics-aggregator
Aggregates metrics from multiple planners into unified reports:
- Metric collection from all sources
- Data normalization
- Statistical analysis
- Trend detection
- Comparative reports

## Metrics & Outputs

The repository-overview plan tracks these key metrics:

- **repository_health_score** (target: 80+) - Overall repository health
- **active_plans_count** (monitored) - Number of installed plans
- **plant_completion_rate** (target: 70%+) - Percentage of completed work
- **code_quality_score** (target: 85+) - Aggregated code quality
- **documentation_coverage** (target: 90%+) - Documentation completeness
- **ci_pipeline_health** (target: 95%+) - CI/CD success rate
- **technical_debt_ratio** (monitored) - Technical debt percentage

## Safety & Policies

The repository-overview plan is designed with safety in mind:

- **Read-only operations** - Never modifies code or configuration
- **Advisory only** - All outputs are informational
- **CI-safe** - Can run in automated pipelines
- **Deterministic** - Same inputs always produce same outputs
- **Idempotent** - Running multiple times is safe

## Integration with Other Plans

The repository-overview plan complements other meta-governance plans:

- **forest-maintenance** - Use overview to identify what maintenance needs
- **forest-governance** - Use overview metrics to inform governance policies
- **risk-aware-planning** - Use overview to identify high-risk areas
- **harvest-discipline** - Use overview to track completion progress

## Tips for Team Leads

1. **Run Regularly** - Schedule the overview plan to run daily or weekly for trend tracking
2. **Focus on Trends** - Single snapshots are useful, but trends over time are more valuable
3. **Share with Team** - Make overview results visible to the entire team
4. **Use for Planning** - Let overview data drive sprint planning and prioritization
5. **Monitor Key Metrics** - Track the health score and other key metrics over time
6. **Identify Patterns** - Look for patterns in plant lifecycle and planter workload
7. **Address Bottlenecks** - Use overview to identify and address bottlenecks early
8. **Celebrate Progress** - Use metrics to celebrate improvements with the team

## Customization

You can customize the repository-overview plan by:

1. **Adjusting metric targets** - Edit the `metrics` section in the plan YAML
2. **Adding custom planners** - Extend with your own analysis planners
3. **Modifying plant templates** - Customize the types of overview reports generated
4. **Changing execution mode** - Adjust policies to fit your workflow
5. **Setting resource limits** - Tune resource usage for your environment

## Troubleshooting

### Plan installation fails
```bash
# Ensure you're in a git repository
git status

# Ensure forest is initialized
git-forest init
```

### Reconciliation produces no plants
```bash
# Check that the plan is installed
git-forest plans list

# Ensure planners are properly configured
cat config/plans/meta-governance/repository-overview.yaml
```

### Plants show errors
```bash
# View plant details to see specific errors
git-forest plant repository-overview:<plant-name> show

# Check planter logs
ls -la .git-forest/logs/
```

## Further Reading

- [Plans Catalog](../config/plans/README.md) - Complete catalog of all available plans
- [CLI Documentation](../CLI.md) - Full CLI command reference
- [Forest Maintenance Plan](../config/plans/meta-governance/forest-maintenance.yaml) - Complementary maintenance plan
- [Product Goal](../PRODUCT_GOAL.md) - Understanding git-forest's vision

## Support

For questions or issues with the repository-overview plan:
1. Check the [GitHub Issues](https://github.com/tweakch/git-forest/issues)
2. Review the [CLI documentation](../CLI.md)
3. Consult the [plan catalog](../config/plans/README.md)
