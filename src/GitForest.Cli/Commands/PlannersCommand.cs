using System.CommandLine;
using GitForest.Application.Features.Planners;
using GitForest.Mediator;
using AppPlanning = GitForest.Application.Features.Planning;
using AppPlans = GitForest.Application.Features.Plans;
using AppReconcile = GitForest.Application.Features.Reconcile;

namespace GitForest.Cli.Commands;

public static class PlannersCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plannersCommand = new Command("planners", "Manage planners");

        var listCommand = new Command("list", "List planners");
        var planFilterOption = new Option<string?>("--plan") { Description = "Filter by plan ID" };
        listCommand.Options.Add(planFilterOption);

        listCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var plan = parseResult.GetValue(planFilterOption);

                try
                {
                    var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                    if (!ForestStore.IsInitialized(forestDir))
                    {
                        throw new ForestStore.ForestNotInitializedException(forestDir);
                    }

                    var rows = (
                        await mediator.Send(new ListPlannersQuery(PlanFilter: plan), token)
                    ).ToArray();

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                planners = rows.Select(r => new { id = r.Id, plans = r.Plans })
                                    .ToArray(),
                            }
                        );
                    }
                    else
                    {
                        if (rows.Length == 0)
                        {
                            output.WriteLine("No planners configured");
                        }
                        else
                        {
                            output.WriteLine($"{PadRight("Id", 30)} {PadRight("Plans", 30)}");
                            foreach (var row in rows)
                            {
                                var plansText =
                                    row.Plans.Length == 0 ? "-" : string.Join(",", row.Plans);
                                output.WriteLine(
                                    $"{PadRight(row.Id, 30)} {PadRight(Truncate(plansText, 30), 30)}"
                                );
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

        plannersCommand.Subcommands.Add(listCommand);

        var planCommand = new Command("plan", "Run planners to refresh desired plants");
        var allOption = new Option<bool>("--all")
        {
            Description = "Plan across all installed plans",
        };
        var planOption = new Option<string?>("--plan")
        {
            Description = "Plan identifier to scope planning",
        };
        var plannerOption = new Option<string?>("--planner")
        {
            Description = "Planner identifier to scope planning",
        };
        var reconcileOption = new Option<bool>("--reconcile")
        {
            Description = "Run reconcile after planning",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };

        planCommand.Options.Add(allOption);
        planCommand.Options.Add(planOption);
        planCommand.Options.Add(plannerOption);
        planCommand.Options.Add(reconcileOption);
        planCommand.Options.Add(dryRunOption);

        planCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var all = parseResult.GetValue(allOption);
                var planId = parseResult.GetValue(planOption);
                var plannerId = parseResult.GetValue(plannerOption);
                var reconcile = parseResult.GetValue(reconcileOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                if (
                    !all
                    && string.IsNullOrWhiteSpace(planId)
                    && string.IsNullOrWhiteSpace(plannerId)
                )
                {
                    return BaseCommand.WriteInvalidArguments(
                        output,
                        "Specify --all, --plan, or --planner",
                        new
                        {
                            all,
                            planId,
                            plannerId,
                        }
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
                        new AppPlanning.PlanForestCommand(
                            PlanId: all ? null : planId,
                            PlannerId: plannerId,
                            DryRun: dryRun
                        ),
                        token
                    );

                    object? reconcileResult = null;
                    if (reconcile)
                    {
                        reconcileResult = await mediator.Send(
                            new AppReconcile.ReconcileForestCommand(
                                PlanId: result.PlanId,
                                DryRun: dryRun
                            ),
                            token
                        );
                    }

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "planned",
                                dryRun,
                                planId = result.PlanId,
                                plannerId = result.PlannerId,
                                plans = result.PlansPlanned,
                                plants = new
                                {
                                    created = result.PlantsCreated,
                                    updated = result.PlantsUpdated,
                                },
                                reconcile = reconcileResult,
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
                                ? $"Would plan {scope}: +{result.PlantsCreated} ~{result.PlantsUpdated}"
                                : $"Planned {scope}: +{result.PlantsCreated} ~{result.PlantsUpdated}"
                        );

                        if (
                            reconcile
                            && reconcileResult is AppReconcile.ReconcileForestResult reconciled
                        )
                        {
                            output.WriteLine(
                                $"Reconciled {scope}: {reconciled.PlantsUpdated} updated, {reconciled.NeedsSelection} need selection"
                            );
                        }
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return BaseCommand.WriteForestNotInitialized(output);
                }
                catch (AppPlans.PlanNotInstalledException)
                {
                    return BaseCommand.WritePlanNotFound(output, planId ?? string.Empty);
                }
            }
        );

        plannersCommand.Subcommands.Add(planCommand);
        return plannersCommand;
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
}
