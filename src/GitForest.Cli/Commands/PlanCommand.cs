using System.CommandLine;
using GitForest.Cli;
using MediatR;
using AppPlans = GitForest.Application.Features.Plans;

namespace GitForest.Cli.Commands;

public static class PlanCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var planCommand = new Command("plan", "Manage a specific plan");
        var planIdArg = new Argument<string>("plan-id") { Description = "Plan identifier" };
        planCommand.Arguments.Add(planIdArg);

        var reconcileCommand = new Command("reconcile", "Reconcile plan to desired state");
        var updateOption = new Option<bool>("--update")
        {
            Description = "Update plan before reconciling",
        };
        var forumOption = new Option<string?>("--forum")
        {
            Description = "Reconciliation forum to use (ai|file). Overrides config reconcile.forum",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };
        reconcileCommand.Options.Add(updateOption);
        reconcileCommand.Options.Add(forumOption);
        reconcileCommand.Options.Add(dryRunOption);

        reconcileCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var planId = parseResult.GetRequiredValue(planIdArg);
                var update = parseResult.GetValue(updateOption);
                var forum = parseResult.GetValue(forumOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                _ = update; // not implemented yet
                forum = string.IsNullOrWhiteSpace(forum) ? null : forum.Trim();

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var result = await mediator.Send(
                        new AppPlans.ReconcilePlanCommand(
                            PlanId: planId,
                            DryRun: dryRun,
                            Forum: forum
                        ),
                        token
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                planId,
                                status = "reconciled",
                                dryRun,
                                plants = new
                                {
                                    created = result.PlantsCreated,
                                    updated = result.PlantsUpdated,
                                },
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine($"Reconciling plan '{planId}'...");
                        output.WriteLine("Planners: +0 ~0 -0");
                        output.WriteLine("Planters: +0 ~0 -0");
                        output.WriteLine(
                            $"Plants:   +{result.PlantsCreated} ~{result.PlantsUpdated} -0 (archived 0)"
                        );
                        output.WriteLine(dryRun ? "done (dry-run)" : "done");
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

        planCommand.Subcommands.Add(reconcileCommand);
        return planCommand;
    }
}
