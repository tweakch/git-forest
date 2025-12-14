# planner

Add or view a planner (organizer/manager).

## Usage

```bash
# Add a new planner
git-forest planner --name <name> --email <email> --role <role>

# View current planner
git-forest planner
```

## Options

- `--name` - Name of the planner
- `--email` - Email address of the planner
- `--role` - Role of the planner (e.g., organizer, manager, lead)

## Description

The `planner` command manages planners - organizers and managers who coordinate the forest. Planners have oversight of the entire forest and its strategy.

## Examples

```bash
# Add a new planner
$ git-forest planner --name "Alice Manager" --email alice@example.com --role "Tech Lead"
Adding planner 'Alice Manager' (alice@example.com) with role 'Tech Lead'...
Planner added successfully!

# View current planner
$ git-forest planner
Current planner information:
Name: Alice Manager
Email: alice@example.com
Role: Tech Lead
```
