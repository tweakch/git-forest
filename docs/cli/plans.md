# plans

Manage plans.

## Usage

```bash
git-forest plans list [--json]
git-forest plans install <source> [--json]
```

## Subcommands

### `plans list`

List installed plans.

```bash
git-forest plans list
```

### `plans install`

Install a plan from a **GitHub slug**, **URL**, or **local path**.

```bash
git-forest plans install <source>
```

## Options

- `--json` - Output in JSON format (global option)

## Notes

- Requires a forest to be initialized (run `git-forest init` first).

