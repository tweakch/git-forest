# plant

Manage a specific plant.

## Usage

```bash
git-forest plant <selector> <subcommand> [options] [--json]
```

## Arguments

- `<selector>` - Plant selector (key, slug, or `P01`)

## Options

- `--json` - Output in JSON format (global option)

## Description

The `plant` command is used for operations on a single plant.

## Subcommands

### `plant <selector> show`

Show plant details.

```bash
git-forest plant <selector> show [--json]
```

### `plant <selector> assign <planter-id>`

Assign a planter to a plant.

```bash
git-forest plant <selector> assign <planter-id> [--dry-run] [--json]
```

### `plant <selector> unassign <planter-id>`

Unassign a planter from a plant.

```bash
git-forest plant <selector> unassign <planter-id> [--dry-run] [--json]
```

### `plant <selector> branches list`

List branches recorded for this plant.

```bash
git-forest plant <selector> branches list [--json]
```

### `plant <selector> harvest`

Mark a plant as harvested.

```bash
git-forest plant <selector> harvest [--force] [--dry-run] [--json]
```

### `plant <selector> archive`

Archive a plant.

```bash
git-forest plant <selector> archive [--force] [--dry-run] [--json]
```

## Examples

```bash
# Show a plant (by key/slug/P01)
git-forest plant P01 show

# Assign a planter
git-forest plant P01 assign dx-improver

# Mark as harvested after changes are integrated
git-forest plant P01 harvest
```
