# GitForest Web UI

A Blazor web application for managing git-forest plans through a browser interface.

## Overview

The GitForest Web UI provides a user-friendly interface for:
- Browsing available plans from the catalog (`config/plans` directory)
- Viewing plan details including planners, planters, and scopes
- Installing plans to your local forest
- Viewing installed plans
- Reconciling plans to create/update plants

## Prerequisites

- .NET 10.0 SDK
- A git-forest repository (this one!)

## Running the Web App

### From the repository root:

```bash
cd src/GitForest.Web
dotnet run
```

The application will start and be available at `http://localhost:5000` (or the URL shown in the console).

### Using a specific port:

```bash
dotnet run --urls "http://localhost:8080"
```

## Features

### Plans Catalog
Browse all available plans from the `config/plans` directory. Filter by category and search by name or ID.

### Plan Details
View comprehensive information about each plan including:
- Description and version
- Category and scopes
- Planners (analysis agents)
- Planters (execution agents)
- Source file path

### Install Plans
Install plans from the catalog to your local forest (`~/.git-forest/plans/`). The web UI uses the same installation mechanism as the CLI.

### Installed Plans
View all plans currently installed in your forest with their version and installation date.

### Reconcile Plans
Execute reconciliation for installed plans to create or update plants based on the plan's templates.

## Architecture

The web application:
- Shares the same business logic and services as the CLI
- Uses MediatR for command/query separation
- Stores state in `~/.git-forest/` (same as CLI)
- Reads plan catalog from repository's `config/plans/` directory

## Development

The web app is built using:
- Blazor Server (interactive server-side rendering)
- Bootstrap 5 for styling
- .NET 10.0

To build:
```bash
dotnet build
```

## Notes

- The web UI shares state with the CLI - plans installed via the web interface are visible in the CLI and vice versa
- The forest directory defaults to `~/.git-forest`
- Plan catalog path is resolved relative to the repository root
