# status

Show the status of the current forest.

## Usage

```bash
git-forest status [--json]
```

## Options

- `--json` - Output in JSON format (global option)

## Description

The `status` command displays information about the current forest, including:

- Installed plan count
- Plant counts (including counts by status)
- Available vs active planters/planners
- Lock status

## Example

```bash
$ git-forest status
Forest: initialized  Repo: origin/main
Plans: 0 installed
Plants: planned 0 | planted 0 | growing 0 | harvestable 0 | harvested 0 | archived 0
Planters: 0 available | 0 active
Planners: 0 available | 0 active
Lock: free
```
