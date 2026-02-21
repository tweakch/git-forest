using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace GitForest.Cli.IntegrationTests;

[TestFixture]
[Category("Integration")]
[Parallelizable(ParallelScope.All)]
public sealed class PlantsRemoveTests
{
    [Test]
    public async Task Plants_remove_requires_yes_when_not_dry_run()
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

        var plantKey = "integration-testing-harness:add-integration-tests";

        var remove = await workspace.RunGitForestAsync(
            ["plants", "remove", plantKey, "--force"],
            timeout: TimeSpan.FromMinutes(1)
        );
        CliTestAsserts.ExitCodeIs(remove, expectedExitCode: 2, "plants remove without --yes should fail");
        Assert.That(remove.StdErr, Does.Contain("confirmation required"));

        workspace.MarkSucceeded();
    }

    [Test]
    public async Task Plants_remove_deletes_plant_directory_and_plant_disappears_from_list()
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

        var plantKey = "integration-testing-harness:add-integration-tests";
        var plantDir = Path.Combine(
            workspace.RepoDirectory,
            ".git-forest",
            "plants",
            "integration-testing-harness__add-integration-tests"
        );
        Assert.That(Directory.Exists(plantDir), Is.True, "Expected seeded plant directory to exist");

        // Removal is safest after archiving (default contract).
        CliTestAsserts.Succeeded(
            await workspace.RunGitForestAsync(
                ["plant", plantKey, "archive", "--force"],
                timeout: TimeSpan.FromMinutes(1)
            ),
            "plant archive --force failed"
        );

        CliTestAsserts.Succeeded(
            await workspace.RunGitForestAsync(
                ["plants", "remove", plantKey, "--yes"],
                timeout: TimeSpan.FromMinutes(1)
            ),
            "plants remove failed"
        );

        Assert.That(
            Directory.Exists(plantDir),
            Is.False,
            "Expected plant directory to be deleted after plants remove"
        );

        var listJson = await workspace.RunGitForestAsync(
            ["plants", "list", "--json"],
            timeout: TimeSpan.FromMinutes(1)
        );
        CliTestAsserts.Succeeded(listJson, "plants list --json failed after remove");
        using var doc = CliTestAsserts.ParseJsonFromStdOut(listJson, "plants list --json");

        Assert.That(doc.RootElement.TryGetProperty("plants", out var plants), Is.True);
        Assert.That(plants.ValueKind, Is.EqualTo(JsonValueKind.Array));

        var keys = plants.EnumerateArray().Select(p => p.GetProperty("key").GetString()).ToArray();
        Assert.That(keys, Does.Not.Contain(plantKey));

        workspace.MarkSucceeded();
    }
}

