# planner

Manage a specific planner.

## Usage

```bash
git-forest planner <planner-id> run --plan <plan-id> [--json]
```

## Arguments

- `<planner-id>` - Planner identifier

## Options

- `--plan <plan-id>` - Plan ID to run against (required)
- `--json` - Output in JSON format (global option)

## Description

The `planner` command is used to run a planner against a specific plan.

## Examples

```bash
$ git-forest planner my-planner run --plan my-plan
Running planner 'my-planner' for plan 'my-plan'...
done
```
