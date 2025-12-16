# plan

Manage a specific plan.

## Usage

```bash
git-forest plan <plan-id> reconcile [--update] [--dry-run] [--json]
```

## Arguments

- `<plan-id>` - Plan identifier

## Subcommands

### `plan <plan-id> reconcile`

Reconcile a plan to its desired state.

## Options

- `--dry-run` - Show what would be done without applying
- `--update` - Update plan before reconciling (currently not implemented)
- `--json` - Output in JSON format (global option)

## Notes

- Requires the plan to be installed (see `git-forest plans install ...`).

