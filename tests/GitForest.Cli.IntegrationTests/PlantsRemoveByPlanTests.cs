using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace GitForest.Cli.IntegrationTests;

[TestFixture]
[Category("Integration")]
[Parallelizable(ParallelScope.All)]
public sealed class PlantsRemoveByPlanTests
{
    [Test]
    public async Task Plants_remove_by_plan_requires_yes_when_not_dry_run()
    {
        var repoRoot = RepoPaths.FindRepoRoot(TestContext.CurrentContext.TestDirectory);

        var planSource = Path.Combine(
            repoRoot,
            "config",
            "plans",
            "quality-reliability",
            "integration-testing-harness.yaml"
        );
        Assert.That(File.Exists(planSource), Is.True, () => $"Expected plan file to exist: {planSource}");

        await using var workspace = TestWorkspace.Create();

        await GitRepo.CreateAsync(workspace.RepoDirectory, workspace.GitEnvironment);

        CliTestAsserts.Succeeded(
            await workspace.RunGitForestAsync(["init"], timeout: TimeSpan.FromMinutes(3)),
            "git-forest init failed"
        );
        CliTestAsserts.Succeeded(
            await workspace.RunGitForestAsync(
                ["plans", "install", planSource],
                timeout: TimeSpan.FromMinutes(3)
            ),
            "git-forest plans install failed"
        );
        CliTestAsserts.Succeeded(
            await workspace.RunGitForestAsync(
                ["plan", "integration-testing-harness", "reconcile"],
                timeout: TimeSpan.FromMinutes(3)
            ),
            "git-forest plan reconcile failed"
        );

        var remove = await workspace.RunGitForestAsync(
            ["plants", "remove", "--plan", "integration-testing-harness", "--force"],
            timeout: TimeSpan.FromMinutes(1)
        );
        CliTestAsserts.ExitCodeIs(
            remove,
            expectedExitCode: 2,
            "plants remove --plan without --yes should fail"
        );
        Assert.That(remove.StdErr, Does.Contain("confirmation required"));

        workspace.MarkSucceeded();
    }

    [Test]
    public async Task Plants_remove_by_plan_deletes_all_plant_directories_for_plan()
    {
        var repoRoot = RepoPaths.FindRepoRoot(TestContext.CurrentContext.TestDirectory);

        var planSource = Path.Combine(
            repoRoot,
            "config",
            "plans",
            "quality-reliability",
            "integration-testing-harness.yaml"
        );
        Assert.That(File.Exists(planSource), Is.True, () => $"Expected plan file to exist: {planSource}");

        await using var workspace = TestWorkspace.Create();

        await GitRepo.CreateAsync(workspace.RepoDirectory, workspace.GitEnvironment);

        CliTestAsserts.Succeeded(
            await workspace.RunGitForestAsync(["init"], timeout: TimeSpan.FromMinutes(3)),
            "git-forest init failed"
        );
        CliTestAsserts.Succeeded(
            await workspace.RunGitForestAsync(
                ["plans", "install", planSource],
                timeout: TimeSpan.FromMinutes(3)
            ),
            "git-forest plans install failed"
        );
        CliTestAsserts.Succeeded(
            await workspace.RunGitForestAsync(
                ["plan", "integration-testing-harness", "reconcile"],
                timeout: TimeSpan.FromMinutes(3)
            ),
            "git-forest plan reconcile failed"
        );

        var listBefore = await workspace.RunGitForestAsync(
            ["plants", "list", "--plan", "integration-testing-harness", "--json"],
            timeout: TimeSpan.FromMinutes(1)
        );
        CliTestAsserts.Succeeded(listBefore, "plants list --plan --json failed");
        using (var doc = CliTestAsserts.ParseJsonFromStdOut(listBefore, "plants list --plan --json"))
        {
            Assert.That(doc.RootElement.TryGetProperty("plants", out var plants), Is.True);
            Assert.That(plants.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(plants.GetArrayLength(), Is.GreaterThan(0), "Expected at least one seeded plant");
        }

        CliTestAsserts.Succeeded(
            await workspace.RunGitForestAsync(
                ["plants", "remove", "--plan", "integration-testing-harness", "--yes", "--force"],
                timeout: TimeSpan.FromMinutes(1)
            ),
            "plants remove --plan failed"
        );

        var plantsDir = Path.Combine(workspace.RepoDirectory, ".git-forest", "plants");
        Assert.That(Directory.Exists(plantsDir), Is.True, "Expected .git-forest/plants to exist");

        var remaining = Directory
            .GetDirectories(plantsDir)
            .Select(Path.GetFileName)
            .Where(d => d is not null)
            .Select(d => d!)
            .Where(d => d.StartsWith("integration-testing-harness__", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.That(
            remaining,
            Is.Empty,
            "Expected no remaining plant directories for integration-testing-harness after remove --plan"
        );

        var listAfter = await workspace.RunGitForestAsync(
            ["plants", "list", "--plan", "integration-testing-harness", "--json"],
            timeout: TimeSpan.FromMinutes(1)
        );
        CliTestAsserts.Succeeded(listAfter, "plants list --plan --json failed after remove --plan");
        using (var doc = CliTestAsserts.ParseJsonFromStdOut(listAfter, "plants list --plan --json"))
        {
            Assert.That(doc.RootElement.TryGetProperty("plants", out var plants), Is.True);
            Assert.That(plants.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(plants.GetArrayLength(), Is.EqualTo(0));
        }

        workspace.MarkSucceeded();
    }
}

