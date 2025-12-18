using System.CommandLine;
using MediatR;
using AppPlantCmd = GitForest.Application.Features.Plants.Commands;
using CliPlants = GitForest.Cli.Features.Plants;

namespace GitForest.Cli.Commands;

public static class PlantCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plantCommand = new Command("plant", "Manage a specific plant");
        var selectorArg = new Argument<string>("selector")
        {
            Description = "Plant selector (key, slug, or P01)",
        };
        plantCommand.Arguments.Add(selectorArg);

        var showCommand = new Command("show", "Show plant details");
        showCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetRequiredValue(selectorArg);
                try
                {
                    var plant = await mediator.Send(
                        new CliPlants.GetPlantQuery(Selector: selector),
                        token
                    );
                    if (output.Json)
                    {
                        output.WriteJson(
                            new
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
                                    updatedAt = plant.UpdatedAt,
                                },
                            }
                        );
                    }
                    else
                    {
                        var plantersText =
                            plant.AssignedPlanters.Count == 0
                                ? "-"
                                : string.Join(", ", plant.AssignedPlanters);
                        var branchesText =
                            plant.Branches.Count == 0 ? "-" : string.Join(", ", plant.Branches);
                        output.WriteLine($"Key: {plant.Key}");
                        output.WriteLine($"Status: {plant.Status}");
                        output.WriteLine($"Title: {plant.Title}");
                        output.WriteLine($"Plan: {plant.PlanId}");
                        output.WriteLine($"Planner: {plant.PlannerId ?? "-"}");
                        output.WriteLine($"Planters: {plantersText}");
                        output.WriteLine($"Branches: {branchesText}");
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    // For single-plant lookup, treat missing forest as "not found" (keeps CLI usable
                    // in fresh repos and matches the smoke-test contract).
                    return WritePlantNotFound(output, selector);
                }
                catch (ForestStore.PlantNotFoundException)
                {
                    return WritePlantNotFound(output, selector);
                }
                catch (ForestStore.PlantAmbiguousSelectorException ex)
                {
                    return WritePlantAmbiguous(
                        output,
                        selector: ex.Selector,
                        matches: ex.Matches,
                        printMatches: true
                    );
                }
            }
        );

        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };

        var assignCommand = new Command("assign", "Assign a planter to this plant");
        var planterIdArg = new Argument<string>("planter-id")
        {
            Description = "Planter identifier",
        };
        assignCommand.Arguments.Add(planterIdArg);
        assignCommand.Options.Add(dryRunOption);
        assignCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetRequiredValue(selectorArg);
                var planterId = parseResult.GetRequiredValue(planterIdArg);
                var dryRun = parseResult.GetValue(dryRunOption);
                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var updated = await mediator.Send(
                        new AppPlantCmd.AssignPlanterToPlantCommand(
                            Selector: selector,
                            PlanterId: planterId,
                            DryRun: dryRun
                        ),
                        token
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "assigned",
                                dryRun,
                                plantKey = updated.Key,
                                planterId = planterId.Trim(),
                                plantStatus = updated.Status,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine(
                            dryRun
                                ? $"Would assign planter '{planterId}' to plant '{updated.Key}'"
                                : $"Assigned planter '{planterId}' to plant '{updated.Key}'"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return WriteForestNotInitialized(output);
                }
                catch (AppPlantCmd.PlantNotFoundException)
                {
                    return WritePlantNotFound(output, selector);
                }
                catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
                {
                    return WritePlantAmbiguous(
                        output,
                        selector: ex.Selector,
                        matches: ex.Matches,
                        printMatches: false
                    );
                }
            }
        );

        var unassignCommand = new Command("unassign", "Unassign a planter from this plant");
        unassignCommand.Arguments.Add(planterIdArg);
        unassignCommand.Options.Add(dryRunOption);
        unassignCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetRequiredValue(selectorArg);
                var planterId = parseResult.GetRequiredValue(planterIdArg);
                var dryRun = parseResult.GetValue(dryRunOption);
                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var updated = await mediator.Send(
                        new AppPlantCmd.UnassignPlanterFromPlantCommand(
                            Selector: selector,
                            PlanterId: planterId,
                            DryRun: dryRun
                        ),
                        token
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "unassigned",
                                dryRun,
                                plantKey = updated.Key,
                                planterId = planterId.Trim(),
                                plantStatus = updated.Status,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine(
                            dryRun
                                ? $"Would unassign planter '{planterId}' from plant '{updated.Key}'"
                                : $"Unassigned planter '{planterId}' from plant '{updated.Key}'"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return WriteForestNotInitialized(output);
                }
                catch (AppPlantCmd.PlantNotFoundException)
                {
                    return WritePlantNotFound(output, selector);
                }
                catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
                {
                    return WritePlantAmbiguous(
                        output,
                        selector: ex.Selector,
                        matches: ex.Matches,
                        printMatches: false
                    );
                }
            }
        );

        var branchesCommand = new Command("branches", "Manage plant branches");
        var branchesListCommand = new Command("list", "List branches recorded for this plant");
        branchesListCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetRequiredValue(selectorArg);

                try
                {
                    var result = await mediator.Send(
                        new CliPlants.ListPlantBranchesQuery(Selector: selector),
                        token
                    );
                    var branches = result.Branches;

                    if (output.Json)
                    {
                        output.WriteJson(
                            new { plantKey = result.PlantKey, branches = branches.ToArray() }
                        );
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

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return WriteForestNotInitialized(output);
                }
                catch (ForestStore.PlantNotFoundException)
                {
                    return WritePlantNotFound(output, selector);
                }
                catch (ForestStore.PlantAmbiguousSelectorException ex)
                {
                    return WritePlantAmbiguous(
                        output,
                        selector: ex.Selector,
                        matches: ex.Matches,
                        printMatches: false
                    );
                }
            }
        );
        branchesCommand.Subcommands.Add(branchesListCommand);

        var forceOption = new Option<bool>("--force")
        {
            Description = "Force state transition even if current status is unexpected",
        };

        var harvestCommand = new Command("harvest", "Mark plant as harvested");
        harvestCommand.Options.Add(forceOption);
        harvestCommand.Options.Add(dryRunOption);
        harvestCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetRequiredValue(selectorArg);
                var force = parseResult.GetValue(forceOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var updated = await mediator.Send(
                        new AppPlantCmd.HarvestPlantCommand(
                            Selector: selector,
                            Force: force,
                            DryRun: dryRun
                        ),
                        token
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "harvested",
                                dryRun,
                                plantKey = updated.Key,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine(
                            dryRun ? $"Would harvest '{updated.Key}'" : $"Harvested '{updated.Key}'"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (InvalidOperationException ex)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(
                            code: "invalid_state",
                            message: ex.Message,
                            details: new { selector }
                        );
                    }
                    else
                    {
                        output.WriteErrorLine($"Error: {ex.Message}");
                    }

                    return ExitCodes.InvalidArguments;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return WriteForestNotInitialized(output);
                }
                catch (AppPlantCmd.PlantNotFoundException)
                {
                    return WritePlantNotFound(output, selector);
                }
                catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
                {
                    return WritePlantAmbiguous(
                        output,
                        selector: ex.Selector,
                        matches: ex.Matches,
                        printMatches: false
                    );
                }
            }
        );

        var archiveCommand = new Command("archive", "Archive plant");
        archiveCommand.Options.Add(forceOption);
        archiveCommand.Options.Add(dryRunOption);
        archiveCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetRequiredValue(selectorArg);
                var force = parseResult.GetValue(forceOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var updated = await mediator.Send(
                        new AppPlantCmd.ArchivePlantCommand(
                            Selector: selector,
                            Force: force,
                            DryRun: dryRun
                        ),
                        token
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "archived",
                                dryRun,
                                plantKey = updated.Key,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine(
                            dryRun ? $"Would archive '{updated.Key}'" : $"Archived '{updated.Key}'"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (InvalidOperationException ex)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(
                            code: "invalid_state",
                            message: ex.Message,
                            details: new { selector }
                        );
                    }
                    else
                    {
                        output.WriteErrorLine($"Error: {ex.Message}");
                    }

                    return ExitCodes.InvalidArguments;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return WriteForestNotInitialized(output);
                }
                catch (AppPlantCmd.PlantNotFoundException)
                {
                    return WritePlantNotFound(output, selector);
                }
                catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
                {
                    return WritePlantAmbiguous(
                        output,
                        selector: ex.Selector,
                        matches: ex.Matches,
                        printMatches: false
                    );
                }
            }
        );

        plantCommand.Subcommands.Add(showCommand);
        plantCommand.Subcommands.Add(assignCommand);
        plantCommand.Subcommands.Add(unassignCommand);
        plantCommand.Subcommands.Add(branchesCommand);
        plantCommand.Subcommands.Add(harvestCommand);
        plantCommand.Subcommands.Add(archiveCommand);
        return plantCommand;
    }

    private static int WriteForestNotInitialized(Output output)
    {
        if (output.Json)
        {
            output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
        }
        else
        {
            output.WriteErrorLine("Error: forest not initialized");
        }

        return ExitCodes.ForestNotInitialized;
    }

    private static int WritePlantNotFound(Output output, string selector)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "plant_not_found",
                message: "Plant not found",
                details: new { selector }
            );
        }
        else
        {
            output.WriteErrorLine($"Plant '{selector}': not found");
        }

        return ExitCodes.PlantNotFoundOrAmbiguous;
    }

    private static int WritePlantAmbiguous(
        Output output,
        string selector,
        string[] matches,
        bool printMatches
    )
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "plant_ambiguous",
                message: "Plant selector is ambiguous",
                details: new { selector, matches }
            );
        }
        else
        {
            output.WriteErrorLine($"Plant '{selector}': ambiguous; matched {matches.Length} plants");
            if (printMatches)
            {
                foreach (var key in matches.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    output.WriteErrorLine($"- {key}");
                }
            }
        }

        return ExitCodes.PlantNotFoundOrAmbiguous;
    }
}
