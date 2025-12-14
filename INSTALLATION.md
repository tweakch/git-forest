# Ticket: Implement .NET Global Tool Installation

This ticket describes the work required to package and distribute `git-forest` as a .NET Global Tool, making it easily installable for end-users.

- **State:** To Do
- **Assignee:** Unassigned
- **Epic:** MVP Readiness
- **Tags:** `build`, `packaging`, `distribution`, `user-experience`

---

## User Story

As a new user, I want to install the `git-forest` CLI with a single, standard command, so that I can quickly start using the tool without needing to clone, build, or manage the source code manually.

## Desired User Experience

The primary installation method should be via the .NET tool ecosystem. The user should be able to open their terminal and run a standard command to install the latest version.

**For Stable Release:**
```shell
dotnet tool install --global git-forest
```

**For Pre-releases:**
```shell
dotnet tool install --global git-forest --prerelease
```

After a successful installation, the `git-forest` command (and its intended alias `gf`) should be immediately available in the user's shell path.

```shell
gf --version
```

## Acceptance Criteria

- [ ] The `GitForest.Cli` project is configured to be packaged as a .NET Global Tool.
- [ ] A `dotnet pack` command successfully creates a NuGet package (`.nupkg`) in a designated output directory (e.g., `/artifacts/packages`).
- [ ] The GitHub Actions workflow (`ci.yml`) is updated to include a `pack` step that runs after a successful build.
- [ ] A new GitHub Actions workflow is created to automatically publish the NuGet package to NuGet.org when a new version tag (e.g., `v1.0.0`) is pushed.
- [ ] A user can successfully install the tool from NuGet.org using the `dotnet tool install --global git-forest` command.
- [ ] After installation, running `git-forest --version` prints the correct version number.
- [ ] (Optional) A user can configure a shell alias `gf` that invokes `git-forest`.
- [ ] The `README.md` file is updated to replace the "build from source" installation guide with the new, simpler `dotnet tool install` command.

## Technical Implementation Plan

### 1. Update `GitForest.Cli.csproj` for Packaging

The `src/GitForest.Cli/GitForest.Cli.csproj` file needs to be updated with properties that enable packaging for distribution as a .NET tool.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- ... existing properties ... -->

  <PropertyGroup>
    <!-- Add these properties for packaging -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>git-forest</ToolCommandName>
    <PackageId>git-forest</PackageId>
    <Title>git-forest</Title>
    <Description>A .NET CLI for managing collections of git repositories as a unified forest, enabling systematic codebase cultivation.</Description>
    <PackageTags>git;automation;refactoring;dotnet;cli;developer-tools</PackageTags>
    <RepositoryUrl>https://github.com/tweakch/git-forest</RepositoryUrl>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression> <!-- Or other appropriate license -->
  </PropertyGroup>

  <!-- ... existing ItemGroups ... -->

</Project>
```
*Note:* .NET tools expose a single command name; `gf` can be supported by users creating a shell alias. For the MVP, `git-forest` is sufficient as the primary command name.

### 2. Update the CI Workflow (`.github/workflows/ci.yml`)

The existing CI workflow should be modified to produce the NuGet package as an artifact.

```yaml
# .github/workflows/ci.yml

# ... after the 'Test' step ...

    - name: Pack
      run: dotnet pack src/GitForest.Cli/GitForest.Cli.csproj --no-build --configuration Release --output ./artifacts/packages

    - name: Upload package artifact
      uses: actions/upload-artifact@v4
      with:
        name: git-forest-package
        path: ./artifacts/packages
```

### 3. Create a Release Workflow for NuGet Publishing

A new workflow file should be created (e.g., `.github/workflows/release.yml`) to handle publishing to NuGet.

```yaml
# .github/workflows/release.yml
name: Publish to NuGet

on:
  push:
    tags:
      - 'v[0-9]+.[0-9]+.[0-9]+*' # Trigger on tags like v1.0.0, v1.2.3-beta

permissions:
  contents: read

jobs:
  publish:
    name: Publish NuGet Package
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore, Build, and Pack
        run: |
          dotnet restore
          dotnet pack src/GitForest.Cli/GitForest.Cli.csproj --no-restore --configuration Release -p:Version=${GITHUB_REF_NAME#v} --output ./package

      - name: NuGet login (OIDC → temp API key)
        uses: NuGet/login@v1
        id: login
        with:
          user: ${{ secrets.NUGET_USER }}

      - name: Push to NuGet
        run: dotnet nuget push "./package/*.nupkg" --api-key ${{steps.login.outputs.NUGET_API_KEY}} --source "https://api.nuget.org/v3/index.json"
```
**Pre-requisite (Trusted Publishing):**

1. Configure a **Trusted Publishing** policy on nuget.org for this repo + workflow file (see `https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing`).
2. Add a GitHub Actions secret named `NUGET_USER` containing your nuget.org **profile name** (not email).

### 4. Update Documentation

Once the tool is available on NuGet.org, the `Installation` section in `README.md` must be updated to reflect the new, simpler process.
