using System.CommandLine;
using System.CommandLine.Invocation;
using AppPlantCmd = GitForest.Application.Features.Plants.Commands;
using CliPlants = GitForest.Cli.Features.Plants;
using MediatR;

namespace GitForest.Cli.Commands;

public static class PlantCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plantCommand = new Command("plant", "Manage a specific plant");
        var selectorArg = new Argument<string>("selector", "Plant selector (key, slug, or P01)");
        plantCommand.AddArgument(selectorArg);

        var showCommand = new Command("show", "Show plant details");
        showCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);
            try
            {
                var plant = await mediator.Send(new CliPlants.GetPlantQuery(Selector: selector));
                if (output.Json)
                {
                    output.WriteJson(new
                    {
                        plant = new
                        {
                            key = plant.Key,
                            status = plant.Status,
                            title = plant.Title,
                            planId = plant.PlanId,
                            plannerId = plant.PlannerId,
                            planters = plant.AssignedPlanters.ToArray(),
                            branches = plant.Branches.ToArray(),
                            createdAt = plant.CreatedAt,
                            updatedAt = plant.UpdatedAt
                        }
                    });
                }
                else
                {
                    var plantersText = plant.AssignedPlanters.Count == 0 ? "-" : string.Join(", ", plant.AssignedPlanters);
                    var branchesText = plant.Branches.Count == 0 ? "-" : string.Join(", ", plant.Branches);
                    output.WriteLine($"Key: {plant.Key}");
                    output.WriteLine($"Status: {plant.Status}");
                    output.WriteLine($"Title: {plant.Title}");
                    output.WriteLine($"Plan: {plant.PlanId}");
                    output.WriteLine($"Planner: {plant.PlannerId ?? "-"}");
                    output.WriteLine($"Planters: {plantersText}");
                    output.WriteLine($"Branches: {branchesText}");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                // For single-plant lookup, treat missing forest as "not found" (keeps CLI usable
                // in fresh repos and matches the smoke-test contract).
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (ForestStore.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (ForestStore.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(
                        code: "plant_ambiguous",
                        message: "Plant selector is ambiguous",
                        details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants:");
                    foreach (var key in ex.Matches.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    {
                        output.WriteErrorLine($"- {key}");
                    }
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
        });

        var dryRunOption = new Option<bool>("--dry-run", "Show what would be done without applying");

        var assignCommand = new Command("assign", "Assign a planter to this plant");
        var planterIdArg = new Argument<string>("planter-id", "Planter identifier");
        assignCommand.AddArgument(planterIdArg);
        assignCommand.AddOption(dryRunOption);
        assignCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);
            var planterId = context.ParseResult.GetValueForArgument(planterIdArg);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            try
            {
                var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                if (!ForestStore.IsInitialized(forestDir))
                {
                    throw new ForestStore.ForestNotInitializedException(forestDir);
                }

                var updated = await mediator.Send(new AppPlantCmd.AssignPlanterToPlantCommand(Selector: selector, PlanterId: planterId, DryRun: dryRun));

                if (output.Json)
                {
                    output.WriteJson(new { status = "assigned", dryRun, plantKey = updated.Key, planterId = planterId.Trim(), plantStatus = updated.Status });
                }
                else
                {
                    output.WriteLine(dryRun
                        ? $"Would assign planter '{planterId}' to plant '{updated.Key}'"
                        : $"Assigned planter '{planterId}' to plant '{updated.Key}'");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
                }
                else
                {
                    output.WriteErrorLine("Error: forest not initialized");
                }

                context.ExitCode = ExitCodes.ForestNotInitialized;
            }
            catch (AppPlantCmd.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_ambiguous", message: "Plant selector is ambiguous", details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
        });

        var unassignCommand = new Command("unassign", "Unassign a planter from this plant");
        unassignCommand.AddArgument(planterIdArg);
        unassignCommand.AddOption(dryRunOption);
        unassignCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);
            var planterId = context.ParseResult.GetValueForArgument(planterIdArg);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            try
            {
                var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                if (!ForestStore.IsInitialized(forestDir))
                {
                    throw new ForestStore.ForestNotInitializedException(forestDir);
                }

                var updated = await mediator.Send(new AppPlantCmd.UnassignPlanterFromPlantCommand(Selector: selector, PlanterId: planterId, DryRun: dryRun));

                if (output.Json)
                {
                    output.WriteJson(new { status = "unassigned", dryRun, plantKey = updated.Key, planterId = planterId.Trim(), plantStatus = updated.Status });
                }
                else
                {
                    output.WriteLine(dryRun
                        ? $"Would unassign planter '{planterId}' from plant '{updated.Key}'"
                        : $"Unassigned planter '{planterId}' from plant '{updated.Key}'");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
                }
                else
                {
                    output.WriteErrorLine("Error: forest not initialized");
                }

                context.ExitCode = ExitCodes.ForestNotInitialized;
            }
            catch (AppPlantCmd.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_ambiguous", message: "Plant selector is ambiguous", details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
        });

        var branchesCommand = new Command("branches", "Manage plant branches");
        var branchesListCommand = new Command("list", "List branches recorded for this plant");
        branchesListCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);

            try
            {
                var result = await mediator.Send(new CliPlants.ListPlantBranchesQuery(Selector: selector));
                var branches = result.Branches;

                if (output.Json)
                {
                    output.WriteJson(new { plantKey = result.PlantKey, branches = branches.ToArray() });
                }
                else
                {
                    if (branches.Length == 0)
                    {
                        output.WriteLine("No branches recorded");
                    }
                    else
                    {
                        foreach (var br in branches)
                        {
                            output.WriteLine(br);
                        }
                    }
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
                }
                else
                {
                    output.WriteErrorLine("Error: forest not initialized");
                }

                context.ExitCode = ExitCodes.ForestNotInitialized;
            }
            catch (ForestStore.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (ForestStore.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_ambiguous", message: "Plant selector is ambiguous", details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
        });
        branchesCommand.AddCommand(branchesListCommand);

        var forceOption = new Option<bool>("--force", "Force state transition even if current status is unexpected");

        var harvestCommand = new Command("harvest", "Mark plant as harvested");
        harvestCommand.AddOption(forceOption);
        harvestCommand.AddOption(dryRunOption);
        harvestCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            try
            {
                var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                if (!ForestStore.IsInitialized(forestDir))
                {
                    throw new ForestStore.ForestNotInitializedException(forestDir);
                }

                var updated = await mediator.Send(new AppPlantCmd.HarvestPlantCommand(Selector: selector, Force: force, DryRun: dryRun));

                if (output.Json)
                {
                    output.WriteJson(new { status = "harvested", dryRun, plantKey = updated.Key });
                }
                else
                {
                    output.WriteLine(dryRun ? $"Would harvest '{updated.Key}'" : $"Harvested '{updated.Key}'");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (InvalidOperationException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "invalid_state", message: ex.Message, details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Error: {ex.Message}");
                }

                context.ExitCode = ExitCodes.InvalidArguments;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
                }
                else
                {
                    output.WriteErrorLine("Error: forest not initialized");
                }

                context.ExitCode = ExitCodes.ForestNotInitialized;
            }
            catch (AppPlantCmd.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_ambiguous", message: "Plant selector is ambiguous", details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
        });

        var archiveCommand = new Command("archive", "Archive plant");
        archiveCommand.AddOption(forceOption);
        archiveCommand.AddOption(dryRunOption);
        archiveCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            try
            {
                var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                if (!ForestStore.IsInitialized(forestDir))
                {
                    throw new ForestStore.ForestNotInitializedException(forestDir);
                }

                var updated = await mediator.Send(new AppPlantCmd.ArchivePlantCommand(Selector: selector, Force: force, DryRun: dryRun));

                if (output.Json)
                {
                    output.WriteJson(new { status = "archived", dryRun, plantKey = updated.Key });
                }
                else
                {
                    output.WriteLine(dryRun ? $"Would archive '{updated.Key}'" : $"Archived '{updated.Key}'");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (InvalidOperationException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "invalid_state", message: ex.Message, details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Error: {ex.Message}");
                }

                context.ExitCode = ExitCodes.InvalidArguments;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
                }
                else
                {
                    output.WriteErrorLine("Error: forest not initialized");
                }

                context.ExitCode = ExitCodes.ForestNotInitialized;
            }
            catch (AppPlantCmd.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_ambiguous", message: "Plant selector is ambiguous", details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
        });

        plantCommand.AddCommand(showCommand);
        plantCommand.AddCommand(assignCommand);
        plantCommand.AddCommand(unassignCommand);
        plantCommand.AddCommand(branchesCommand);
        plantCommand.AddCommand(harvestCommand);
        plantCommand.AddCommand(archiveCommand);
        return plantCommand;
    }
}


