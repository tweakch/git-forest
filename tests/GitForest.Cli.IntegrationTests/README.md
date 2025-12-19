## GitForest CLI integration tests

These tests exercise the CLI end-to-end by running the built `GitForest.Cli` via `dotnet <path-to-dll> ...` in an isolated temp repo.

### Run

- `dotnet test tests/GitForest.Cli.IntegrationTests/GitForest.Cli.IntegrationTests.csproj -c Release`

### Temp workspace

- Each test creates a workspace under `%TEMP%\\git-forest-integration\\<guid>`.
- Workspaces are kept on failure to aid debugging.
- Set `GITFOREST_IT_KEEP_TEMP=1` to always keep workspaces (paths are printed to the test log).

### Notes

- Git is sandboxed per test (no reliance on developer/CI global git config).
- `trx` and `coverlet.collector` are enabled only when `CI=true` to keep local runs fast and clean.
- If your environment blocks build outputs under the repo, run `dotnet test` with `-p:IntermediateOutputPath=%TEMP%\\gitforest-it\\obj\\ -p:OutputPath=%TEMP%\\gitforest-it\\bin\\`.
