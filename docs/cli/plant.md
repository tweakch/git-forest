# plant

Add a new plant (repository) to the forest.

## Usage

```bash
git-forest plant --name <name> --path <path>
```

## Options

- `--name` (required) - Name of the plant
- `--path` (required) - Path to the git repository

## Description

The `plant` command registers a git repository as a plant in your forest. This allows the forest to track and manage the repository.

## Example

```bash
$ git-forest plant --name my-app --path ./my-app
Planting 'my-app' at './my-app'...
Plant added successfully!
```
