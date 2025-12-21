# planners

Manage planners.

## Usage

```bash
git-forest planners list [--plan <plan-id>] [--json]
git-forest planners plan --all [--plan <plan-id>] [--planner <planner-id>] [--reconcile] [--dry-run] [--json]
```

## Subcommands

### `planners list`

List planners aggregated from installed plans.

### `planners plan`

Run planners to refresh desired plants (planning only).

## Options

- `--plan <plan-id>` - Filter by plan ID
- `--planner <planner-id>` - Filter by planner ID (plan subcommand)
- `--all` - Plan across all installed plans
- `--reconcile` - Run reconcile after planning
- `--dry-run` - Show what would be done without applying
- `--json` - Output in JSON format (global option)

## Example

```bash
$ git-forest planners list
No planners configured
```

```bash
$ git-forest planners plan --all
Planned forest: +4 ~6
```
