# git-forest CLI Commands

This directory contains documentation for all git-forest CLI commands.

## Available Commands

- **Global options**
  - `--json`: output in JSON format (available on all commands)

- **Commands**
  - [init](./init.md) - Initialize forest state
  - [status](./status.md) - Show forest status
  - [config](./config.md) - Manage configuration (`config show`)
  - [plans](./plans.md) - Manage plans (`plans list`, `plans install`)
  - [plan](./plan.md) - Manage a specific plan (`plan <id> reconcile`)
  - [plants](./plants.md) - Manage plants (`plants list`)
  - [plant](./plant.md) - Manage a specific plant (`plant <selector> show`)
  - [planters](./planters.md) - Manage planters (`planters list`)
  - [planter](./planter.md) - Manage a specific planter (`planter <id> show`)
  - [planners](./planners.md) - Manage planners (`planners list`)
  - [planner](./planner.md) - Manage a specific planner (`planner <id> run`)

## Getting Started

```bash
# Initialize a new forest
git-forest init

# Check status
git-forest status

# List plants
git-forest plants list

# List installed plans
git-forest plans list
```
