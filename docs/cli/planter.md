# planter

Add or view a planter (contributor).

## Usage

```bash
# Add a new planter
git-forest planter --name <name> --email <email>

# View current planter
git-forest planter
```

## Options

- `--name` - Name of the planter
- `--email` - Email address of the planter

## Description

The `planter` command manages planters - contributors who plant repositories in the forest. Planters are the active developers and contributors.

## Examples

```bash
# Add a new planter
$ git-forest planter --name "John Doe" --email john@example.com
Adding planter 'John Doe' (john@example.com)...
Planter added successfully!

# View current planter
$ git-forest planter
Current planter information:
Name: John Doe
Email: john@example.com
```
