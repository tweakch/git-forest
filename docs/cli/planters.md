# planters

Manage planters.

## Usage

```bash
git-forest planters list [--builtin] [--custom] [--json]
```

## Subcommands

### `planters list`

List planters aggregated from installed plans (built-in) and from `.git-forest/planters` (custom).

## Options

- `--builtin` - Show only built-in planters (from installed plans)
- `--custom` - Show only custom planters (from `.git-forest/planters`)
- `--json` - Output in JSON format (global option)

## Example

```bash
$ git-forest planters list
No planters configured
```
