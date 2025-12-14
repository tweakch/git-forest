# Next Steps to MVP

This document outlines the high-level tasks required to move `git-forest` from its current state (a well-defined CLI skeleton) to a Minimum Viable Product (MVP).

The current implementation consists of CLI commands that parse arguments but execute no logic, returning only hardcoded example output. The MVP will focus on implementing the core end-to-end workflow: **initializing a forest, installing a plan, reconciling it to create plants, and viewing the resulting state.**

## MVP Milestone: A Functional Core Loop

The goal is to make the following sequence of commands fully functional:

```shell
# 1. Initialize the forest in the current repository
gf init

# 2. Install a pre-defined plan from the local catalog
gf plans install config/plans/engineering-excellence/dead-code-elimination.yaml

# 3. Reconcile the plan to generate the initial set of plants
gf plan dead-code-elimination reconcile

# 4. List the newly created plants
gf plants list

# 5. Get the status of the forest
gf status
```

## Key Implementation Steps

### 1. Implement the `init` Command
The `init` command must prepare the local repository for use with `git-forest`.

- **Action:** Create the `.git-forest/` directory at the repository root.
- **Action:** Create a default `forest.yaml` file inside `.git-forest/` to store metadata (e.g., version, initialization timestamp).
- **Action:** Create a default `config.yaml` file for repository-specific configuration.
- **Action:** Implement the file-based locking mechanism (`.git-forest/lock`) to ensure safe concurrent operations. The `Program.cs` stubs should be replaced with logic that acquires and releases this lock for any mutating commands.

### 2. Implement the `plans install` Command
This command needs to fetch a plan and install it into the local forest.

- **MVP Scope:** Implement installation from the local filesystem. Installing from a Git URL can be a post-MVP feature.
- **Action:** Copy the specified plan file (e.g., `.../dead-code-elimination.yaml`) and any associated assets into a new directory at `.git-forest/plans/<plan-id>/`.
- **Action:** The plan's ID should be parsed from its content.

### 3. Implement the `plan reconcile` Command
This is the most critical step. It involves running the planner logic to generate the desired state (plants).

- **Action:** Load the specified plan from `.git-forest/plans/<plan-id>/`.
- **Action:** Implement a basic Planner service. For the MVP, this service will be simple:
    - It will read a list of pre-defined `plant` definitions from within the plan file itself.
    - It will not perform complex code analysis yet. The goal is to prove the plant generation mechanism.
- **Action:** For each plant the Planner "generates," create a corresponding subdirectory and `plant.yaml` file (e.g., `.git-forest/plants/dead-code-elimination__my-first-plant/plant.yaml`).
- **Action:** The reconciliation logic must be idempotent. Running it a second time should not create duplicate plants.

### 4. Implement the `plants list` Command
The user must be able to see the results of the reconciliation.

- **Action:** Read the subdirectories within `.git-forest/plants/`.
- **Action:** For each subdirectory, load the `plant.yaml` file to get its metadata (key, status, title).
- **Action:** Display the list of plants and their status in a formatted table, replacing the current hardcoded output.

### 5. Implement the `status` Command
The `status` command should provide a real-time summary of the forest.

- **Action:** Read the contents of the `.git-forest` directory.
- **Action:** Count the number of installed plans (directories in `.git-forest/plans/`).
- **Action:** Count the number of plants and group them by status (by reading each `plant.yaml`).
- **Action:** Display these real counts instead of the current hardcoded values.
