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

            // Step 1: Initialize
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

            // Step 2: Install plan
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
                "developer-experience",
                "plan.yaml"
            );
            Assert.That(
                File.Exists(installedPlanYaml),
                Is.True,
                "Expected installed plan at .git-forest/plans/developer-experience/plan.yaml"
            );

            // Step 3: Reconcile
            var reconcile = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plan", "developer-experience", "reconcile"],
                timeout: TimeSpan.FromMinutes(3)
            );
            Assert.That(
                reconcile.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plan reconcile failed.\nSTDOUT:\n{reconcile.StdOut}\nSTDERR:\n{reconcile.StdErr}"
            );

            // Verify at least one plant was seeded
            var plantsDir = Path.Combine(forestDir, "plants");
            var plantDirs = Directory.GetDirectories(plantsDir);
            Assert.That(
                plantDirs.Length,
                Is.GreaterThan(0),
                "Expected at least one plant directory after reconcile"
            );

            // Step 4: View plants
            var plantsList = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plants", "list", "--plan", "developer-experience"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                plantsList.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plants list failed.\nSTDOUT:\n{plantsList.StdOut}\nSTDERR:\n{plantsList.StdErr}"
            );
            Assert.That(
                plantsList.StdOut,
                Does.Contain("developer-experience"),
                "Expected plants list to mention developer-experience plan"
            );

            // Get plants list in JSON to extract the first plant key
            var plantsListJson = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plants", "list", "--plan", "developer-experience", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                plantsListJson.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plants list --json failed.\nSTDOUT:\n{plantsListJson.StdOut}\nSTDERR:\n{plantsListJson.StdErr}"
            );

            using var plantsDoc = JsonDocument.Parse(plantsListJson.StdOut.Trim());
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
            var plantShow = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plant", plantKey, "show"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                plantShow.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plant {plantKey} show failed.\nSTDOUT:\n{plantShow.StdOut}\nSTDERR:\n{plantShow.StdErr}"
            );
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
            var plant = await GitForestCli.RunAsync(
                workingRepoDir,
                ["planter", "build-optimizer", "plant", plantKey, "--branch", "auto", "--yes"],
                timeout: TimeSpan.FromMinutes(3)
            );
            Assert.That(
                plant.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest planter build-optimizer plant failed.\nSTDOUT:\n{plant.StdOut}\nSTDERR:\n{plant.StdErr}"
            );
            Assert.That(
                plant.StdOut,
                Does.Contain("Planted"),
                "Expected plant command to confirm planting"
            );

            // Verify branch was created
            var branches = Git.AsText(workingRepoDir, ["branch", "--list"]);
            Assert.That(
                branches,
                Does.Contain("build-optimizer/"),
                "Expected a build-optimizer/* branch to be created by the planter"
            );

            // Verify plant status changed to "planted"
            var plantShowAfterPlant = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plant", plantKey, "show", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                plantShowAfterPlant.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plant show after planting failed.\nSTDOUT:\n{plantShowAfterPlant.StdOut}\nSTDERR:\n{plantShowAfterPlant.StdErr}"
            );

            using var plantAfterPlantDoc = JsonDocument.Parse(plantShowAfterPlant.StdOut.Trim());
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
            var grow = await GitForestCli.RunAsync(
                workingRepoDir,
                ["planter", "build-optimizer", "grow", plantKey, "--mode", "propose"],
                timeout: TimeSpan.FromMinutes(3)
            );
            Assert.That(
                grow.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest planter build-optimizer grow failed.\nSTDOUT:\n{grow.StdOut}\nSTDERR:\n{grow.StdErr}"
            );
            Assert.That(
                grow.StdOut,
                Does.Contain("Grew"),
                "Expected grow command to confirm growing"
            );

            // Verify plant status changed to "harvestable"
            var plantShowAfterGrow = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plant", plantKey, "show", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                plantShowAfterGrow.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plant show after growing failed.\nSTDOUT:\n{plantShowAfterGrow.StdOut}\nSTDERR:\n{plantShowAfterGrow.StdErr}"
            );

            using var plantAfterGrowDoc = JsonDocument.Parse(plantShowAfterGrow.StdOut.Trim());
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
            var status = await GitForestCli.RunAsync(
                workingRepoDir,
                ["status"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                status.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest status failed.\nSTDOUT:\n{status.StdOut}\nSTDERR:\n{status.StdErr}"
            );
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
            var statusJson = await GitForestCli.RunAsync(
                workingRepoDir,
                ["status", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                statusJson.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest status --json failed.\nSTDOUT:\n{statusJson.StdOut}\nSTDERR:\n{statusJson.StdErr}"
            );

            using var statusDoc = JsonDocument.Parse(statusJson.StdOut.Trim());
            var plantsByStatus = statusDoc.RootElement.GetProperty("plantsByStatus");
            var harvestableCount = plantsByStatus.GetProperty("harvestable").GetInt32();
            Assert.That(
                harvestableCount,
                Is.GreaterThan(0),
                "Expected at least one harvestable plant in status"
            );

            // Step 9: Harvest
            var harvest = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plant", plantKey, "harvest"],
                timeout: TimeSpan.FromMinutes(3)
            );
            Assert.That(
                harvest.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plant harvest failed.\nSTDOUT:\n{harvest.StdOut}\nSTDERR:\n{harvest.StdErr}"
            );
            Assert.That(
                harvest.StdOut,
                Does.Contain("Harvested"),
                "Expected harvest command to confirm harvesting"
            );

            // Verify plant status changed to "harvested"
            var plantShowAfterHarvest = await GitForestCli.RunAsync(
                workingRepoDir,
                ["plant", plantKey, "show", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                plantShowAfterHarvest.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest plant show after harvest failed.\nSTDOUT:\n{plantShowAfterHarvest.StdOut}\nSTDERR:\n{plantShowAfterHarvest.StdErr}"
            );

            using var plantAfterHarvestDoc = JsonDocument.Parse(
                plantShowAfterHarvest.StdOut.Trim()
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
            var finalStatus = await GitForestCli.RunAsync(
                workingRepoDir,
                ["status", "--json"],
                timeout: TimeSpan.FromMinutes(1)
            );
            Assert.That(
                finalStatus.ExitCode,
                Is.EqualTo(0),
                () =>
                    $"git-forest final status failed.\nSTDOUT:\n{finalStatus.StdOut}\nSTDERR:\n{finalStatus.StdErr}"
            );

            using var finalStatusDoc = JsonDocument.Parse(finalStatus.StdOut.Trim());
            var finalPlantsByStatus = finalStatusDoc.RootElement.GetProperty("plantsByStatus");
            var harvestedCount = finalPlantsByStatus.GetProperty("harvested").GetInt32();
            Assert.That(
                harvestedCount,
                Is.GreaterThan(0),
                "Expected at least one harvested plant in final status"
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
