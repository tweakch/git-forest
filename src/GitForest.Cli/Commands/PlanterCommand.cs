using System.CommandLine;
using GitForest.Cli.Features.Planters;
using MediatR;

namespace GitForest.Cli.Commands;

public static class PlanterCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var planterCommand = new Command("planter", "Manage a specific planter");
        var planterIdArg = new Argument<string>("planter-id")
        {
            Description = "Planter identifier",
        };
        planterCommand.Arguments.Add(planterIdArg);

        var showCommand = new Command("show", "Show planter details");
        showCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var planterId = parseResult.GetRequiredValue(planterIdArg);

                try
                {
                    var info = await mediator.Send(
                        new GetPlanterQuery(PlanterId: planterId),
                        token
                    );
                    if (!info.Exists)
                    {
                        return WritePlanterNotFound(output, planterId);
                    }

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                planter = new
                                {
                                    id = info.Id,
                                    kind = info.Kind,
                                    plans = info.Plans,
                                },
                            }
                        );
                    }
                    else
                    {
                        var plansText =
                            info.Plans.Length == 0 ? "-" : string.Join(", ", info.Plans);
                        output.WriteLine($"Id: {info.Id}");
                        output.WriteLine($"Kind: {info.Kind}");
                        output.WriteLine($"Plans: {plansText}");
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    // For single-planter lookup, treat missing forest as "not found" (keeps CLI usable in fresh repos
                    // and matches the smoke-test contract).
                    return WritePlanterNotFound(output, planterId);
                }
            }
        );

        var plantCommand = new Command("plant", "Assign + create branch for a plant");
        var selectorArg = new Argument<string>("selector")
        {
            Description = "Plant selector (key, slug, or P01)",
        };
        var branchOption = new Option<string>("--branch")
        {
            Description = "Branch name to use ('auto' uses a deterministic default)",
            DefaultValueFactory = _ => "auto",
        };
        var yesOption = new Option<bool>("--yes") { Description = "Proceed without prompting" };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying",
        };
        plantCommand.Arguments.Add(selectorArg);
        plantCommand.Options.Add(branchOption);
        plantCommand.Options.Add(yesOption);
        plantCommand.Options.Add(dryRunOption);
        plantCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var planterId = parseResult.GetRequiredValue(planterIdArg);
                var selector = parseResult.GetRequiredValue(selectorArg);
                var branch = (parseResult.GetValue(branchOption) ?? "auto").Trim();
                var yes = parseResult.GetValue(yesOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                try
                {
                    var result = await mediator.Send(
                        new PlantWithPlanterCommand(
                            PlanterId: planterId,
                            Selector: selector,
                            BranchOption: branch,
                            Yes: yes,
                            DryRun: dryRun
                        )
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "planted",
                                dryRun = result.DryRun,
                                planterId,
                                plantKey = result.PlantKey,
                                branch = result.BranchName,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine(
                            dryRun
                                ? $"Would plant '{result.PlantKey}' with planter '{planterId}' on branch '{result.BranchName}'"
                                : $"Planted '{result.PlantKey}' with planter '{planterId}' on branch '{result.BranchName}'"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (PlanterNotFoundException)
                {
                    return WritePlanterNotFound(output, planterId);
                }
                catch (ConfirmationRequiredException ex)
                {
                    return WriteConfirmationRequired(output, ex.BranchName);
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
                    return WritePlantAmbiguous(output, ex.Selector, ex.Matches);
                }
                catch (GitRunner.GitRunnerException ex)
                {
                    return WriteGitFailed(output, ex);
                }
            }
        );

        var growCommand = new Command("grow", "Grow a plant (propose or apply changes)");
        var modeOption = new Option<string>("--mode")
        {
            Description = "Mode: propose|apply",
            DefaultValueFactory = _ => "apply",
        };
        growCommand.Arguments.Add(selectorArg);
        growCommand.Options.Add(modeOption);
        growCommand.Options.Add(dryRunOption);
        growCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var planterId = parseResult.GetRequiredValue(planterIdArg);
                var selector = parseResult.GetRequiredValue(selectorArg);
                var mode = (parseResult.GetValue(modeOption) ?? "apply").Trim();
                var dryRun = parseResult.GetValue(dryRunOption);

                try
                {
                    var result = await mediator.Send(
                        new GrowWithPlanterCommand(
                            PlanterId: planterId,
                            Selector: selector,
                            Mode: mode,
                            DryRun: dryRun
                        )
                    );

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "harvestable",
                                dryRun = result.DryRun,
                                mode = result.Mode,
                                planterId,
                                plantKey = result.PlantKey,
                                branch = result.BranchName,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine(
                            dryRun
                                ? $"Would grow '{result.PlantKey}' (mode={result.Mode})"
                                : $"Grew '{result.PlantKey}' (mode={result.Mode}) → harvestable"
                        );
                    }

                    return ExitCodes.Success;
                }
                catch (PlanterNotFoundException)
                {
                    return WritePlanterNotFound(output, planterId);
                }
                catch (InvalidModeException ex)
                {
                    return WriteInvalidMode(output, ex.Mode);
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
                    return WritePlantAmbiguous(output, ex.Selector, ex.Matches);
                }
                catch (GitRunner.GitRunnerException ex)
                {
                    return WriteGitFailed(output, ex);
                }
            }
        );

        planterCommand.Subcommands.Add(showCommand);
        planterCommand.Subcommands.Add(plantCommand);
        planterCommand.Subcommands.Add(growCommand);
        return planterCommand;
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

    private static int WritePlanterNotFound(Output output, string planterId)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "planter_not_found",
                message: "Planter not found",
                details: new { planterId }
            );
        }
        else
        {
            output.WriteErrorLine($"Planter '{planterId}': not found");
        }

        return ExitCodes.PlanterNotFound;
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

    private static int WriteGitFailed(Output output, GitRunner.GitRunnerException ex)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "git_failed",
                message: ex.Message,
                details: new
                {
                    exitCode = ex.ExitCode,
                    stdout = ex.StdOut,
                    stderr = ex.StdErr,
                }
            );
        }
        else
        {
            output.WriteErrorLine($"Error: {ex.Message}");
            if (!string.IsNullOrWhiteSpace(ex.StdErr))
            {
                output.WriteErrorLine(ex.StdErr.Trim());
            }
        }

        return ExitCodes.GitOperationFailed;
    }

    private static int WriteConfirmationRequired(Output output, string branchName)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "confirmation_required",
                message: "Use --yes to create/check out a git branch.",
                details: new { branch = branchName }
            );
        }
        else
        {
            output.WriteErrorLine("Error: confirmation required. Re-run with --yes.");
        }

        return ExitCodes.InvalidArguments;
    }

    private static int WriteInvalidMode(Output output, string mode)
    {
        if (output.Json)
        {
            output.WriteJsonError(
                code: "invalid_arguments",
                message: "Invalid --mode. Expected: propose|apply",
                details: new { mode }
            );
        }
        else
        {
            output.WriteErrorLine("Error: invalid --mode. Expected: propose|apply");
        }

        return ExitCodes.InvalidArguments;
    }
}
