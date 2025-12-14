# Versioning Strategy

This document defines the versioning approach for the `git-forest` repository. Adhering to a consistent versioning strategy is crucial for communicating changes to users, managing releases, and maintaining a predictable development lifecycle, especially for a tool intended for distribution on NuGet.

## Chosen Standard: Semantic Versioning (SemVer)

This project will adhere to **Semantic Versioning 2.0.0**. You can find the full specification at [https://semver.org/](https://semver.org/).

A version number is composed of three parts: `MAJOR.MINOR.PATCH`.

- **`MAJOR`**: Incremented for incompatible API changes. This includes any modification to the CLI commands, options, or JSON output structure that would break a user's existing scripts or workflows.
- **`MINOR`**: Incremented when new functionality is added in a backward-compatible manner. For example, adding a new non-required command or an optional flag to an existing command.
- **`PATCH`**: Incremented for backward-compatible bug fixes. This covers internal logic fixes that correct behavior without altering the public-facing CLI contract.

This standard provides clear and predictable signals to users about the nature of changes in each new release.

## Versioning Process

The version number is not stored in any file in the repository. Instead, the version is determined at release time based on Git tags. This is the single source of truth.

### Release Workflow

1.  **Determine the Version:** Based on the changes accumulated on the `main` branch since the last release, decide on the next version number according to SemVer rules.

2.  **Create a Git Tag:** Create a new, annotated Git tag for the release. The tag name **must** be prefixed with `v`.
    ```shell
    # Example for a minor release
    git tag -a v0.2.0 -m "Release version 0.2.0"

    # Example for a patch release
    git tag -a v0.2.1 -m "Release version 0.2.1"
    ```

3.  **Push the Tag:** Push the tag to the `origin` remote repository.
    ```shell
    git push origin v0.2.0
    ```

4.  **Automated Release:** Pushing a new version tag will automatically trigger the "Publish to NuGet" GitHub Actions workflow (as defined in `INSTALLATION.md`). The workflow will:
    - Build the project.
    - Create a NuGet package, dynamically setting its version from the Git tag.
    - Publish the package to NuGet.org.

### Pre-releases

For testing new features or releasing potentially unstable versions, we will use pre-release tags. These are appended to the version number with a hyphen.

- **Examples:** `v0.2.0-alpha.1`, `v1.0.0-beta.1`, `v1.0.0-rc.1`

These can be created and pushed just like regular release tags. They will also be published to NuGet but will be marked as pre-releases, so users must opt-in to install them (`dotnet tool install ... --prerelease`). This allows for public testing without affecting the stable release channel.

## Initial Development Phase (Version 0.x)

Until the project reaches version `1.0.0`, it is considered to be in the "initial development phase."

- **Starting Version:** The first MVP release should be `v0.1.0`.
- **API Stability:** While the project is in the `0.x.y` range, the public API (CLI commands, flags, JSON output) should be considered unstable.
- **Breaking Changes:** During this phase, breaking changes may be introduced in `MINOR` versions (e.g., moving from `v0.1.0` to `v0.2.0`). Each `0.x` release signals a potentially significant update.
- **Goal for 1.0.0:** A `v1.0.0` release will signify that the core features are complete and the public API has stabilized. After this point, SemVer rules will be strictly followed, and breaking changes will only be introduced in new `MAJOR` versions.
