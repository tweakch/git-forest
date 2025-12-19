# plants

Manage plants.

## Usage

```bash
git-forest plants list [--status <status>] [--plan <plan-id>] [--json]
```

## Subcommands

### `plants list`

List plants.

## Options

- `--status <status>` - Filter by status (`planned|planted|growing|harvestable|harvested|archived`)
- `--plan <plan-id>` - Filter by plan ID
- `--json` - Output in JSON format (global option)

## Example

```bash
$ git-forest plants list
No plants found
```
