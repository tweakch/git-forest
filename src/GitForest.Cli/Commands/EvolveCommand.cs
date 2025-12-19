using System.CommandLine;
using GitForest.Mediator;
using AppPlans = GitForest.Application.Features.Plans;
using AppPlantCmd = GitForest.Application.Features.Plants.Commands;
using CliEvolve = GitForest.Cli.Features.Evolve;

namespace GitForest.Cli.Commands;

public static class EvolveCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var command = new Command("evolve", "Evolve plants (create candidate branches)");

        var planOption = new Option<string?>("--plan")
        {
            Description = "Plan identifier to scope evolution",
        };
        var plantOption = new Option<string?>("--plant")
        {
            Description = "Plant selector/key to scope evolution to a single plant",
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
        command.Options.Add(plantOption);
        command.Options.Add(allOption);
        command.Options.Add(dryRunOption);

        command.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var planId = parseResult.GetValue(planOption);
                var plantSelector = parseResult.GetValue(plantOption);
                var all = parseResult.GetValue(allOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                if (!all && string.IsNullOrWhiteSpace(planId) && string.IsNullOrWhiteSpace(plantSelector))
                {
                    // Default for automation: evolve the whole forest.
                    all = true;
                }

                var specified = (all ? 1 : 0)
                    + (!string.IsNullOrWhiteSpace(planId) ? 1 : 0)
                    + (!string.IsNullOrWhiteSpace(plantSelector) ? 1 : 0);
                if (specified != 1)
                {
                    return WriteInvalidArguments(
                        output,
                        "Specify exactly one of: --all, --plan, or --plant",
                        new { all, planId, plant = plantSelector }
                    );
                }

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var result = await mediator.Send(
                        new CliEvolve.EvolveForestCommand(
                            PlanId: all ? null : planId,
                            PlantSelector: plantSelector,
                            DryRun: dryRun
                        ),
                        token
                    );

                    if (output.Json)
                    {
                        var scope = result.PlantKey is not null
                            ? "plant"
                            : result.PlanId is not null
                                ? "plan"
                                : "all";
                        output.WriteJson(
                            new
                            {
                                status = "evolved",
                                dryRun,
                                scope,
                                planId = result.PlanId,
                                plantKey = result.PlantKey,
                                plantsConsidered = result.PlantsConsidered,
                                plantsEvolved = result.PlantsEvolved,
                                skipped = result.Skipped,
                            }
                        );
                    }
                    else
                    {
                        var scope = result.PlantKey is not null
                            ? $"plant '{result.PlantKey}'"
                            : string.IsNullOrWhiteSpace(result.PlanId)
                                ? "forest"
                                : $"plan '{result.PlanId}'";
                        output.WriteLine(
                            dryRun
                                ? $"Would evolve {scope}: {result.PlantsEvolved} evolved, {result.Skipped} skipped"
                                : $"Evolved {scope}: {result.PlantsEvolved} evolved, {result.Skipped} skipped"
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
                catch (AppPlantCmd.PlantNotFoundException)
                {
                    return WritePlantNotFound(output, plantSelector ?? string.Empty);
                }
                catch (AppPlantCmd.PlantAmbiguousSelectorException ex)
                {
                    return WritePlantAmbiguous(output, selector: ex.Selector, matches: ex.Matches);
                }
                catch (ArgumentException ex)
                {
                    return WriteInvalidArguments(
                        output,
                        ex.Message,
                        new { all, planId, plant = plantSelector }
                    );
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
