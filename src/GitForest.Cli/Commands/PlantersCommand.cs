using System.CommandLine;
using CliPlanters = GitForest.Cli.Features.Planters;
using GitForest.Mediator;
using AppPlans = GitForest.Application.Features.Plans;

namespace GitForest.Cli.Commands;

public static class PlantersCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plantersCommand = new Command("planters", "Manage planters");

        var listCommand = new Command("list", "List planters");
        var builtinOption = new Option<bool>("--builtin")
        {
            Description = "Show only built-in planters",
        };
        var customOption = new Option<bool>("--custom")
        {
            Description = "Show only custom planters",
        };
        listCommand.Options.Add(builtinOption);
        listCommand.Options.Add(customOption);

        listCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var builtin = parseResult.GetValue(builtinOption);
                var custom = parseResult.GetValue(customOption);

                try
                {
                    var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                    if (!ForestStore.IsInitialized(forestDir))
                    {
                        throw new ForestStore.ForestNotInitializedException(forestDir);
                    }

                    // By default (no flags), show both.
                    var includeBuiltin = !builtin && !custom || builtin;
                    var includeCustom = !builtin && !custom || custom;
                    var merged = (
                        await mediator.Send(
                            new CliPlanters.ListPlantersQuery(
                                IncludeBuiltin: includeBuiltin,
                                IncludeCustom: includeCustom
                            ),
                            token
                        )
                    ).ToArray();

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                planters = merged
                                    .Select(r => new
                                    {
                                        id = r.Id,
                                        kind = r.Kind,
                                        plans = r.Plans,
                                    })
                                    .ToArray(),
                            }
                        );
                    }
                    else
                    {
                        if (merged.Length == 0)
                        {
                            output.WriteLine("No planters configured");
                        }
                        else
                        {
                            output.WriteLine(
                                $"{PadRight("Id", 30)} {PadRight("Kind", 8)} {PadRight("Plans", 30)}"
                            );
                            foreach (var row in merged)
                            {
                                var plansText =
                                    row.Plans.Length == 0 ? "-" : string.Join(",", row.Plans);
                                output.WriteLine(
                                    $"{PadRight(row.Id, 30)} {PadRight(row.Kind, 8)} {PadRight(Truncate(plansText, 30), 30)}"
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

        plantersCommand.Subcommands.Add(listCommand);

        var plantCommand = new Command("plant", "Assign planters to plants (metadata only)");
        var allOption = new Option<bool>("--all")
        {
            Description = "Assign default planters for all plants",
        };
        var planOption = new Option<string?>("--plan")
        {
            Description = "Plan identifier to scope assignment",
        };
        var singleOption = new Option<bool>("--single")
        {
            Description = "Assign a single planter per plant (deterministic)",
        };
        var resetOption = new Option<bool>("--reset")
        {
            Description = "Overwrite existing assignments",
        };
        var onlyUnassignedOption = new Option<bool>("--only-unassigned")
        {
            Description = "Assign only when a plant has no planters",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };

        plantCommand.Options.Add(allOption);
        plantCommand.Options.Add(planOption);
        plantCommand.Options.Add(singleOption);
        plantCommand.Options.Add(resetOption);
        plantCommand.Options.Add(onlyUnassignedOption);
        plantCommand.Options.Add(dryRunOption);

        plantCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var all = parseResult.GetValue(allOption);
                var planId = parseResult.GetValue(planOption);
                var single = parseResult.GetValue(singleOption);
                var reset = parseResult.GetValue(resetOption);
                var onlyUnassigned = parseResult.GetValue(onlyUnassignedOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                if (!all && string.IsNullOrWhiteSpace(planId))
                {
                    return WriteInvalidArguments(
                        output,
                        "Specify --all or --plan",
                        new { all, planId }
                    );
                }

                if (reset && onlyUnassigned)
                {
                    return WriteInvalidArguments(
                        output,
                        "Cannot combine --reset with --only-unassigned",
                        new { reset, onlyUnassigned }
                    );
                }

                try
                {
                    var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                    if (!ForestStore.IsInitialized(forestDir))
                    {
                        throw new ForestStore.ForestNotInitializedException(forestDir);
                    }

                    var result = await mediator.Send(
                        new CliPlanters.AssignDefaultPlantersCommand(
                            PlanId: all ? null : planId,
                            Single: single,
                            Reset: reset,
                            OnlyUnassigned: onlyUnassigned,
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
                                planId = result.PlanId,
                                plants = new
                                {
                                    considered = result.PlantsConsidered,
                                    updated = result.PlantsUpdated,
                                },
                            }
                        );
                    }
                    else
                    {
                        var scope = string.IsNullOrWhiteSpace(result.PlanId)
                            ? "forest"
                            : $"plan '{result.PlanId}'";
                        output.WriteLine(
                            dryRun
                                ? $"Would assign planters for {scope}: {result.PlantsUpdated} updated"
                                : $"Assigned planters for {scope}: {result.PlantsUpdated} updated"
                        );
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
                catch (AppPlans.PlanNotInstalledException)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(
                            code: "plan_not_found",
                            message: "Plan not found",
                            details: new { planId }
                        );
                    }
                    else
                    {
                        output.WriteErrorLine($"Error: plan not found: {planId}");
                    }

                    return ExitCodes.PlanNotFound;
                }
            }
        );

        plantersCommand.Subcommands.Add(plantCommand);
        return plantersCommand;
    }

    private static string PadRight(string value, int width)
    {
        value ??= string.Empty;
        return value.Length >= width ? value : value.PadRight(width);
    }

    private static string Truncate(string value, int max)
    {
        value ??= string.Empty;
        return value.Length <= max ? value : value[..Math.Max(0, max - 3)] + "...";
    }

    private static int WriteInvalidArguments(Output output, string message, object? details)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "invalid_arguments",
                message: message,
                details: details
            );
        }
        else
        {
            output.WriteErrorLine($"Error: {message}");
        }

        return ExitCodes.InvalidArguments;
    }
}
