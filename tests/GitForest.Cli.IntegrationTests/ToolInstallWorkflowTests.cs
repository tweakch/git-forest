using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace GitForest.Cli.IntegrationTests;

[TestFixture]
[Category("Integration")]
[NonParallelizable]
public sealed class ToolInstallWorkflowTests
{
    [Test]
    public async Task Pack_install_run_master_workflow_uninstall_verify()
    {
        var repoRoot = RepoPaths.FindRepoRoot(TestContext.CurrentContext.TestDirectory);
        var cliProject = Path.Combine(repoRoot, "src", "GitForest.Cli", "GitForest.Cli.csproj");
        Assert.That(File.Exists(cliProject), Is.True, () => $"Expected CLI project at: {cliProject}");

        var tempRoot = Path.Combine(Path.GetTempPath(), "git-forest-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var packagesDir = Path.Combine(tempRoot, "packages");
        var toolPath = Path.Combine(tempRoot, "tool-path");
        var workingRepoDir = Path.Combine(tempRoot, "repo");

        Directory.CreateDirectory(packagesDir);
        Directory.CreateDirectory(toolPath);
        Directory.CreateDirectory(workingRepoDir);

        var dotnetEnv = TestEnvironments.DotNet;
        var gitEnv = TestEnvironments.Git;

        string toolExePath;

        try
        {
            // 0) Build once (so pack can be --no-build/--no-restore)
            var build = await ProcessRunner.RunAsync(
                fileName: "dotnet",
                arguments:
                [
                    "build",
                    cliProject,
                    "--configuration",
                    "Release",
                    "--nologo"
                ],
                workingDirectory: repoRoot,
                environmentVariables: dotnetEnv,
                timeout: TimeSpan.FromMinutes(5));

            Assert.That(build.ExitCode, Is.EqualTo(0), () => $"dotnet build failed.\nSTDOUT:\n{build.StdOut}\nSTDERR:\n{build.StdErr}");

            // 1) Pack tool
            var pack = await ProcessRunner.RunAsync(
                fileName: "dotnet",
                arguments:
                [
                    "pack",
                    cliProject,
                    "--configuration",
                    "Release",
                    "--output",
                    packagesDir,
                    "--no-build",
                    "--no-restore",
                    "--nologo"
                ],
                workingDirectory: repoRoot,
                environmentVariables: dotnetEnv,
                timeout: TimeSpan.FromMinutes(5));

            Assert.That(pack.ExitCode, Is.EqualTo(0), () => $"dotnet pack failed.\nSTDOUT:\n{pack.StdOut}\nSTDERR:\n{pack.StdErr}");

            var nupkg = Directory.GetFiles(packagesDir, "git-forest*.nupkg").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            Assert.That(nupkg, Is.Not.Null, "Expected a .nupkg in the pack output directory");

            // 2) Install tool into isolated tool-path
            var install = await ProcessRunner.RunAsync(
                fileName: "dotnet",
                arguments:
                [
                    "tool",
                    "install",
                    "--tool-path",
                    toolPath,
                    "--add-source",
                    packagesDir,
                    "git-forest"
                ],
                workingDirectory: repoRoot,
                environmentVariables: dotnetEnv,
                timeout: TimeSpan.FromMinutes(3));

            Assert.That(install.ExitCode, Is.EqualTo(0), () => $"dotnet tool install failed.\nSTDOUT:\n{install.StdOut}\nSTDERR:\n{install.StdErr}");

            toolExePath = ToolPaths.GetToolCommandPath(toolPath, "git-forest");
            Assert.That(File.Exists(toolExePath), Is.True, () => $"Expected tool command to exist after install: {toolExePath}");

            // 3) Run a basic sanity check
            var version = await ProcessRunner.RunAsync(
                fileName: toolExePath,
                arguments: ["--version"],
                workingDirectory: workingRepoDir,
                environmentVariables: dotnetEnv,
                timeout: TimeSpan.FromMinutes(1));

            Assert.That(version.ExitCode, Is.EqualTo(0), () => $"git-forest --version failed.\nSTDOUT:\n{version.StdOut}\nSTDERR:\n{version.StdErr}");
            Assert.That(version.StdOut.Trim(), Is.Not.Empty);

            // 4) Create deterministic git repo context
            await GitRepo.CreateAsync(workingRepoDir, gitEnv);

            // Ensure workflow inputs exist inside the temp repo (so path-based steps are realistic).
            var planSource = Path.Combine(repoRoot, "config", "plans", "engineering-excellence", "dependency-hygiene.yaml");
            Assert.That(File.Exists(planSource), Is.True, () => $"Expected plan file to exist in repo: {planSource}");

            var planDest = Path.Combine(workingRepoDir, "config", "plans", "engineering-excellence", "dependency-hygiene.yaml");
            Directory.CreateDirectory(Path.GetDirectoryName(planDest)!);
            File.Copy(planSource, planDest, overwrite: true);

            // 5) Execute master workflow spec, deterministically asserting JSON outputs
            var workflowPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "workflows", "master-workflow.json");
            Assert.That(File.Exists(workflowPath), Is.True, () => $"Workflow spec not found at: {workflowPath}");

            var workflowJson = await File.ReadAllTextAsync(workflowPath, Encoding.UTF8);
            var workflow = JsonSerializer.Deserialize<WorkflowSpec>(workflowJson, JsonSerialization.Options);
            Assert.That(workflow, Is.Not.Null);
            Assert.That(workflow!.Steps, Is.Not.Null);
            Assert.That(workflow.Steps.Length, Is.GreaterThan(0));

            foreach (var step in workflow.Steps)
            {
                var result = await ProcessRunner.RunAsync(
                    fileName: toolExePath,
                    arguments: step.Args,
                    workingDirectory: workingRepoDir,
                    environmentVariables: dotnetEnv,
                    timeout: TimeSpan.FromMinutes(1));

                Assert.That(
                    result.ExitCode,
                    Is.EqualTo(step.ExitCode),
                    () => $"Step '{step.Name}' failed.\nArgs: {string.Join(' ', step.Args)}\nExpected exit: {step.ExitCode}\nActual exit: {result.ExitCode}\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");

                if ((step.JsonEquals is not null && step.JsonEquals.Count > 0) || (step.JsonPaths is not null && step.JsonPaths.Length > 0))
                {
                    var trimmed = result.StdOut.Trim();
                    Assert.That(trimmed, Is.Not.Empty, () => $"Step '{step.Name}' expected JSON output but stdout was empty.");

                    JsonNode? node;
                    try
                    {
                        node = JsonNode.Parse(trimmed);
                    }
                    catch (JsonException ex)
                    {
                        Assert.Fail($"Step '{step.Name}' expected JSON but stdout was not valid JSON.\nSTDOUT:\n{result.StdOut}\nParse error: {ex}");
                        return;
                    }

                    Assert.That(node, Is.Not.Null);

                    if (step.JsonPaths is not null)
                    {
                        foreach (var path in step.JsonPaths)
                        {
                            Assert.That(JsonAsserts.TryGetAtPath(node!, path, out _), Is.True, () => $"Step '{step.Name}' missing JSON path '{path}'.\nSTDOUT:\n{result.StdOut}");
                        }
                    }

                    if (step.JsonEquals is not null)
                    {
                        foreach (var (path, expected) in step.JsonEquals)
                        {
                            Assert.That(JsonAsserts.TryGetAtPath(node!, path, out var actual), Is.True, () => $"Step '{step.Name}' missing JSON path '{path}'.\nSTDOUT:\n{result.StdOut}");

                            Assert.That(
                                JsonAsserts.JsonValuesEqual(actual, expected),
                                Is.True,
                                () => $"Step '{step.Name}' JSON mismatch at '{path}'.\nExpected: {expected?.ToJsonString() ?? "null"}\nActual: {actual?.ToJsonString() ?? "null"}\nSTDOUT:\n{result.StdOut}");
                        }
                    }
                }
            }

            // 6) Uninstall and verify
            var uninstall = await ProcessRunner.RunAsync(
                fileName: "dotnet",
                arguments:
                [
                    "tool",
                    "uninstall",
                    "--tool-path",
                    toolPath,
                    "git-forest"
                ],
                workingDirectory: repoRoot,
                environmentVariables: dotnetEnv,
                timeout: TimeSpan.FromMinutes(2));

            Assert.That(uninstall.ExitCode, Is.EqualTo(0), () => $"dotnet tool uninstall failed.\nSTDOUT:\n{uninstall.StdOut}\nSTDERR:\n{uninstall.StdErr}");

            Assert.That(File.Exists(toolExePath), Is.False, () => $"Expected tool command to be removed after uninstall: {toolExePath}");

            var list = await ProcessRunner.RunAsync(
                fileName: "dotnet",
                arguments:
                [
                    "tool",
                    "list",
                    "--tool-path",
                    toolPath
                ],
                workingDirectory: repoRoot,
                environmentVariables: dotnetEnv,
                timeout: TimeSpan.FromMinutes(1));

            Assert.That(list.ExitCode, Is.EqualTo(0), () => $"dotnet tool list failed.\nSTDOUT:\n{list.StdOut}\nSTDERR:\n{list.StdErr}");
            Assert.That(list.StdOut, Does.Not.Contain("git-forest"), "Expected 'git-forest' to not appear in dotnet tool list after uninstall");
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private static class JsonSerialization
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private sealed record WorkflowSpec(StepSpec[] Steps);

    private sealed record StepSpec(
        string Name,
        string[] Args,
        int ExitCode,
        Dictionary<string, JsonNode?>? JsonEquals = null,
        string[]? JsonPaths = null);

    private static class JsonAsserts
    {
        public static bool TryGetAtPath(JsonNode node, string dottedPath, out JsonNode? value)
        {
            value = node;
            var segments = dottedPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var segment in segments)
            {
                if (value is JsonObject obj)
                {
                    if (!obj.TryGetPropertyValue(segment, out value))
                    {
                        value = null;
                        return false;
                    }

                    continue;
                }

                value = null;
                return false;
            }

            return true;
        }

        public static bool JsonValuesEqual(JsonNode? actual, JsonNode? expected)
        {
            if (actual is null && expected is null)
            {
                return true;
            }

            if (actual is null || expected is null)
            {
                return false;
            }

            // Primitive comparison
            if (actual is JsonValue && expected is JsonValue)
            {
                return Normalize(actual) == Normalize(expected);
            }

            // Fallback to JSON string comparison (stable enough for small objects)
            return actual.ToJsonString() == expected.ToJsonString();
        }

        private static string Normalize(JsonNode value)
        {
            // Using ToJsonString keeps primitives comparable across string/number/bool.
            return value.ToJsonString();
        }
    }
}
