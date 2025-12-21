using System.Text.Json;
using NUnit.Framework;

namespace GitForest.Cli.IntegrationTests;

[TestFixture]
[Category("Integration")]
[Parallelizable(ParallelScope.All)]
public sealed class QuickStartGuideWorkflowTests
{
    [Test]
    public async Task Quick_start_guide_workflow_executes_successfully()
    {
        var repoRoot = RepoPaths.FindRepoRoot(TestContext.CurrentContext.TestDirectory);

        var planSource = Path.Combine(
            repoRoot,
            "config",
            "plans",
            "team-process",
            "developer-experience.yaml"
        );
        Assert.That(
            File.Exists(planSource),
            Is.True,
            () => $"Expected plan file to exist in repo: {planSource}"
        );

        await using var workspace = TestWorkspace.Create();

        {
            await GitRepo.CreateAsync(workspace.RepoDirectory, workspace.GitEnvironment);

            // Step 1: Initialize
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

            // Step 2: Install plan
            var install = await workspace.RunGitForestAsync(
                ["plans", "install", planSource],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(install, "git-forest plans install failed");

            var installedPlanYaml = Path.Combine(
                forestDir,
                "plans",
                "developer-experience",
                "plan.yaml"
            );
            Assert.That(
                File.Exists(installedPlanYaml),
                Is.True,
                "Expected installed plan at .git-forest/plans/developer-experience/plan.yaml"
            );

            // Step 3: Reconcile
            var reconcile = await workspace.RunGitForestAsync(
                ["plan", "developer-experience", "reconcile"],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(reconcile, "git-forest plan reconcile failed");

            // Verify at least one plant was seeded
            var plantsDir = Path.Combine(forestDir, "plants");
            var plantDirs = Directory.GetDirectories(plantsDir);
            Assert.That(
                plantDirs.Length,
                Is.GreaterThan(0),
                "Expected at least one plant directory after reconcile"
            );

            // Step 4: View plants
            var plantsList = await workspace.RunGitForestAsync(
                ["plants", "list", "--plan", "developer-experience"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(plantsList, "git-forest plants list failed");
            Assert.That(
                plantsList.StdOut,
                Does.Contain("developer-experience"),
                "Expected plants list to mention developer-experience plan"
            );

            // Get plants list in JSON to extract the first plant key
            var plantsListJson = await workspace.RunGitForestAsync(
                ["plants", "list", "--plan", "developer-experience", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(plantsListJson, "git-forest plants list --json failed");

            using var plantsDoc = CliTestAsserts.ParseJsonFromStdOut(
                plantsListJson,
                "git-forest plants list --json"
            );
            var plants = plantsDoc.RootElement.GetProperty("plants");
            Assert.That(
                plants.GetArrayLength(),
                Is.GreaterThan(0),
                "Expected at least one plant in JSON output"
            );

            var firstPlant = plants[0];
            var plantKey = firstPlant.GetProperty("key").GetString()!;
            // Use plant key (can also use sequence like P01, but key is always available)

            // Step 5: Pick a plant (use P01 or full key) - we'll use the key
            var plantShow = await workspace.RunGitForestAsync(
                ["plant", plantKey, "show"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(plantShow, $"git-forest plant {plantKey} show failed");
            Assert.That(
                plantShow.StdOut,
                Does.Contain(plantKey),
                $"Expected plant show output to contain plant key: {plantKey}"
            );
            Assert.That(
                plantShow.StdOut,
                Does.Contain("developer-experience"),
                "Expected plant show to mention the plan"
            );

            // Step 6: Plant it (assign + create branch)
            var plant = await workspace.RunGitForestAsync(
                ["planter", "build-optimizer", "plant", plantKey, "--branch", "auto", "--yes"],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(plant, "git-forest planter build-optimizer plant failed");
            Assert.That(
                plant.StdOut,
                Does.Contain("Planted"),
                "Expected plant command to confirm planting"
            );

            // Verify branch was created
            var branches = workspace.GitAsText(["branch", "--list"]);
            Assert.That(
                branches,
                Does.Contain("build-optimizer/"),
                "Expected a build-optimizer/* branch to be created by the planter"
            );

            // Verify plant status changed to "planted"
            var plantShowAfterPlant = await workspace.RunGitForestAsync(
                ["plant", plantKey, "show", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(plantShowAfterPlant, "git-forest plant show after planting failed");

            using var plantAfterPlantDoc = CliTestAsserts.ParseJsonFromStdOut(
                plantShowAfterPlant,
                "git-forest plant show after planting --json"
            );
            var statusAfterPlant = plantAfterPlantDoc
                .RootElement.GetProperty("plant")
                .GetProperty("status")
                .GetString();
            Assert.That(
                statusAfterPlant,
                Is.EqualTo("planted"),
                "Expected plant status to be 'planted' after planting"
            );

            // Step 7: Grow it (propose changes)
            var grow = await workspace.RunGitForestAsync(
                ["planter", "build-optimizer", "grow", plantKey, "--mode", "propose"],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(grow, "git-forest planter build-optimizer grow failed");
            Assert.That(
                grow.StdOut,
                Does.Contain("Grew"),
                "Expected grow command to confirm growing"
            );

            // Verify plant status changed to "harvestable"
            var plantShowAfterGrow = await workspace.RunGitForestAsync(
                ["plant", plantKey, "show", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(plantShowAfterGrow, "git-forest plant show after growing failed");

            using var plantAfterGrowDoc = CliTestAsserts.ParseJsonFromStdOut(
                plantShowAfterGrow,
                "git-forest plant show after growing --json"
            );
            var statusAfterGrow = plantAfterGrowDoc
                .RootElement.GetProperty("plant")
                .GetProperty("status")
                .GetString();
            Assert.That(
                statusAfterGrow,
                Is.EqualTo("harvestable"),
                "Expected plant status to be 'harvestable' after growing"
            );

            // Step 8: Check status
            var status = await workspace.RunGitForestAsync(["status"], timeout: TimeSpan.FromMinutes(1));
            CliTestAsserts.Succeeded(status, "git-forest status failed");
            Assert.That(
                status.StdOut,
                Does.Contain("initialized"),
                "Expected status to show forest is initialized"
            );
            Assert.That(
                status.StdOut,
                Does.Contain("harvestable"),
                "Expected status to mention harvestable plants"
            );

            // Verify status JSON output
            var statusJson = await workspace.RunGitForestAsync(
                ["status", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(statusJson, "git-forest status --json failed");

            using var statusDoc = CliTestAsserts.ParseJsonFromStdOut(statusJson, "git-forest status --json");
            var plantsByStatus = statusDoc.RootElement.GetProperty("plantsByStatus");
            var harvestableCount = plantsByStatus.GetProperty("harvestable").GetInt32();
            Assert.That(
                harvestableCount,
                Is.GreaterThan(0),
                "Expected at least one harvestable plant in status"
            );

            // Step 9: Harvest
            var harvest = await workspace.RunGitForestAsync(
                ["plant", plantKey, "harvest"],
                timeout: TimeSpan.FromMinutes(3)
            );
            CliTestAsserts.Succeeded(harvest, "git-forest plant harvest failed");
            Assert.That(
                harvest.StdOut,
                Does.Contain("Harvested"),
                "Expected harvest command to confirm harvesting"
            );

            // Verify plant status changed to "harvested"
            var plantShowAfterHarvest = await workspace.RunGitForestAsync(
                ["plant", plantKey, "show", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(plantShowAfterHarvest, "git-forest plant show after harvest failed");

            using var plantAfterHarvestDoc = CliTestAsserts.ParseJsonFromStdOut(
                plantShowAfterHarvest,
                "git-forest plant show after harvest --json"
            );
            var statusAfterHarvest = plantAfterHarvestDoc
                .RootElement.GetProperty("plant")
                .GetProperty("status")
                .GetString();
            Assert.That(
                statusAfterHarvest,
                Is.EqualTo("harvested"),
                "Expected plant status to be 'harvested' after harvesting"
            );

            // Final verification: status should show the harvested plant
            var finalStatus = await workspace.RunGitForestAsync(
                ["status", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            CliTestAsserts.Succeeded(finalStatus, "git-forest final status failed");

            using var finalStatusDoc = CliTestAsserts.ParseJsonFromStdOut(
                finalStatus,
                "git-forest final status --json"
            );
            var finalPlantsByStatus = finalStatusDoc.RootElement.GetProperty("plantsByStatus");
            var harvestedCount = finalPlantsByStatus.GetProperty("harvested").GetInt32();
            Assert.That(
                harvestedCount,
                Is.GreaterThan(0),
                "Expected at least one harvested plant in final status"
            );

            workspace.MarkSucceeded();
        }
    }
}
