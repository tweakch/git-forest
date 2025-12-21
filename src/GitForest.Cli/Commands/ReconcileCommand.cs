using System.CommandLine;
using GitForest.Mediator;
using AppPlantCmd = GitForest.Application.Features.Plants.Commands;
using CliReconcile = GitForest.Cli.Features.Reconcile;

namespace GitForest.Cli.Commands;

public static class ReconcileCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var command = new Command("reconcile", "Reconcile plant outcomes");

        var allOption = new Option<bool>("--all")
        {
            Description = "Reconcile all eligible plants across the forest",
        };
        var planOption = new Option<string?>("--plan")
        {
            Description = "Plan identifier to scope reconciliation",
        };
        var plantOption = new Option<string?>("--plant")
        {
            Description = "Plant selector/key to scope reconciliation to a single plant",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };

        command.Options.Add(allOption);
        command.Options.Add(planOption);
        command.Options.Add(plantOption);
        command.Options.Add(dryRunOption);

        command.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var all = parseResult.GetValue(allOption);
                var planId = parseResult.GetValue(planOption);
                var plantSelector = parseResult.GetValue(plantOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                if (!all && string.IsNullOrWhiteSpace(planId) && string.IsNullOrWhiteSpace(plantSelector))
                {
                    // Default: reconcile the whole forest.
                    all = true;
                }

                var specified = (all ? 1 : 0)
                    + (!string.IsNullOrWhiteSpace(planId) ? 1 : 0)
                    + (!string.IsNullOrWhiteSpace(plantSelector) ? 1 : 0);
                if (specified != 1)
                {
                    return BaseCommand.WriteInvalidArguments(
                        output,
                        "Specify exactly one of: --all, --plan, or --plant",
                        new { all, planId, plant = plantSelector }
                    );
                }

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    if (!string.IsNullOrWhiteSpace(plantSelector))
                    {
                        var plantResult = await mediator.Send(
                            new CliReconcile.ReconcilePlantCommand(
                                Selector: plantSelector!,
                                SelectedBranch: null,
                                Status: null,
                                Prune: false,
                                Force: false,
                                DryRun: dryRun
                            ),
                            token
                        );

                        if (output.Json)
                        {
                            output.WriteJson(
                                new
                                {
                                    status = "reconciled",
                                    dryRun,
                                    plantKey = plantResult.PlantKey,
                                    plantStatus = plantResult.Status,
                                    selectedBranch = plantResult.SelectedBranch,
                                    pruned = plantResult.Pruned,
                                }
                            );
                        }
                        else
                        {
                            output.WriteLine(
                                dryRun
                                    ? $"Would reconcile '{plantResult.PlantKey}' (status={plantResult.Status})"
                                    : $"Reconciled '{plantResult.PlantKey}' (status={plantResult.Status})"
                            );
                        }

                        return ExitCodes.Success;
                    }

                    var result = await mediator.Send(
                        new CliReconcile.ReconcileForestCommand(all ? null : planId, dryRun),
                        token
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "reconciled",
                                dryRun,
                                planId = result.PlanId,
                                updated = result.PlantsUpdated,
                                needsSelection = result.NeedsSelection,
                            }
                        );
                    }
                    else
                    {
                        var scope = string.IsNullOrWhiteSpace(result.PlanId)
                            ? "forest"
                            : $"plan '{result.PlanId}'";
                        output.WriteLine(
                            $"Reconciled {scope}: {result.PlantsUpdated} updated, {result.NeedsSelection} need selection"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return BaseCommand.WriteForestNotInitialized(output);
                }
                catch (AppPlantCmd.PlantNotFoundException)
                {
                    return BaseCommand.WritePlantNotFound(output, plantSelector ?? string.Empty);
                }
                catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
                {
                    return BaseCommand.WritePlantAmbiguous(
                        output,
                        selector: ex.Selector,
                        matches: ex.Matches
                    );
                }
                catch (InvalidOperationException ex)
                {
                    return BaseCommand.WriteInvalidArguments(
                        output,
                        ex.Message,
                        new { all, planId, plant = plantSelector }
                    );
                }
            }
        );

        var plantCommand = new Command("plant", "Reconcile a specific plant");
        var selectorArg = new Argument<string>("selector")
        {
            Description = "Plant selector (key, slug, or P01)",
        };
        plantCommand.Arguments.Add(selectorArg);

        var selectOption = new Option<string?>("--select")
        {
            Description = "Selected branch to keep",
        };
        var statusOption = new Option<string?>("--status")
        {
            Description = "Target status (harvestable|harvested|archived)",
        };
        var pruneOption = new Option<bool>("--prune")
        {
            Description = "Prune non-selected branches when a selection is made",
        };
        var forceOption = new Option<bool>("--force")
        {
            Description = "Force state transition even if current status is unexpected",
        };

        plantCommand.Options.Add(selectOption);
        plantCommand.Options.Add(statusOption);
        plantCommand.Options.Add(pruneOption);
        plantCommand.Options.Add(forceOption);
        plantCommand.Options.Add(dryRunOption);

        plantCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetRequiredValue(selectorArg);
                var selected = parseResult.GetValue(selectOption);
                var status = parseResult.GetValue(statusOption);
                var prune = parseResult.GetValue(pruneOption);
                var force = parseResult.GetValue(forceOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var result = await mediator.Send(
                        new CliReconcile.ReconcilePlantCommand(
                            Selector: selector,
                            SelectedBranch: selected,
                            Status: status,
                            Prune: prune,
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
                                status = "reconciled",
                                dryRun,
                                plantKey = result.PlantKey,
                                plantStatus = result.Status,
                                selectedBranch = result.SelectedBranch,
                                pruned = result.Pruned,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine(
                            dryRun
                                ? $"Would reconcile '{result.PlantKey}' (status={result.Status})"
                                : $"Reconciled '{result.PlantKey}' (status={result.Status})"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return BaseCommand.WriteForestNotInitialized(output);
                }
                catch (AppPlantCmd.PlantNotFoundException)
                {
                    return BaseCommand.WritePlantNotFound(output, selector);
                }
                catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
                {
                    return BaseCommand.WritePlantAmbiguous(
                        output,
                        selector: ex.Selector,
                        matches: ex.Matches
                    );
                }
                catch (InvalidOperationException ex)
                {
                    return BaseCommand.WriteInvalidArguments(output, ex.Message, new { selector });
                }
            }
        );

        command.Subcommands.Add(plantCommand);
        return command;
    }
}
