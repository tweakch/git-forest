using System.Diagnostics;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace GitForest.Cli.IntegrationTests;

[TestFixture]
[Category("Integration")]
public sealed class ForestInitAndPlantsListTests
{
    [Test]
    public async Task Init_creates_required_git_forest_structure_and_plants_list_references_plan_planter_and_planner()
    {
        var repoRoot = RepoPaths.FindRepoRoot(TestContext.CurrentContext.TestDirectory);
        var cliProject = Path.Combine(repoRoot, "src", "GitForest.Cli", "GitForest.Cli.csproj");
        Assert.That(File.Exists(cliProject), Is.True, () => $"Expected CLI project at: {cliProject}");

        var planSource = Path.Combine(repoRoot, "config", "plans", "quality-reliability", "integration-testing-harness.yaml");
        Assert.That(File.Exists(planSource), Is.True, () => $"Expected plan file to exist in repo: {planSource}");

        var tempRoot = Path.Combine(Path.GetTempPath(), "git-forest-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var workingRepoDir = Path.Combine(tempRoot, "repo");
        Directory.CreateDirectory(workingRepoDir);

        try
        {
            await GitRepo.CreateAsync(workingRepoDir);

            // 1) init
            var init = await DotNetCli.RunGitForestAsync(cliProject, workingRepoDir, ["init"]);
            Assert.That(init.ExitCode, Is.EqualTo(0), () => $"git-forest init failed.\nSTDOUT:\n{init.StdOut}\nSTDERR:\n{init.StdErr}");

            var forestDir = Path.Combine(workingRepoDir, ".git-forest");
            Assert.That(Directory.Exists(forestDir), Is.True, "Expected .git-forest directory to exist after init");

            Assert.That(File.Exists(Path.Combine(forestDir, "forest.yaml")), Is.True, "Expected .git-forest/forest.yaml");
            Assert.That(File.Exists(Path.Combine(forestDir, "config.yaml")), Is.True, "Expected .git-forest/config.yaml");
            Assert.That(File.Exists(Path.Combine(forestDir, "lock")), Is.True, "Expected .git-forest/lock");

            Assert.That(Directory.Exists(Path.Combine(forestDir, "plans")), Is.True, "Expected .git-forest/plans/");
            Assert.That(Directory.Exists(Path.Combine(forestDir, "plants")), Is.True, "Expected .git-forest/plants/");
            Assert.That(Directory.Exists(Path.Combine(forestDir, "planters")), Is.True, "Expected .git-forest/planters/");
            Assert.That(Directory.Exists(Path.Combine(forestDir, "planners")), Is.True, "Expected .git-forest/planners/");
            Assert.That(Directory.Exists(Path.Combine(forestDir, "logs")), Is.True, "Expected .git-forest/logs/");

            // 2) plans install
            var install = await DotNetCli.RunGitForestAsync(cliProject, workingRepoDir, ["plans", "install", planSource]);
            Assert.That(install.ExitCode, Is.EqualTo(0), () => $"git-forest plans install failed.\nSTDOUT:\n{install.StdOut}\nSTDERR:\n{install.StdErr}");

            var installedPlanYaml = Path.Combine(forestDir, "plans", "integration-testing-harness", "plan.yaml");
            Assert.That(File.Exists(installedPlanYaml), Is.True, "Expected installed plan at .git-forest/plans/integration-testing-harness/plan.yaml");

            // 3) plan reconcile (seed plants deterministically)
            var reconcile = await DotNetCli.RunGitForestAsync(cliProject, workingRepoDir, ["plan", "integration-testing-harness", "reconcile"]);
            Assert.That(reconcile.ExitCode, Is.EqualTo(0), () => $"git-forest plan reconcile failed.\nSTDOUT:\n{reconcile.StdOut}\nSTDERR:\n{reconcile.StdErr}");

            var seededPlantYaml = Path.Combine(forestDir, "plants", "integration-testing-harness__add-integration-tests", "plant.yaml");
            Assert.That(File.Exists(seededPlantYaml), Is.True, "Expected seeded plant at .git-forest/plants/integration-testing-harness__add-integration-tests/plant.yaml");

            // 4) plants list (human output should include key + plan + planter)
            var list = await DotNetCli.RunGitForestAsync(cliProject, workingRepoDir, ["plants", "list"]);
            Assert.That(list.ExitCode, Is.EqualTo(0), () => $"git-forest plants list failed.\nSTDOUT:\n{list.StdOut}\nSTDERR:\n{list.StdErr}");
            Assert.That(list.StdOut, Does.Contain("integration-testing-harness:add-integration-tests"));
            Assert.That(list.StdOut, Does.Contain("integration-testing-harness"));
            Assert.That(list.StdOut, Does.Contain("harness-builder"), "Expected plants list to include the assigned planter");

            // 4b) plant <key> show should resolve the plant and show details (regression for stub implementation)
            var show = await DotNetCli.RunGitForestAsync(
                cliProject,
                workingRepoDir,
                ["plant", "integration-testing-harness:add-integration-tests", "show"]);
            Assert.That(show.ExitCode, Is.EqualTo(0), () => $"git-forest plant <key> show failed.\nSTDOUT:\n{show.StdOut}\nSTDERR:\n{show.StdErr}");
            Assert.That(show.StdOut, Does.Contain("Key: integration-testing-harness:add-integration-tests"));
            Assert.That(show.StdOut, Does.Contain("Plan: integration-testing-harness"));

            // 4a) planters list should reflect planters available via installed plan.yaml
            var plantersList = await DotNetCli.RunGitForestAsync(cliProject, workingRepoDir, ["planters", "list"]);
            Assert.That(plantersList.ExitCode, Is.EqualTo(0), () => $"git-forest planters list failed.\nSTDOUT:\n{plantersList.StdOut}\nSTDERR:\n{plantersList.StdErr}");
            Assert.That(plantersList.StdOut, Does.Not.Contain("No planters configured"));
            Assert.That(plantersList.StdOut, Does.Contain("harness-builder"));
            Assert.That(plantersList.StdOut, Does.Contain("integration-test-author"));
            Assert.That(plantersList.StdOut, Does.Contain("stability-engineer"));

            // 4a2) planters list --json should include the planter IDs
            var plantersJson = await DotNetCli.RunGitForestAsync(cliProject, workingRepoDir, ["planters", "list", "--json"]);
            Assert.That(plantersJson.ExitCode, Is.EqualTo(0), () => $"git-forest planters list --json failed.\nSTDOUT:\n{plantersJson.StdOut}\nSTDERR:\n{plantersJson.StdErr}");

            using (var plantersDoc = JsonDocument.Parse(plantersJson.StdOut.Trim()))
            {
                var rootPlanters = plantersDoc.RootElement;
                Assert.That(rootPlanters.TryGetProperty("planters", out var plantersArray), Is.True);
                Assert.That(plantersArray.ValueKind, Is.EqualTo(JsonValueKind.Array));
                var ids = plantersArray.EnumerateArray().Select(x => x.GetProperty("id").GetString()).ToArray();
                Assert.That(ids, Does.Contain("harness-builder"));
                Assert.That(ids, Does.Contain("integration-test-author"));
                Assert.That(ids, Does.Contain("stability-engineer"));
            }

            // 4b) planners list should reflect planners available via installed plan.yaml
            var plannersList = await DotNetCli.RunGitForestAsync(cliProject, workingRepoDir, ["planners", "list"]);
            Assert.That(plannersList.ExitCode, Is.EqualTo(0), () => $"git-forest planners list failed.\nSTDOUT:\n{plannersList.StdOut}\nSTDERR:\n{plannersList.StdErr}");
            Assert.That(plannersList.StdOut, Does.Not.Contain("No planners configured"));
            Assert.That(plannersList.StdOut, Does.Contain("integration-surface-mapper"));
            Assert.That(plannersList.StdOut, Does.Contain("dependency-harness-planner"));
            Assert.That(plannersList.StdOut, Does.Contain("flaky-integration-detector"));

            // 4c) planners list --json should include the planner IDs
            var plannersJson = await DotNetCli.RunGitForestAsync(cliProject, workingRepoDir, ["planners", "list", "--json"]);
            Assert.That(plannersJson.ExitCode, Is.EqualTo(0), () => $"git-forest planners list --json failed.\nSTDOUT:\n{plannersJson.StdOut}\nSTDERR:\n{plannersJson.StdErr}");

            using (var plannersDoc = JsonDocument.Parse(plannersJson.StdOut.Trim()))
            {
                var rootPlanners = plannersDoc.RootElement;
                Assert.That(rootPlanners.TryGetProperty("planners", out var planners), Is.True);
                Assert.That(planners.ValueKind, Is.EqualTo(JsonValueKind.Array));
                var ids = planners.EnumerateArray().Select(x => x.GetProperty("id").GetString()).ToArray();
                Assert.That(ids, Does.Contain("integration-surface-mapper"));
                Assert.That(ids, Does.Contain("dependency-harness-planner"));
                Assert.That(ids, Does.Contain("flaky-integration-detector"));
            }

            // 5) plants list --json should reference planner + planters explicitly
            var listJson = await DotNetCli.RunGitForestAsync(cliProject, workingRepoDir, ["plants", "list", "--json"]);
            Assert.That(listJson.ExitCode, Is.EqualTo(0), () => $"git-forest plants list --json failed.\nSTDOUT:\n{listJson.StdOut}\nSTDERR:\n{listJson.StdErr}");

            using var doc = JsonDocument.Parse(listJson.StdOut.Trim());
            var root = doc.RootElement;
            Assert.That(root.TryGetProperty("plants", out var plants), Is.True);
            Assert.That(plants.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(plants.GetArrayLength(), Is.GreaterThanOrEqualTo(1));

            var first = plants[0];
            Assert.That(first.GetProperty("planId").GetString(), Is.EqualTo("integration-testing-harness"));
            Assert.That(first.GetProperty("plannerId").GetString(), Is.EqualTo("integration-surface-mapper"));

            var planters = first.GetProperty("planters");
            Assert.That(planters.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(planters.EnumerateArray().Select(x => x.GetString()).ToArray(), Does.Contain("harness-builder"));
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

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

    private static class DotNetCli
    {
        public static Task<ProcessResult> RunGitForestAsync(string cliProjectPath, string workingDirectory, IReadOnlyList<string> args)
        {
            var arguments = new List<string>
            {
                "run",
                "--project",
                cliProjectPath,
                "--"
            };
            arguments.AddRange(args);

            return ProcessRunner.RunAsync(
                fileName: "dotnet",
                arguments: arguments,
                workingDirectory: workingDirectory,
                environmentVariables: new Dictionary<string, string>
                {
                    ["DOTNET_NOLOGO"] = "1",
                    ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                    ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1"
                },
                timeout: TimeSpan.FromMinutes(3));
        }
    }

    private static class RepoPaths
    {
        public static string FindRepoRoot(string startDirectory)
        {
            var dir = new DirectoryInfo(startDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "GitForest.sln")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException($"Could not locate repo root containing GitForest.sln starting from '{startDirectory}'");
        }
    }

    private static class GitRepo
    {
        public static async Task CreateAsync(string directory)
        {
            Directory.CreateDirectory(directory);
            await EnsureSuccessAsync(await ProcessRunner.RunAsync("git", ["init"], directory, environmentVariables: new Dictionary<string, string>
            {
                ["GIT_TERMINAL_PROMPT"] = "0",
                ["GIT_CONFIG_NOSYSTEM"] = "1"
            }, timeout: TimeSpan.FromMinutes(1)));

            await EnsureSuccessAsync(await ProcessRunner.RunAsync("git", ["config", "user.email", "test@example.com"], directory, environmentVariables: null, timeout: TimeSpan.FromMinutes(1)));
            await EnsureSuccessAsync(await ProcessRunner.RunAsync("git", ["config", "user.name", "Test User"], directory, environmentVariables: null, timeout: TimeSpan.FromMinutes(1)));
            // Ensure tests are not coupled to developer machine commit signing (e.g. 1Password SSH/GPG signing).
            await EnsureSuccessAsync(await ProcessRunner.RunAsync("git", ["config", "commit.gpgsign", "false"], directory, environmentVariables: null, timeout: TimeSpan.FromMinutes(1)));

            var readme = Path.Combine(directory, "README.md");
            await File.WriteAllTextAsync(readme, "# Test Repo\n", Encoding.UTF8);

            await EnsureSuccessAsync(await ProcessRunner.RunAsync("git", ["add", "README.md"], directory, environmentVariables: null, timeout: TimeSpan.FromMinutes(1)));
            await EnsureSuccessAsync(await ProcessRunner.RunAsync("git", ["commit", "-m", "Initial commit"], directory, environmentVariables: null, timeout: TimeSpan.FromMinutes(1)));
        }

        private static Task EnsureSuccessAsync(ProcessResult result)
        {
            Assert.That(result.ExitCode, Is.EqualTo(0), () => $"Git command failed.\nSTDOUT:\n{result.StdOut}\nSTDERR:\n{result.StdErr}");
            return Task.CompletedTask;
        }
    }

    private static class ProcessRunner
    {
        public static async Task<ProcessResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            string workingDirectory,
            IReadOnlyDictionary<string, string>? environmentVariables,
            TimeSpan timeout)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            foreach (var arg in arguments)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            if (environmentVariables is not null)
            {
                foreach (var (key, value) in environmentVariables)
                {
                    process.StartInfo.Environment[key] = value;
                }
            }

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    stdout.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    stderr.AppendLine(e.Data);
                }
            };

            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process: {fileName}");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = new CancellationTokenSource(timeout);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                throw new TimeoutException($"Process timed out after {timeout}: {fileName} {string.Join(' ', arguments)}");
            }

            return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
        }
    }
}

