using System.CommandLine;
using GitForest.Cli.Features.Planning;
using GitForest.Mediator;
using AppPlans = GitForest.Application.Features.Plans;

namespace GitForest.Cli.Commands;

public static class EvolveCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var command = new Command("evolve", "Evolve plants (planning-level refresh)");

        var planOption = new Option<string?>("--plan")
        {
            Description = "Plan identifier to scope evolution",
        };
        var allOption = new Option<bool>("--all")
        {
            Description = "Evolve all plans in the forest",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };

        command.Options.Add(planOption);
        command.Options.Add(allOption);
        command.Options.Add(dryRunOption);

        command.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var planId = parseResult.GetValue(planOption);
                var all = parseResult.GetValue(allOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var result = await mediator.Send(
                        new PlanForestCommand(
                            PlanId: all ? null : planId,
                            PlannerId: null,
                            DryRun: dryRun
                        ),
                        token
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "evolved",
                                dryRun,
                                planId = result.PlanId,
                                plans = result.PlansPlanned,
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
                        var scope = string.IsNullOrWhiteSpace(result.PlanId)
                            ? "forest"
                            : $"plan '{result.PlanId}'";
                        output.WriteLine(
                            dryRun
                                ? $"Would evolve {scope}: +{result.PlantsCreated} ~{result.PlantsUpdated}"
                                : $"Evolved {scope}: +{result.PlantsCreated} ~{result.PlantsUpdated}"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return WriteForestNotInitialized(output);
                }
                catch (AppPlans.PlanNotInstalledException)
                {
                    return WritePlanNotFound(output, planId ?? string.Empty);
                }
                catch (ArgumentException ex)
                {
                    return WriteInvalidArguments(output, ex.Message, new { planId });
                }
            }
        );
        return command;
    }

    private static int WriteForestNotInitialized(Output output)
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

    private static int WritePlanNotFound(Output output, string planId)
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
            output.WriteErrorLine($"Plan '{planId}': not found");
        }

        return ExitCodes.PlanNotFound;
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
