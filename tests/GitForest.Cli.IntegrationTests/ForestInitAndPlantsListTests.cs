using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace GitForest.Cli.IntegrationTests;

[TestFixture]
[Category("Integration")]
[Parallelizable(ParallelScope.All)]
public sealed class ForestInitAndPlantsListTests
{
    [Test]
    public async Task Init_creates_required_git_forest_structure_and_plants_list_references_plan_planter_and_planner()
    {
        var repoRoot = RepoPaths.FindRepoRoot(TestContext.CurrentContext.TestDirectory);

        var planSource = Path.Combine(
            repoRoot,
            "config",
            "plans",
            "quality-reliability",
            "integration-testing-harness.yaml"
        );
        Assert.That(
            File.Exists(planSource),
            Is.True,
            () => $"Expected plan file to exist in repo: {planSource}"
        );

        await using var workspace = TestWorkspace.Create();

        {
            await GitRepo.CreateAsync(workspace.RepoDirectory, workspace.GitEnvironment);

            // 1) init
            var init = await workspace.RunGitForestAsync(["init"], timeout: TimeSpan.FromMinutes(3));
            CliTestAsserts.Succeeded(init, "git-forest init failed");

            var forestDir = Path.Combine(workspace.RepoDirectory, ".git-forest");
            Assert.That(
                Directory.Exists(forestDir),
                Is.True,
                "Expected .git-forest directory to exist after init"
            );

            Assert.That(
                File.Exists(Path.Combine(forestDir, "forest.yaml")),
                Is.True,
                "Expected .git-forest/forest.yaml"
            );
            Assert.That(
                File.Exists(Path.Combine(forestDir, "config.yaml")),
                Is.True,
                "Expected .git-forest/config.yaml"
            );
            Assert.That(
                File.Exists(Path.Combine(forestDir, "lock")),
                Is.True,
                "Expected .git-forest/lock"
            );

            Assert.That(
                Directory.Exists(Path.Combine(forestDir, "plans")),
                Is.True,
                "Expected .git-forest/plans/"
            );
            Assert.That(
                Directory.Exists(Path.Combine(forestDir, "plants")),
                Is.True,
                "Expected .git-forest/plants/"
            );
            Assert.That(
                Directory.Exists(Path.Combine(forestDir, "planters")),
                Is.True,
                "Expected .git-forest/planters/"
            );
            Assert.That(
                Directory.Exists(Path.Combine(forestDir, "planners")),
                Is.True,
                "Expected .git-forest/planners/"
            );
            Assert.That(
                Directory.Exists(Path.Combine(forestDir, "logs")),
                Is.True,
                "Expected .git-forest/logs/"
            );

            // 2) plans install
            var install = await workspace.RunGitForestAsync(
                ["plans", "install", planSource],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(install, "git-forest plans install failed");

            var installedPlanYaml = Path.Combine(
                forestDir,
                "plans",
                "integration-testing-harness",
                "plan.yaml"
            );
            Assert.That(
                File.Exists(installedPlanYaml),
                Is.True,
                "Expected installed plan at .git-forest/plans/integration-testing-harness/plan.yaml"
            );

            // 3) plan reconcile (seed plants deterministically)
            var reconcile = await workspace.RunGitForestAsync(
                ["plan", "integration-testing-harness", "reconcile"],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(reconcile, "git-forest plan reconcile failed");

            var seededPlantYaml = Path.Combine(
                forestDir,
                "plants",
                "integration-testing-harness__add-integration-tests",
                "plant.yaml"
            );
            Assert.That(
                File.Exists(seededPlantYaml),
                Is.True,
                "Expected seeded plant at .git-forest/plants/integration-testing-harness__add-integration-tests/plant.yaml"
            );
            var seededPlantYamlBefore = await File.ReadAllTextAsync(seededPlantYaml, Encoding.UTF8);

            // 3b) second reconcile should be idempotent (no duplicates, no rewrites)
            var reconcile2 = await workspace.RunGitForestAsync(
                ["plan", "integration-testing-harness", "reconcile"],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(reconcile2, "git-forest plan reconcile (second run) failed");
            var seededPlantYamlAfter = await File.ReadAllTextAsync(seededPlantYaml, Encoding.UTF8);
            Assert.That(
                seededPlantYamlAfter,
                Is.EqualTo(seededPlantYamlBefore),
                "Expected second reconcile to be idempotent (no plant.yaml changes)"
            );

            // 4) plants list (human output should include key + plan + planter)
            var list = await workspace.RunGitForestAsync(["plants", "list"], timeout: TimeSpan.FromMinutes(1));
            CliTestAsserts.Succeeded(list, "git-forest plants list failed");
            Assert.That(
                list.StdOut,
                Does.Contain("integration-testing-harness:add-integration-tests")
            );
            Assert.That(list.StdOut, Does.Contain("integration-testing-harness"));
            Assert.That(
                list.StdOut,
                Does.Contain("harness-builder"),
                "Expected plants list to include the assigned planter"
            );

            // 4b) plant <key> show should resolve the plant and show details (regression for stub implementation)
            var show = await workspace.RunGitForestAsync(
                ["plant", "integration-testing-harness:add-integration-tests", "show"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(show, "git-forest plant <key> show failed");
            Assert.That(
                show.StdOut,
                Does.Contain("Key: integration-testing-harness:add-integration-tests")
            );
            Assert.That(show.StdOut, Does.Contain("Plan: integration-testing-harness"));

            // 4a2) planters list --json should include the planter IDs
            var plantersJson = await workspace.RunGitForestAsync(
                ["planters", "list", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(plantersJson, "git-forest planters list --json failed");

            using (var plantersDoc = CliTestAsserts.ParseJsonFromStdOut(
                       plantersJson,
                       "git-forest planters list --json"
                   ))
            {
                var rootPlanters = plantersDoc.RootElement;
                Assert.That(
                    rootPlanters.TryGetProperty("planters", out var plantersArray),
                    Is.True
                );
                Assert.That(plantersArray.ValueKind, Is.EqualTo(JsonValueKind.Array));
                Assert.That(
                    plantersArray.GetArrayLength(),
                    Is.GreaterThan(0),
                    "Expected at least one planter after installing the plan"
                );
                var ids = plantersArray
                    .EnumerateArray()
                    .Select(x => x.GetProperty("id").GetString())
                    .ToArray();
                Assert.That(ids, Does.Contain("harness-builder"));
                Assert.That(ids, Does.Contain("integration-test-author"));
                Assert.That(ids, Does.Contain("stability-engineer"));
            }

            // 4c) planners list --json should include the planner IDs
            var plannersJson = await workspace.RunGitForestAsync(
                ["planners", "list", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(plannersJson, "git-forest planners list --json failed");

            using (var plannersDoc = CliTestAsserts.ParseJsonFromStdOut(
                       plannersJson,
                       "git-forest planners list --json"
                   ))
            {
                var rootPlanners = plannersDoc.RootElement;
                Assert.That(rootPlanners.TryGetProperty("planners", out var planners), Is.True);
                Assert.That(planners.ValueKind, Is.EqualTo(JsonValueKind.Array));
                Assert.That(
                    planners.GetArrayLength(),
                    Is.GreaterThan(0),
                    "Expected at least one planner after installing the plan"
                );
                var ids = planners
                    .EnumerateArray()
                    .Select(x => x.GetProperty("id").GetString())
                    .ToArray();
                Assert.That(ids, Does.Contain("integration-surface-mapper"));
                Assert.That(ids, Does.Contain("dependency-harness-planner"));
                Assert.That(ids, Does.Contain("flaky-integration-detector"));
            }

            // 5) plants list --json should reference planner + planters explicitly
            var listJson = await workspace.RunGitForestAsync(
                ["plants", "list", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(listJson, "git-forest plants list --json failed");

            using var doc = CliTestAsserts.ParseJsonFromStdOut(listJson, "git-forest plants list --json");
            var root = doc.RootElement;
            Assert.That(root.TryGetProperty("plants", out var plants), Is.True);
            Assert.That(plants.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(plants.GetArrayLength(), Is.GreaterThanOrEqualTo(1));

            // Ensure reconcile did not create duplicates for the seeded plant.
            var seeded = plants
                .EnumerateArray()
                .Where(p =>
                    string.Equals(
                        p.GetProperty("key").GetString(),
                        "integration-testing-harness:add-integration-tests",
                        StringComparison.Ordinal
                    )
                )
                .ToArray();
            Assert.That(
                seeded.Length,
                Is.EqualTo(1),
                "Expected exactly one plant with key integration-testing-harness:add-integration-tests"
            );

            var first = plants[0];
            Assert.That(
                first.GetProperty("planId").GetString(),
                Is.EqualTo("integration-testing-harness")
            );
            Assert.That(
                first.GetProperty("plannerId").GetString(),
                Is.EqualTo("integration-surface-mapper")
            );

            var planters = first.GetProperty("planters");
            Assert.That(planters.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(
                planters.EnumerateArray().Select(x => x.GetString()).ToArray(),
                Does.Contain("harness-builder")
            );

            workspace.MarkSucceeded();
        }
    }

    [Test]
    public async Task Plant_can_move_through_full_lifecycle_and_creates_branch_and_commit()
    {
        var repoRoot = RepoPaths.FindRepoRoot(TestContext.CurrentContext.TestDirectory);

        var planSource = Path.Combine(
            repoRoot,
            "config",
            "plans",
            "quality-reliability",
            "integration-testing-harness.yaml"
        );
        Assert.That(
            File.Exists(planSource),
            Is.True,
            () => $"Expected plan file to exist in repo: {planSource}"
        );

        await using var workspace = TestWorkspace.Create();

        {
            await GitRepo.CreateAsync(workspace.RepoDirectory, workspace.GitEnvironment);

            // init + install + reconcile
            var init = await workspace.RunGitForestAsync(["init"], timeout: TimeSpan.FromMinutes(3));
            CliTestAsserts.Succeeded(init, "git-forest init failed");

            var install = await workspace.RunGitForestAsync(
                ["plans", "install", planSource],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(install, "git-forest plans install failed");

            var reconcile = await workspace.RunGitForestAsync(
                ["plan", "integration-testing-harness", "reconcile"],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(reconcile, "git-forest plan reconcile failed");

            var plantKey = "integration-testing-harness:add-integration-tests";
            var planterId = "harness-builder";

            // planned -> planted (assign)
            var assign = await workspace.RunGitForestAsync(
                ["plant", plantKey, "assign", planterId],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(assign, "assign failed");

            // planter plant (branch + record branch)
            var plant = await workspace.RunGitForestAsync(
                ["planter", planterId, "plant", plantKey, "--branch", "auto", "--yes"],
                timeout: TimeSpan.FromMinutes(2)
            );
            CliTestAsserts.Succeeded(plant, "planter plant failed");

            // branch exists + checked out
            var headBranch = workspace.GitAsText(["rev-parse", "--abbrev-ref", "HEAD"]).Trim();
            Assert.That(
                headBranch.StartsWith(planterId + "/", StringComparison.OrdinalIgnoreCase),
                Is.True,
                () => $"Expected HEAD branch to start with '{planterId}/' but was '{headBranch}'"
            );

            // grow apply (writes README marker and commits)
            var grow = await workspace.RunGitForestAsync(
                ["planter", planterId, "grow", plantKey, "--mode", "apply"],
                timeout: TimeSpan.FromMinutes(2)
            );
            CliTestAsserts.Succeeded(grow, "grow failed");

            // one new commit should exist on the branch
            var log = workspace.GitAsText(["log", "--max-count", "1", "--pretty=%B"]).Trim();
            Assert.That(log, Does.Contain($"git-forest: grow {plantKey}"));

            // harvestable -> harvested
            var harvest = await workspace.RunGitForestAsync(
                ["plant", plantKey, "harvest"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(harvest, "harvest failed");

            // harvested -> archived
            var archive = await workspace.RunGitForestAsync(
                ["plant", plantKey, "archive"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(archive, "archive failed");

            // status should count archived >= 1
            var status = await workspace.RunGitForestAsync(
                ["status", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(status, "git-forest status --json failed");

            using var statusDoc = CliTestAsserts.ParseJsonFromStdOut(status, "git-forest status --json");
            var plantsByStatus = statusDoc.RootElement.GetProperty("plantsByStatus");
            Assert.That(
                plantsByStatus.GetProperty("archived").GetInt32(),
                Is.GreaterThanOrEqualTo(1)
            );

            workspace.MarkSucceeded();
        }
    }
}
