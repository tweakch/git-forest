using System.CommandLine;
using System.CommandLine.Invocation;
using GitForest.Cli.Features.Planters;
using MediatR;

namespace GitForest.Cli.Commands;

public static class PlanterCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var planterCommand = new Command("planter", "Manage a specific planter");
        var planterIdArg = new Argument<string>("planter-id", "Planter identifier");
        planterCommand.AddArgument(planterIdArg);

        var showCommand = new Command("show", "Show planter details");
        showCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var planterId = context.ParseResult.GetValueForArgument(planterIdArg);

            try
            {
                var info = await mediator.Send(new GetPlanterQuery(PlanterId: planterId));
                if (!info.Exists)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(code: "planter_not_found", message: "Planter not found", details: new { planterId });
                    }
                    else
                    {
                        output.WriteErrorLine($"Planter '{planterId}': not found");
                    }

                    context.ExitCode = ExitCodes.PlanterNotFound;
                    return;
                }

                if (output.Json)
                {
                    output.WriteJson(new { planter = new { id = info.Id, kind = info.Kind, plans = info.Plans } });
                }
                else
                {
                    var plansText = info.Plans.Length == 0 ? "-" : string.Join(", ", info.Plans);
                    output.WriteLine($"Id: {info.Id}");
                    output.WriteLine($"Kind: {info.Kind}");
                    output.WriteLine($"Plans: {plansText}");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                // For single-planter lookup, treat missing forest as "not found" (keeps CLI usable in fresh repos
                // and matches the smoke-test contract).
                if (output.Json)
                {
                    output.WriteJsonError(code: "planter_not_found", message: "Planter not found", details: new { planterId });
                }
                else
                {
                    output.WriteErrorLine($"Planter '{planterId}': not found");
                }

                context.ExitCode = ExitCodes.PlanterNotFound;
            }
        });

        var plantCommand = new Command("plant", "Assign + create branch for a plant");
        var selectorArg = new Argument<string>("selector", "Plant selector (key, slug, or P01)");
        var branchOption = new Option<string>("--branch", () => "auto", "Branch name to use ('auto' uses a deterministic default)");
        var yesOption = new Option<bool>("--yes", "Proceed without prompting");
        var dryRunOption = new Option<bool>("--dry-run", "Show what would be done without applying");
        plantCommand.AddArgument(selectorArg);
        plantCommand.AddOption(branchOption);
        plantCommand.AddOption(yesOption);
        plantCommand.AddOption(dryRunOption);
        plantCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var planterId = context.ParseResult.GetValueForArgument(planterIdArg);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);
            var branch = context.ParseResult.GetValueForOption(branchOption) ?? "auto";
            var yes = context.ParseResult.GetValueForOption(yesOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            try
            {
                var result = await mediator.Send(new PlantWithPlanterCommand(
                    PlanterId: planterId,
                    Selector: selector,
                    BranchOption: branch,
                    Yes: yes,
                    DryRun: dryRun));

                if (output.Json)
                {
                    output.WriteJson(new { status = "planted", dryRun = result.DryRun, planterId, plantKey = result.PlantKey, branch = result.BranchName });
                }
                else
                {
                    output.WriteLine(dryRun
                        ? $"Would plant '{result.PlantKey}' with planter '{planterId}' on branch '{result.BranchName}'"
                        : $"Planted '{result.PlantKey}' with planter '{planterId}' on branch '{result.BranchName}'");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (PlanterNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "planter_not_found", message: "Planter not found", details: new { planterId });
                }
                else
                {
                    output.WriteErrorLine($"Planter '{planterId}': not found");
                }

                context.ExitCode = ExitCodes.PlanterNotFound;
            }
            catch (ConfirmationRequiredException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "confirmation_required", message: "Use --yes to create/check out a git branch.", details: new { branch = ex.BranchName });
                }
                else
                {
                    output.WriteErrorLine("Error: confirmation required. Re-run with --yes.");
                }

                context.ExitCode = ExitCodes.InvalidArguments;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
                }
                else
                {
                    output.WriteErrorLine("Error: forest not initialized");
                }

                context.ExitCode = ExitCodes.ForestNotInitialized;
            }
            catch (ForestStore.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (ForestStore.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_ambiguous", message: "Plant selector is ambiguous", details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (GitRunner.GitRunnerException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "git_failed", message: ex.Message, details: new { exitCode = ex.ExitCode, stdout = ex.StdOut, stderr = ex.StdErr });
                }
                else
                {
                    output.WriteErrorLine($"Error: {ex.Message}");
                    if (!string.IsNullOrWhiteSpace(ex.StdErr))
                    {
                        output.WriteErrorLine(ex.StdErr.Trim());
                    }
                }

                context.ExitCode = ExitCodes.GitOperationFailed;
            }
        });

        var growCommand = new Command("grow", "Grow a plant (propose or apply changes)");
        var modeOption = new Option<string>("--mode", () => "apply", "Mode: propose|apply");
        growCommand.AddArgument(selectorArg);
        growCommand.AddOption(modeOption);
        growCommand.AddOption(dryRunOption);
        growCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var planterId = context.ParseResult.GetValueForArgument(planterIdArg);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);
            var mode = (context.ParseResult.GetValueForOption(modeOption) ?? "apply").Trim();
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);

            try
            {
                var result = await mediator.Send(new GrowWithPlanterCommand(
                    PlanterId: planterId,
                    Selector: selector,
                    Mode: mode,
                    DryRun: dryRun));

                if (output.Json)
                {
                    output.WriteJson(new { status = "harvestable", dryRun = result.DryRun, mode = result.Mode, planterId, plantKey = result.PlantKey, branch = result.BranchName });
                }
                else
                {
                    output.WriteLine(dryRun
                        ? $"Would grow '{result.PlantKey}' (mode={result.Mode})"
                        : $"Grew '{result.PlantKey}' (mode={result.Mode}) → harvestable");
                }

                context.ExitCode = ExitCodes.Success;
            }
            catch (PlanterNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "planter_not_found", message: "Planter not found", details: new { planterId });
                }
                else
                {
                    output.WriteErrorLine($"Planter '{planterId}': not found");
                }

                context.ExitCode = ExitCodes.PlanterNotFound;
            }
            catch (InvalidModeException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "invalid_arguments", message: ex.Message, details: new { mode = ex.Mode });
                }
                else
                {
                    output.WriteErrorLine("Error: invalid --mode. Expected: propose|apply");
                }

                context.ExitCode = ExitCodes.InvalidArguments;
            }
            catch (ForestStore.ForestNotInitializedException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "forest_not_initialized", message: "Forest not initialized");
                }
                else
                {
                    output.WriteErrorLine("Error: forest not initialized");
                }

                context.ExitCode = ExitCodes.ForestNotInitialized;
            }
            catch (ForestStore.PlantNotFoundException)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': not found");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (ForestStore.PlantAmbiguousSelectorException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "plant_ambiguous", message: "Plant selector is ambiguous", details: new { selector = ex.Selector, matches = ex.Matches });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{ex.Selector}': ambiguous; matched {ex.Matches.Length} plants");
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
            }
            catch (GitRunner.GitRunnerException ex)
            {
                if (output.Json)
                {
                    output.WriteJsonError(code: "git_failed", message: ex.Message, details: new { exitCode = ex.ExitCode, stdout = ex.StdOut, stderr = ex.StdErr });
                }
                else
                {
                    output.WriteErrorLine($"Error: {ex.Message}");
                    if (!string.IsNullOrWhiteSpace(ex.StdErr))
                    {
                        output.WriteErrorLine(ex.StdErr.Trim());
                    }
                }

                context.ExitCode = ExitCodes.GitOperationFailed;
            }
        });

        planterCommand.AddCommand(showCommand);
        planterCommand.AddCommand(plantCommand);
        planterCommand.AddCommand(growCommand);
        return planterCommand;
    }
}
