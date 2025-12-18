# planter

Manage a specific planter.

## Usage

```bash
git-forest planter <planter-id> <subcommand> [options] [--json]
```

## Arguments

- `<planter-id>` - Planter identifier

## Options

- `--json` - Output in JSON format (global option)

## Description

The `planter` command is used for operations on a single planter.

## Subcommands

### `planter <planter-id> show`

Show planter details.

```bash
git-forest planter <planter-id> show [--json]
```

### `planter <planter-id> plant <selector>`

Assign a planter to a plant and create/check out a branch.

```bash
git-forest planter <planter-id> plant <selector> [--branch auto|<name>] [--yes] [--dry-run] [--json]
```

### `planter <planter-id> grow <selector>`

Grow a plant (propose or apply changes). After growing, the plant is typically `harvestable`.

```bash
git-forest planter <planter-id> grow <selector> [--mode propose|apply] [--dry-run] [--json]
```

## Examples

```bash
# Show a planter
git-forest planter dx-improver show

# Plant a work item (auto branch; requires --yes for branch checkout/creation)
git-forest planter dx-improver plant P01 --branch auto --yes

# Grow it in proposal mode
git-forest planter dx-improver grow P01 --mode propose
```
