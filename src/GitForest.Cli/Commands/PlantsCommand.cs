using System.CommandLine;
using GitForest.Application.Features.Plants;
using GitForest.Mediator;
using AppPlantCmd = GitForest.Application.Features.Plants.Commands;

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
                            output.WriteLine("Key Status Planter");
                            foreach (var p in plants)
                            {
                                var planter =
                                    p.AssignedPlanters.Count > 0 ? p.AssignedPlanters[0] : "-";
                                output.WriteLine($"{p.Key} {p.Status} {planter}");
                            }
                        }
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return BaseCommand.WriteForestNotInitialized(output);
                }
            }
        );

        var removeCommand = new Command(
            "remove",
            "Remove plants from the forest (deletes metadata)"
        );
        var selectorArg = new Argument<string?>("selector")
        {
            Description = "Plant selector (key, slug, or P01)",
            Arity = ArgumentArity.ZeroOrOne,
        };
        var planOption = new Option<string?>("--plan")
        {
            Description = "Remove all plants for a plan ID",
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
        removeCommand.Options.Add(planOption);
        removeCommand.Options.Add(yesOption);
        removeCommand.Options.Add(forceOption);
        removeCommand.Options.Add(dryRunOption);

        removeCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetValue(selectorArg);
                var planId = parseResult.GetValue(planOption);
                var yes = parseResult.GetValue(yesOption);
                var force = parseResult.GetValue(forceOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                var hasSelector = !string.IsNullOrWhiteSpace(selector);
                var hasPlan = !string.IsNullOrWhiteSpace(planId);
                if (hasSelector && hasPlan)
                {
                    return BaseCommand.WriteInvalidArguments(
                        output,
                        "Use either <selector> or --plan (not both).",
                        new { selector, planId }
                    );
                }

                if (!hasSelector && !hasPlan)
                {
                    return BaseCommand.WriteInvalidArguments(
                        output,
                        "Provide either <selector> or --plan.",
                        details: null
                    );
                }

                if (!dryRun && !yes)
                {
                    return BaseCommand.WriteConfirmationRequired(
                        output,
                        message: "Use --yes to remove a plant.",
                        details: null
                    );
                }

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    if (hasPlan)
                    {
                        var removed = await mediator.Send(
                            new AppPlantCmd.RemovePlantsByPlanCommand(
                                PlanId: planId!.Trim(),
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
                                    planId = removed.PlanId,
                                    plantsRemovedCount = removed.PlantKeys.Length,
                                    plantKeys = removed.PlantKeys,
                                }
                            );
                        }
                        else
                        {
                            if (removed.PlantKeys.Length == 0)
                            {
                                output.WriteLine($"No plants found for plan '{removed.PlanId}'");
                            }
                            else
                            {
                                output.WriteLine(
                                    dryRun
                                        ? $"Would remove {removed.PlantKeys.Length} plants for plan '{removed.PlanId}'"
                                        : $"Removed {removed.PlantKeys.Length} plants for plan '{removed.PlanId}'"
                                );
                            }
                        }
                    }
                    else
                    {
                        var removed = await mediator.Send(
                            new AppPlantCmd.RemovePlantCommand(
                                Selector: selector!.Trim(),
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
                    }

                    return ExitCodes.Success;
                }
                catch (InvalidOperationException ex)
                {
                    return BaseCommand.WriteInvalidState(
                        output,
                        message: ex.Message,
                        details: new { selector, force }
                    );
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return BaseCommand.WriteForestNotInitialized(output);
                }
                catch (AppPlantCmd.PlantNotFoundException)
                {
                    return BaseCommand.WritePlantNotFound(output, selector ?? string.Empty);
                }
                catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
                {
                    return BaseCommand.WritePlantAmbiguous(
                        output,
                        selector: ex.Selector,
                        matches: ex.Matches
                    );
                }
            }
        );

        plantsCommand.Subcommands.Add(listCommand);
        plantsCommand.Subcommands.Add(removeCommand);
        return plantsCommand;
    }

    private static string Truncate(string value, int max)
    {
        value ??= string.Empty;
        return value.Length <= max ? value : value[..Math.Max(0, max - 3)] + "...";
    }
}
