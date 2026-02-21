# planters

Manage planters.

## Usage

```bash
git-forest planters list [--builtin] [--custom] [--json]
git-forest planters plant --all [--plan <plan-id>] [--single] [--reset] [--only-unassigned] [--dry-run] [--json]
```

## Subcommands

### `planters list`

List planters aggregated from installed plans (built-in) and from `.git-forest/planters` (custom).

### `planters plant`

Assign default planters to plants (metadata only).

## Options

- `--builtin` - Show only built-in planters (from installed plans)
- `--custom` - Show only custom planters (from `.git-forest/planters`)
- `--plan <plan-id>` - Plan identifier to scope assignment
- `--all` - Assign default planters for all plants
- `--single` - Assign a single planter per plant (deterministic)
- `--reset` - Overwrite existing assignments
- `--only-unassigned` - Assign only when a plant has no planters
- `--dry-run` - Show what would be done without applying
- `--json` - Output in JSON format (global option)

## Example

```bash
$ git-forest planters list
No planters configured
```

```bash
$ git-forest planters plant --all
Assigned planters for forest: 12 updated
```
