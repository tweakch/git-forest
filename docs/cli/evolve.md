# evolve

Evolve the forest at the planning layer (no execution).

## Usage

```bash
git-forest evolve [--all] [--plan <plan-id>] [--dry-run] [--json]
```

## Options

- `--all` - Evolve all plans in the forest
- `--plan` - Plan identifier to scope evolution
- `--dry-run` - Show what would be done without applying
- `--json` - Output in JSON format (global option)

## Description

Evolve refreshes desired plants in `.git-forest/` without running planters or creating branches.

## Example

```bash
$ git-forest evolve --all
Evolved forest: +4 ~6
```
