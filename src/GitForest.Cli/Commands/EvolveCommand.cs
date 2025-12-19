using System.CommandLine;
using GitForest.Mediator;
using AppPlantCmd = GitForest.Application.Features.Plants.Commands;
using CliEvolve = GitForest.Cli.Features.Evolve;

namespace GitForest.Cli.Commands;

public static class EvolveCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var command = new Command("evolve", "Evolve plants (branches + growth)");

        var planOption = new Option<string?>("--plan")
        {
            Description = "Plan identifier to scope evolution",
        };
        var planterOption = new Option<string>("--planter")
        {
            Description = "Planter identifier to evolve with",
            Required = true,
        };
        var branchOption = new Option<string>("--branch")
        {
            Description = "Branch name (or 'auto' for default naming)",
        };
        var modeOption = new Option<string>("--mode")
        {
            Description = "Evolution mode (propose|apply)",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };

        command.Options.Add(planOption);
        command.Options.Add(planterOption);
        command.Options.Add(branchOption);
        command.Options.Add(modeOption);
        command.Options.Add(dryRunOption);

        command.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var planId = parseResult.GetValue(planOption);
                var planterId = parseResult.GetRequiredValue(planterOption);
                var branch = parseResult.GetValue(branchOption);
                var mode = parseResult.GetValue(modeOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var result = await mediator.Send(
                        new CliEvolve.EvolveForestCommand(
                            PlanId: planId,
                            PlanterId: planterId,
                            BranchOption: branch ?? "auto",
                            Mode: string.IsNullOrWhiteSpace(mode) ? "propose" : mode.Trim(),
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
                                planterId = result.PlanterId,
                                branch = branch ?? "auto",
                                mode = result.Mode,
                                evolved = result.PlantsEvolved,
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
                                ? $"Would evolve {scope}: {result.PlantsEvolved} plants"
                                : $"Evolved {scope}: {result.PlantsEvolved} plants"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return WriteForestNotInitialized(output);
                }
                catch (CliEvolve.InvalidModeException ex)
                {
                    return WriteInvalidArguments(
                        output,
                        message: ex.Message,
                        details: new { mode = ex.Mode }
                    );
                }
                catch (ArgumentException ex)
                {
                    return WriteInvalidArguments(output, ex.Message, new { planterId });
                }
            }
        );

        var plantCommand = new Command("plant", "Evolve a specific plant");
        var selectorArg = new Argument<string>("selector")
        {
            Description = "Plant selector (key, slug, or P01)",
        };
        plantCommand.Arguments.Add(selectorArg);

        var plantPlanterOption = new Option<string>("--planter")
        {
            Description = "Planter identifier to evolve with",
            Required = true,
        };
        var plantBranchOption = new Option<string>("--branch")
        {
            Description = "Branch name (or 'auto' for default naming)",
        };
        var plantModeOption = new Option<string>("--mode")
        {
            Description = "Evolution mode (propose|apply)",
        };
        var plantDryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };

        plantCommand.Options.Add(plantPlanterOption);
        plantCommand.Options.Add(plantBranchOption);
        plantCommand.Options.Add(plantModeOption);
        plantCommand.Options.Add(plantDryRunOption);

        plantCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var selector = parseResult.GetRequiredValue(selectorArg);
                var planterId = parseResult.GetRequiredValue(plantPlanterOption);
                var branch = parseResult.GetValue(plantBranchOption);
                var mode = parseResult.GetValue(plantModeOption);
                var dryRun = parseResult.GetValue(plantDryRunOption);

                try
                {
                    var forestDir = ForestStore.GetDefaultForestDir();
                    ForestStore.EnsureInitialized(forestDir);

                    var result = await mediator.Send(
                        new CliEvolve.EvolvePlantCommand(
                            Selector: selector,
                            PlanterId: planterId,
                            BranchOption: branch ?? "auto",
                            Mode: string.IsNullOrWhiteSpace(mode) ? "propose" : mode.Trim(),
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
                                plantKey = result.PlantKey,
                                branch = result.BranchName,
                                mode = result.Mode,
                                plantStatus = result.Status,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine(
                            dryRun
                                ? $"Would evolve '{result.PlantKey}' on branch '{result.BranchName}' ({result.Mode})"
                                : $"Evolved '{result.PlantKey}' on branch '{result.BranchName}' ({result.Mode})"
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
                    return WritePlantAmbiguous(output, selector: ex.Selector, matches: ex.Matches);
                }
                catch (CliEvolve.InvalidModeException ex)
                {
                    return WriteInvalidArguments(
                        output,
                        message: ex.Message,
                        details: new { mode = ex.Mode }
                    );
                }
                catch (ArgumentException ex)
                {
                    return WriteInvalidArguments(output, ex.Message, new { selector });
                }
            }
        );

        command.Subcommands.Add(plantCommand);
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
