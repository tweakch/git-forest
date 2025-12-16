# init

Initialize a new forest in the current directory.

## Usage

```bash
git-forest init [--force] [--dir <dir>] [--json]
```

## Options

- `--dir <dir>` - Directory for forest state (default: `.git-forest`)
- `--force` - Force re-initialization (currently a no-op; init is idempotent)
- `--json` - Output in JSON format (global option)

## Description

The `init` command creates a new forest configuration in the current directory. This sets up the necessary structure for managing multiple git repositories as a collection.

## What it does

- Creates a `.git-forest` directory
- Initializes forest configuration
- Sets up default settings

## Example

```bash
$ git-forest init
initialized (.git-forest)
```
