using System.CommandLine;
using GitForest.Application.Features.Plants;
using AppPlantCmd = GitForest.Application.Features.Plants.Commands;
using GitForest.Mediator;

namespace GitForest.Cli.Commands;

public static class PlantsCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plantsCommand = new Command("plants", "Manage plants");

        var listCommand = new Command("list", "List plants");
        var statusFilterOption = new Option<string?>("--status")
        {
            Description =
                "Filter by status (planned|planted|growing|harvestable|harvested|archived)",
        };
        var planFilterOption = new Option<string?>("--plan") { Description = "Filter by plan ID" };
        listCommand.Options.Add(statusFilterOption);
        listCommand.Options.Add(planFilterOption);

        listCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var status = parseResult.GetValue(statusFilterOption);
                var plan = parseResult.GetValue(planFilterOption);

                try
                {
                    var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                    if (!ForestStore.IsInitialized(forestDir))
                    {
                        throw new ForestStore.ForestNotInitializedException(forestDir);
                    }

                    var plants = await mediator.Send(
                        new ListPlantsQuery(Status: status, PlanId: plan),
                        token
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                plants = plants
                                    .Select(p => new
                                    {
                                        key = p.Key,
                                        status = p.Status,
                                        title = p.Title,
                                        planId = p.PlanId,
                                        plannerId = string.IsNullOrWhiteSpace(p.PlannerId)
                                            ? null
                                            : p.PlannerId,
                                        planters = (
                                            p.AssignedPlanters ?? new List<string>()
                                        ).ToArray(),
                                    })
                                    .ToArray(),
                            }
                        );
                    }
                    else
                    {
                        if (plants.Count == 0)
                        {
                            output.WriteLine("No plants found");
                        }
                        else
                        {
                            output.WriteLine("Key Status");
                            foreach (var p in plants)
                            {
                                var planter =
                                    p.AssignedPlanters.Count > 0 ? p.AssignedPlanters[0] : "-";
                                output.WriteLine(
                                    $"{p.Key} {p.Status}"
                                );
                            }
                        }
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(
                            code: "forest_not_initialized",
                            message: "Forest not initialized"
                        );
                    }
                    else
                    {
                        output.WriteErrorLine("Error: forest not initialized");
                    }

                    return ExitCodes.ForestNotInitialized;
                }
            }
        );

        var removeCommand = new Command("remove", "Remove a plant from the forest (deletes metadata)");
        var selectorArg = new Argument<string>("selector")
        {
            Description = "Plant selector (key, slug, or P01)",
        };
        var yesOption = new Option<bool>("--yes") { Description = "Proceed without prompting" };
        var forceOption = new Option<bool>("--force")
        {
            Description = "Remove even if plant is not archived",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };
        removeCommand.Arguments.Add(selectorArg);
        removeCommand.Options.Add(yesOption);
        removeCommand.Options.Add(forceOption);
        removeCommand.Options.Add(dryRunOption);

        removeCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetRequiredValue(selectorArg);
                var yes = parseResult.GetValue(yesOption);
                var force = parseResult.GetValue(forceOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                if (!dryRun && !yes)
                {
                    return WriteConfirmationRequired(output);
                }

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var removed = await mediator.Send(
                        new AppPlantCmd.RemovePlantCommand(
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
                                status = "removed",
                                dryRun,
                                force,
                                plantKey = removed.Key,
                                plantStatus = removed.Status,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine(
                            dryRun
                                ? $"Would remove '{removed.Key}'"
                                : $"Removed '{removed.Key}'"
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
                            details: new { selector, force }
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
                    if (output.Json)
                    {
                        output.WriteJsonError(
                            code: "forest_not_initialized",
                            message: "Forest not initialized"
                        );
                    }
                    else
                    {
                        output.WriteErrorLine("Error: forest not initialized");
                    }

                    return ExitCodes.ForestNotInitialized;
                }
                catch (AppPlantCmd.PlantNotFoundException)
                {
                    return WritePlantNotFound(output, selector);
                }
                catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
                {
                    return WritePlantAmbiguous(output, selector: ex.Selector, matches: ex.Matches);
                }
            }
        );

        plantsCommand.Subcommands.Add(listCommand);
        plantsCommand.Subcommands.Add(removeCommand);
        return plantsCommand;
    }

    private static int WriteConfirmationRequired(Output output)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "confirmation_required",
                message: "Use --yes to remove a plant.",
                details: null
            );
        }
        else
        {
            output.WriteErrorLine("Error: confirmation required. Re-run with --yes.");
        }

        return ExitCodes.InvalidArguments;
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

    private static int WritePlantAmbiguous(Output output, string selector, string[] matches)
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
            output.WriteErrorLine(
                $"Plant '{selector}': ambiguous; matched {matches.Length} plants"
            );
        }

        return ExitCodes.PlantNotFoundOrAmbiguous;
    }

    private static string Truncate(string value, int max)
    {
        value ??= string.Empty;
        return value.Length <= max ? value : value[..Math.Max(0, max - 3)] + "...";
    }
}
