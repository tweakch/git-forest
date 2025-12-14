# Gemini Code Companion Context: `git-forest`

This document provides context for the `git-forest` project to the Gemini code companion.

## Project Overview

`git-forest` is a command-line tool built with .NET 10 for managing and automating software development workflows across multiple Git repositories, or within a single complex one. It conceptualizes codebase maintenance as "cultivating a forest."

The core idea is to transform fragmented work (branches, fixes, refactors) into a durable, living system. It uses a declarative approach where `Plans` define the desired state of the codebase, and automated agents (`Planners` and `Planters`) work to achieve that state.

**Core Concepts:**

*   **Forest:** The overall repository state and configuration, stored locally in the `.git-forest/` directory.
*   **Plan:** A versioned, packaged set of instructions that defines a specific goal (e.g., `dependency-hygiene`, `dead-code-elimination`). It contains planners and planter configurations. The project includes a catalog of over 40 pre-defined plans.
*   **Planner:** A deterministic generator that analyzes the repository against a `Plan` and produces a desired set of `Plants`.
*   **Plant:** A concrete, trackable work item representing a proposed change, like a refactoring task or a bug fix. Each plant has a stable key and a lifecycle (`planned` -> `planted` -> `growing` -> `harvested`).
*   **Planter:** An executor or "agent" persona that acts on a `Plant` to create and propose changes (e.g., create a branch, generate a diff, open a pull request).

The system is designed to be idempotent, deterministic, and automation-friendly, with clear ownership and safe concurrency mechanisms.

## Architecture

The project is a .NET solution (`GitForest.sln`) with two main components:

*   **`src/GitForest.Core`**: A .NET class library containing the core domain models (`Forest`, `Plan`, `Plant`, etc.) and business logic.
*   **`src/GitForest.Cli`**: A .NET console application that provides the command-line interface. It is built using the `System.CommandLine` library.

The CLI (`git-forest` or `gf`) is the primary user interface and is designed for both interactive use and integration into automated CI/CD pipelines via JSON output.

## Building and Running

The project is built and tested using standard .NET CLI commands.

**1. Restore Dependencies:**
This command restores the NuGet packages for the solution.
```shell
dotnet restore
```

**2. Build the Solution:**
This command builds both the `GitForest.Core` library and the `GitForest.Cli` executable.
```shell
dotnet build --no-restore
```

**3. Run the CLI:**
To run the command-line interface from the source code:
```shell
dotnet run --project src/GitForest.Cli -- [command] [options]
```
*Example:*
```shell
dotnet run --project src/GitForest.Cli -- status
```

**4. Run Tests:**
The continuous integration pipeline defines a test step.
```shell
dotnet test --no-build
```

## Development Conventions

*   **CLI as Contract:** The `CLI.md` file serves as the formal specification for the command-line interface. All CLI changes should be reflected there.
*   **Automation First:** Every command that outputs data supports a `--json` flag for predictable, scriptable output. The CLI also uses a set of stable exit codes for automation, documented in `README.md`.
*   **Declarative & Idempotent:** Core operations, especially `plan reconcile`, are designed to be deterministic and idempotent. Running the same command multiple times should not produce new changes unless the underlying code or plan has changed.
*   **Dogfooding:** The project uses its own `forest-maintenance` plan to perform self-inspection and maintain its own code quality, as defined in `docs/forest-maintenance-contract.md`. This is a key part of the CI process.
*   **Configuration:** Configuration is handled through YAML files stored within the `.git-forest/` directory, with a defined hierarchy (user, repo, plan).
*   **On-Disk State:** The tool maintains all its state within the `.git-forest` directory at the root of the repository. This includes installed plans, plant status, and logs. The schema is detailed in `CLI.md`.
