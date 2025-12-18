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

        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "git-forest-integration",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(tempRoot);

        var workingRepoDir = Path.Combine(tempRoot, "repo");
        Directory.CreateDirectory(workingRepoDir);

        try
        {
            await GitRepo.CreateAsync(workingRepoDir, TestEnvironments.Git);

            // 1) init
            var init = await GitForestCli.RunAsync(
                workingRepoDir,
                ["init"],
                timeout: TimeSpan.FromMinutes(3)
            );
            Assert.That(
                init.ExitCode,
                Is.EqualTo(0),
                () => $"git-forest init failed.\nSTDOUT:\n{init.StdOut}\nSTDERR:\n{init.StdErr}"
            );

            var forestDir = Path.Combine(workingRepoDir, ".git-forest");
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
            var install = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plans", "install", planSource],
                timeout: TimeSpan.FromMinutes(3)
            );
            Assert.That(
                install.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plans install failed.\nSTDOUT:\n{install.StdOut}\nSTDERR:\n{install.StdErr}"
            );

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
            var reconcile = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plan", "integration-testing-harness", "reconcile"],
                timeout: TimeSpan.FromMinutes(3)
            );
            Assert.That(
                reconcile.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plan reconcile failed.\nSTDOUT:\n{reconcile.StdOut}\nSTDERR:\n{reconcile.StdErr}"
            );

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
            var reconcile2 = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plan", "integration-testing-harness", "reconcile"],
                timeout: TimeSpan.FromMinutes(3)
            );
            Assert.That(
                reconcile2.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plan reconcile (second run) failed.\nSTDOUT:\n{reconcile2.StdOut}\nSTDERR:\n{reconcile2.StdErr}"
            );
            var seededPlantYamlAfter = await File.ReadAllTextAsync(seededPlantYaml, Encoding.UTF8);
            Assert.That(
                seededPlantYamlAfter,
                Is.EqualTo(seededPlantYamlBefore),
                "Expected second reconcile to be idempotent (no plant.yaml changes)"
            );

            // 4) plants list (human output should include key + plan + planter)
            var list = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plants", "list"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                list.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plants list failed.\nSTDOUT:\n{list.StdOut}\nSTDERR:\n{list.StdErr}"
            );
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
            var show = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plant", "integration-testing-harness:add-integration-tests", "show"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                show.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plant <key> show failed.\nSTDOUT:\n{show.StdOut}\nSTDERR:\n{show.StdErr}"
            );
            Assert.That(
                show.StdOut,
                Does.Contain("Key: integration-testing-harness:add-integration-tests")
            );
            Assert.That(show.StdOut, Does.Contain("Plan: integration-testing-harness"));

            // 4a2) planters list --json should include the planter IDs
            var plantersJson = await GitForestCli.RunAsync(
                workingRepoDir,
                ["planters", "list", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                plantersJson.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest planters list --json failed.\nSTDOUT:\n{plantersJson.StdOut}\nSTDERR:\n{plantersJson.StdErr}"
            );

            using (var plantersDoc = JsonDocument.Parse(plantersJson.StdOut.Trim()))
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
            var plannersJson = await GitForestCli.RunAsync(
                workingRepoDir,
                ["planners", "list", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                plannersJson.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest planners list --json failed.\nSTDOUT:\n{plannersJson.StdOut}\nSTDERR:\n{plannersJson.StdErr}"
            );

            using (var plannersDoc = JsonDocument.Parse(plannersJson.StdOut.Trim()))
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
            var listJson = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plants", "list", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                listJson.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plants list --json failed.\nSTDOUT:\n{listJson.StdOut}\nSTDERR:\n{listJson.StdErr}"
            );

            using var doc = JsonDocument.Parse(listJson.StdOut.Trim());
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

        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "git-forest-integration",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(tempRoot);

        var workingRepoDir = Path.Combine(tempRoot, "repo");
        Directory.CreateDirectory(workingRepoDir);

        try
        {
            await GitRepo.CreateAsync(workingRepoDir, TestEnvironments.Git);

            // init + install + reconcile
            Assert.That(
                (
                    await GitForestCli.RunAsync(
                        workingRepoDir,
                        ["init"],
                        timeout: TimeSpan.FromMinutes(3)
                    )
                ).ExitCode,
                Is.EqualTo(0)
            );
            Assert.That(
                (
                    await GitForestCli.RunAsync(
                        workingRepoDir,
                        ["plans", "install", planSource],
                        timeout: TimeSpan.FromMinutes(3)
                    )
                ).ExitCode,
                Is.EqualTo(0)
            );
            Assert.That(
                (
                    await GitForestCli.RunAsync(
                        workingRepoDir,
                        ["plan", "integration-testing-harness", "reconcile"],
                        timeout: TimeSpan.FromMinutes(3)
                    )
                ).ExitCode,
                Is.EqualTo(0)
            );

            var plantKey = "integration-testing-harness:add-integration-tests";
            var planterId = "harness-builder";

            // planned -> planted (assign)
            var assign = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plant", plantKey, "assign", planterId],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                assign.ExitCode,
                Is.EqualTo(0),
                () => $"assign failed\nSTDOUT:\n{assign.StdOut}\nSTDERR:\n{assign.StdErr}"
            );

            // planter plant (branch + record branch)
            var plant = await GitForestCli.RunAsync(
                workingRepoDir,
                ["planter", planterId, "plant", plantKey, "--branch", "auto", "--yes"],
                timeout: TimeSpan.FromMinutes(2)
            );
            Assert.That(
                plant.ExitCode,
                Is.EqualTo(0),
                () => $"planter plant failed\nSTDOUT:\n{plant.StdOut}\nSTDERR:\n{plant.StdErr}"
            );

            // branch exists + checked out
            var headBranch = Git.AsText(workingRepoDir, ["rev-parse", "--abbrev-ref", "HEAD"])
                .Trim();
            Assert.That(
                headBranch.StartsWith(planterId + "/", StringComparison.OrdinalIgnoreCase),
                Is.True,
                () => $"Expected HEAD branch to start with '{planterId}/' but was '{headBranch}'"
            );

            // grow apply (writes README marker and commits)
            var grow = await GitForestCli.RunAsync(
                workingRepoDir,
                ["planter", planterId, "grow", plantKey, "--mode", "apply"],
                timeout: TimeSpan.FromMinutes(2)
            );
            Assert.That(
                grow.ExitCode,
                Is.EqualTo(0),
                () => $"grow failed\nSTDOUT:\n{grow.StdOut}\nSTDERR:\n{grow.StdErr}"
            );

            // one new commit should exist on the branch
            var log = Git.AsText(workingRepoDir, ["log", "--max-count", "1", "--pretty=%B"]).Trim();
            Assert.That(log, Does.Contain($"git-forest: grow {plantKey}"));

            // harvestable -> harvested
            var harvest = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plant", plantKey, "harvest"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                harvest.ExitCode,
                Is.EqualTo(0),
                () => $"harvest failed\nSTDOUT:\n{harvest.StdOut}\nSTDERR:\n{harvest.StdErr}"
            );

            // harvested -> archived
            var archive = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plant", plantKey, "archive"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                archive.ExitCode,
                Is.EqualTo(0),
                () => $"archive failed\nSTDOUT:\n{archive.StdOut}\nSTDERR:\n{archive.StdErr}"
            );

            // status should count archived >= 1
            var status = await GitForestCli.RunAsync(
                workingRepoDir,
                ["status", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(status.ExitCode, Is.EqualTo(0));
            using var statusDoc = JsonDocument.Parse(status.StdOut.Trim());
            var plantsByStatus = statusDoc.RootElement.GetProperty("plantsByStatus");
            Assert.That(
                plantsByStatus.GetProperty("archived").GetInt32(),
                Is.GreaterThanOrEqualTo(1)
            );
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
}
