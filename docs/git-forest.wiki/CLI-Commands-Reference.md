# CLI Commands Reference

Complete reference for all git-forest CLI commands. For detailed CLI specification, see the [CLI.md in the repository](https://github.com/tweakch/git-forest/blob/main/CLI.md).

## Command Structure

```text
git-forest <command> [<subcommand>] [options]
```

Or use the shorter alias:
```text
gf <command> [<subcommand>] [options]
```

---

## Global Options

All commands support these global options:

| Option | Description |
|--------|-------------|
| `--json` | Output in JSON format for automation |
| `--help` | Display help information |
| `--version` | Display version information |

---

## Core Commands

### `init` - Initialize Forest

Initialize a git-forest in the current repository.

```bash
git-forest init [--force] [--dir .git-forest]
```

**Options:**
- `--force` - Reinitialize even if forest already exists
- `--dir <path>` - Specify custom forest directory (default: `.git-forest`)

**Examples:**
```bash
# Basic initialization
git-forest init

# Force reinit
git-forest init --force

# Custom directory
git-forest init --dir .my-forest
```

**Exit Codes:**
- `0` - Success
- `30` - Git operation failed

---

### `status` - Show Forest Status

Display current forest status.

```bash
git-forest status [--json]
```

**Output:**
```text
Forest: initialized  Repo: origin/main
Plans: 1 installed (sample@v1.2.0)
Plants: planned 15 | planted 1 | growing 0 | harvestable 0 | harvested 0
Planters: 24 available | 2 active
Lock: free
Hints: gf plants list --status planned
```

**JSON Output:**
```bash
git-forest status --json
```

**Exit Codes:**
- `0` - Success
- `10` - Forest not initialized

---

### `evolve` - Evolve Forest (scaffold)

Placeholder for the evolve workflow.

```bash
git-forest evolve [--json]
```

**Output:**
```text
Evolve workflow not implemented yet.
```

**Exit Codes:**
- `0` - Success

---

### `config` - Configuration Management

Manage forest configuration.

#### Show Configuration

```bash
git-forest config show [--effective] [--json]
```

**Options:**
- `--effective` - Show effective configuration (all layers merged)

#### Get Configuration Value

```bash
git-forest config get <path> [--json]
```

**Examples:**
```bash
git-forest config get git.remote
git-forest config get branches.template
git-forest config get llm.profile.default
```

#### Set Configuration Value

```bash
git-forest config set <path> <value> [--scope user|repo]
```

**Scopes:**
- `user` - User-level config (`~/.git-forest/config.yaml`)
- `repo` - Repo-level config (`.git-forest/config.yaml`) [default]

**Examples:**
```bash
git-forest config set git.remote origin
git-forest config set branches.template "{planter}/{plantSlug}"
git-forest config set execution.mode auto --scope user
```

#### Unset Configuration Value

```bash
git-forest config unset <path> [--scope user|repo]
```

**Examples:**
```bash
git-forest config unset git.remote
git-forest config unset execution.mode --scope user
```

**Common Configuration Paths:**
- `git.remote` - Git remote name (default: `origin`)
- `git.baseBranch` - Base branch name (default: `main`)
- `branches.template` - Branch naming template
- `llm.profile.default` - Default LLM profile
- `execution.mode` - Execution mode (`manual` | `auto`)
- `locks.timeoutSeconds` - Lock timeout in seconds

---

## Plans Commands

### `plans list` - List Installed Plans

List all installed plans.

```bash
git-forest plans list [--json]
```

**Output:**
```text
Plan ID              Version  Name                 Category
dependency-hygiene   1.0.0    Dependency Hygiene   engineering-excellence
developer-experience 1.0.0    Developer Experience team-process
```

---

### `plans install` - Install Plan

Install a plan from various sources.

```bash
git-forest plans install <source> [--id <plan-id>] [--ref <tag|branch|sha>] [--force] [--json]
```

**Sources:**
- GitHub: `tweakch/git-forest-plans/sample`
- URL: `https://github.com/...`
- Local path: `./local/plan` or `config/plans/...`

**Options:**
- `--id <plan-id>` - Specify custom plan ID
- `--ref <ref>` - Git reference (tag, branch, or SHA)
- `--force` - Force reinstall

**Examples:**
```bash
# Install from catalog
git-forest plans install config/plans/engineering-excellence/dependency-hygiene.yaml

# Install from GitHub
git-forest plans install tweakch/git-forest-plans/sample

# Install from URL
git-forest plans install https://github.com/user/repo/raw/main/plan.yaml

# Install from local path
git-forest plans install ./custom-plans/my-plan.yaml

# Install specific version
git-forest plans install tweakch/git-forest-plans/sample --ref v1.2.0
```

**Exit Codes:**
- `0` - Success
- `20` - Schema validation failed
- `30` - Git operation failed

---

### `plans show` - Show Plan Details

Display details of an installed plan.

```bash
git-forest plans show <plan-id> [--json]
```

**Examples:**
```bash
git-forest plans show dependency-hygiene
git-forest plans show dependency-hygiene --json
```

**Exit Codes:**
- `0` - Success
- `11` - Plan not found

---

### `plans remove` - Remove Plan

Remove an installed plan.

```bash
git-forest plans remove <plan-id> [--purge-plants] [--yes]
```

**Options:**
- `--purge-plants` - Also remove all plants from this plan
- `--yes` - Skip confirmation prompt

**Examples:**
```bash
# Remove plan (keeps plants)
git-forest plans remove dependency-hygiene

# Remove plan and plants
git-forest plans remove dependency-hygiene --purge-plants --yes
```

**Exit Codes:**
- `0` - Success
- `11` - Plan not found

---

### `plan reconcile` - Reconcile Plan

Run planners and reconcile desired state.

```bash
git-forest plan <plan-id> [reconcile] [--update] [--forum ai|file] [--only <scope>] [--dry-run] [--json]
```

**Options:**
- `--update` - Update plan-owned fields even if unchanged
- `--forum ai|file` - Forum selection (overrides config)
- `--only <scope>` - Reconcile only specific scope
- `--dry-run` - Show what would change without applying
- `--json` - JSON output

**Examples:**
```bash
# Reconcile with default forum
git-forest plan dependency-hygiene reconcile

# Reconcile with file forum
git-forest plan dependency-hygiene reconcile --forum file

# Reconcile with AI forum
git-forest plan dependency-hygiene reconcile --forum ai

# Dry-run
git-forest plan dependency-hygiene reconcile --dry-run

# Reconcile specific scope
git-forest plan dependency-hygiene reconcile --only backend
```

**Output:**
```text
Reconciling plan 'dependency-hygiene@v1.0.0'...
Planners: +2 ~0 -0
Planters: +3 ~1 -0
Plants:   +4 ~6 -0 (archived 0)
done
```

**Exit Codes:**
- `0` - Success
- `11` - Plan not found
- `23` - Lock timeout

---

### `plan diff` - Show Plan Diff

Show what reconciliation would change.

```bash
git-forest plan <plan-id> diff [--only <scope>] [--json]
```

**Examples:**
```bash
git-forest plan dependency-hygiene diff
git-forest plan dependency-hygiene diff --json
```

---

## Plants Commands

### `plants list` - List Plants

List plants with filtering.

```bash
git-forest plants list [--status <status>] [--plan <plan-id>] [--planter <planter-id>] [--scope <scope>] [--search <text>] [--json]
```

**Status Options:**
- `planned` - Not yet started
- `planted` - Assigned and in progress
- `growing` - Changes being applied
- `harvestable` - Ready for integration
- `harvested` - Completed
- `archived` - Removed from active work

**Examples:**
```bash
# List all plants
git-forest plants list

# Filter by status
git-forest plants list --status planned

# Filter by plan
git-forest plants list --plan dependency-hygiene

# Filter by planter
git-forest plants list --planter dependency-cleaner

# Search
git-forest plants list --search "remove unused"

# Combine filters
git-forest plants list --plan dependency-hygiene --status planned

# JSON output
git-forest plants list --json
```

**Output:**
```text
Key Status Planter Title
dependency-hygiene:remove-unused planned - Remove unused packages
dependency-hygiene:update-outdated planted dep-cleaner Update outdated dependencies
```

---

### `plant show` - Show Plant Details

Display detailed information about a plant.

```bash
git-forest plant <selector> [show] [--json]
```

**Selectors:**
- Full key: `dependency-hygiene:remove-unused`
- Short form: `P01` (rendered index)
- Slug: `remove-unused` (if unambiguous)

**Examples:**
```bash
# By full key
git-forest plant dependency-hygiene:remove-unused show

# By short form
git-forest plant P01 show

# By slug (if unique)
git-forest plant remove-unused show

# JSON output
git-forest plant P01 show --json
```

**Exit Codes:**
- `0` - Success
- `12` - Plant not found / ambiguous selector

---

### `plant set` - Set Plant Field

Set user-owned plant fields.

```bash
git-forest plant <selector> set <path> <value>
```

**Common Paths:**
- `priority` - `low` | `medium` | `high`
- `notes` - User notes (string)

**Examples:**
```bash
git-forest plant P01 set priority high
git-forest plant P01 set notes "Focus on backend dependencies first"
```

---

### `plant unset` - Unset Plant Field

Remove user-owned field value.

```bash
git-forest plant <selector> unset <path>
```

**Examples:**
```bash
git-forest plant P01 unset priority
git-forest plant P01 unset notes
```

---

### `plant assign` - Assign Planter

Assign a planter to a plant.

```bash
git-forest plant <selector> assign <planter-id> [--capacity 1]
```

**Examples:**
```bash
git-forest plant P01 assign dependency-cleaner
git-forest plant P01 assign dependency-cleaner --capacity 2
```

---

### `plant unassign` - Unassign Planter

Remove planter assignment from a plant.

```bash
git-forest plant <selector> unassign <planter-id>
```

**Examples:**
```bash
git-forest plant P01 unassign dependency-cleaner
```

---

### `plant history` - View History

Show plant history log.

```bash
git-forest plant <selector> history [--json]
```

---

### `plant logs` - View Logs

Show plant logs.

```bash
git-forest plant <selector> logs [--tail 200]
```

---

### `plant candidates` - List Candidates

List candidate diffs/PRs for a plant.

```bash
git-forest plant <selector> candidates list [--json]
```

---

### `plant branches` - List Branches

List branches associated with a plant.

```bash
git-forest plant <selector> branches list [--json]
```

---

### `plant harvest` - Harvest Plant

Mark a plant as harvested (completed).

```bash
git-forest plant <selector> harvest [--message <text>]
```

**Examples:**
```bash
git-forest plant P01 harvest
git-forest plant P01 harvest --message "Removed 15 unused packages"
```

---

## Planters Commands

### `planters list` - List Planters

List available planters.

```bash
git-forest planters list [--builtin|--custom] [--origin plan|user] [--json]
```

**Options:**
- `--builtin` - Show only built-in planters
- `--custom` - Show only custom planters
- `--origin plan|user` - Filter by origin

**Examples:**
```bash
git-forest planters list
git-forest planters list --builtin
git-forest planters list --origin plan
```

---

### `planter show` - Show Planter Details

Display planter information.

```bash
git-forest planter <planter-id> show [--json]
```

**Examples:**
```bash
git-forest planter dependency-cleaner show
git-forest planter dependency-cleaner show --json
```

**Exit Codes:**
- `0` - Success
- `13` - Planter not found

---

### `planter assign` - Assign to Plant

Assign planter to a plant (planter-centric).

```bash
git-forest planter <planter-id> assign <selector> [--capacity 1]
```

**Examples:**
```bash
git-forest planter dependency-cleaner assign P01
git-forest planter dependency-cleaner assign dependency-hygiene:remove-unused
```

---

### `planter unassign` - Unassign from Plant

Remove planter from a plant.

```bash
git-forest planter <planter-id> unassign <selector>
```

---

### `planter plant` - Plant with Branch

Assign planter and create branch.

```bash
git-forest planter <planter-id> plant <selector> [--branch auto|<name>] [--yes] [--dry-run]
```

**Options:**
- `--branch auto` - Use template from config
- `--branch <name>` - Specify branch name
- `--yes` - Skip confirmation
- `--dry-run` - Show what would happen

**Examples:**
```bash
# Auto branch creation
git-forest planter dependency-cleaner plant P01 --branch auto --yes

# Custom branch name
git-forest planter dependency-cleaner plant P01 --branch feature/cleanup-deps --yes
```

---

### `planter grow` - Grow Plant

Execute planter to propose/apply changes.

```bash
git-forest planter <planter-id> grow <selector> [--mode propose|apply] [--max-diffs 3] [--risk low|medium|high] [--dry-run] [--json]
```

**Modes:**
- `propose` - Create candidate diffs only (safe)
- `apply` - May open PRs / apply patches (requires permission)

**Options:**
- `--max-diffs <n>` - Maximum candidate diffs to generate
- `--risk <level>` - Risk tolerance level
- `--dry-run` - Show what would happen

**Examples:**
```bash
# Propose changes (safe)
git-forest planter dependency-cleaner grow P01 --mode propose

# Apply changes (if permitted)
git-forest planter dependency-cleaner grow P01 --mode apply

# With risk limit
git-forest planter dependency-cleaner grow P01 --mode propose --risk low

# Dry run
git-forest planter dependency-cleaner grow P01 --mode propose --dry-run
```

**Exit Codes:**
- `0` - Success
- `40` - Execution not permitted by policy

---

## Planners Commands

### `planners list` - List Planners

List available planners.

```bash
git-forest planners list [--plan <plan-id>] [--json]
```

**Examples:**
```bash
# All planners
git-forest planners list

# Planners for specific plan
git-forest planners list --plan dependency-hygiene
```

---

### `planner run` - Run Planner

Execute a specific planner.

```bash
git-forest planner <planner-id> run --plan <plan-id> [--only <scope>] [--dry-run] [--json]
```

**Examples:**
```bash
git-forest planner code-analyzer run --plan dependency-hygiene
git-forest planner code-analyzer run --plan dependency-hygiene --only backend
```

---

## Exit Codes Reference

git-forest uses stable exit codes for automation:

| Code | Meaning |
|------|---------|
| `0` | Success |
| `2` | Invalid arguments / parse error |
| `10` | Forest not initialized |
| `11` | Plan not found |
| `12` | Plant not found / ambiguous selector |
| `13` | Planter not found |
| `20` | Schema validation failed |
| `23` | Lock timeout / busy |
| `30` | Git operation failed |
| `40` | Execution not permitted by policy |

See [Exit Codes Reference](Exit-Codes-Reference) for complete details.

---

## Next Steps

- [Working with Plans](Working-with-Plans) - Detailed plan management
- [Managing Plants](Managing-Plants) - Plant lifecycle guide
- [Configuration Reference](Configuration-Reference) - All config options
- [Quick Start Guide](Quick-Start-Guide) - Get started quickly

---

## Support

For questions or issues:
- [Common Issues](Common-Issues)
- [FAQ](FAQ)
- [GitHub Issues](https://github.com/tweakch/git-forest/issues)
