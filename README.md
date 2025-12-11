# git-forest

A .NET Aspire-based CLI for managing collections of git repositories as a unified forest.

## Overview

git-forest is a powerful command-line tool that helps you manage multiple git repositories as a cohesive unit called a "forest". It provides an intuitive way to organize, track, and coordinate work across multiple related repositories.

## Core Concepts

### 🌱 Plants
**Plants** represent individual git repositories or trees in your forest. Each plant is a standalone repository that grows with commits, branches, and tags. Just as a plant in nature grows from a seed, a git repository grows from its initial commit.

- Tracked by name and path
- Contains git commit history (the tree)
- Can be planted (added) or removed from the forest
- Maintains its own identity while being part of the collective

### 🌳 Trees
**Trees** refer to the git commit history within each plant. In git terminology, a tree is the structure of files and directories at a specific point in time. In git-forest, we embrace this metaphor - your commit history is the growth rings of your repository tree.

### 🌲 Forest
The **Forest** is a collection of plants (repositories) that are managed together. A forest provides:
- Centralized configuration
- Unified view of multiple repositories
- Coordinated operations across plants
- Relationship tracking between repositories

### 👷 Planters
**Planters** are contributors and developers who plant new repositories and nurture existing ones. They are the active participants who:
- Add new plants to the forest
- Commit changes to repositories
- Collaborate on development
- Maintain the health of their plants

### 📋 Planners
**Planners** are organizers, managers, and technical leads who coordinate the forest. They:
- Define forest strategy and structure
- Oversee multiple forests
- Coordinate between planters
- Ensure forest health and sustainability
- Plan the overall architecture

## Installation

```bash
# Clone the repository
git clone https://github.com/tweakch/git-forest.git
cd git-forest

# Build the solution
dotnet build

# Run the CLI
dotnet run --project src/GitForest.Cli
```

## Quick Start

```bash
# Initialize a new forest
git-forest init

# Add a repository as a plant
git-forest plant --name my-app --path ./my-app

# Check the status of your forest
git-forest status

# List all plants
git-forest plants

# Add yourself as a planter
git-forest planter --name "Your Name" --email your.email@example.com

# List all planters
git-forest planters

# Add a planner
git-forest planner --name "Tech Lead" --email lead@example.com --role "Technical Lead"

# List all planners
git-forest planners
```

## Commands

- **init** - Initialize a new forest in the current directory
- **status** - Show the status of the current forest
- **plant** - Add a new plant (repository) to the forest
- **plants** - List all plants in the forest
- **planter** - Add or view a planter (contributor)
- **planters** - List all planters in the forest
- **planner** - Add or view a planner (organizer/manager)
- **planners** - List all planners in the forest

For detailed documentation on each command, see the [CLI documentation](./docs/cli/README.md).

## Project Structure

```
git-forest/
├── src/
│   ├── GitForest.Core/      # Core domain models and services
│   └── GitForest.Cli/        # Command-line interface
├── config/                   # Configuration files
├── docs/
│   └── cli/                  # CLI command documentation
├── .github/
│   └── workflows/            # GitHub Actions CI/CD
└── GitForest.sln            # Solution file
```

## Technology Stack

- **.NET 10.0** - Latest .NET runtime
- **.NET Aspire** - Cloud-native application framework
- **System.CommandLine** - Modern CLI framework
- **C#** - Primary language

## Contributing

We welcome contributions from both planters (developers) and planners (organizers)!

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

[Your License Here]

## Support

For issues and questions, please use the GitHub issue tracker.