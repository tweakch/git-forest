using System.CommandLine;
using GitForest.Application.Features.Planters;
using GitForest.Application.Features.Plants.Commands;
using GitForest.Core.Services;
using GitForest.Mediator;

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
                        return BaseCommand.WritePlanterNotFound(output, planterId);
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
                    return BaseCommand.WritePlanterNotFound(output, planterId);
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
                    return BaseCommand.WritePlanterNotFound(output, planterId);
                }
                catch (ConfirmationRequiredException ex)
                {
                    return BaseCommand.WriteConfirmationRequired(
                        output,
                        message: "Use --yes to create/check out a git branch.",
                        details: new { branch = ex.BranchName }
                    );
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return BaseCommand.WriteForestNotInitialized(output);
                }
                catch (PlantNotFoundException)
                {
                    return BaseCommand.WritePlantNotFound(output, selector);
                }
                catch (PlantAmbiguousSelectorException ex)
                {
                    return BaseCommand.WritePlantAmbiguous(output, ex.Selector, ex.Matches);
                }
                catch (GitServiceException ex)
                {
                    return BaseCommand.WriteGitFailed(output, ex);
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
                    return BaseCommand.WritePlanterNotFound(output, planterId);
                }
                catch (InvalidModeException ex)
                {
                    return BaseCommand.WriteInvalidMode(output, ex.Mode);
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return BaseCommand.WriteForestNotInitialized(output);
                }
                catch (PlantNotFoundException)
                {
                    return BaseCommand.WritePlantNotFound(output, selector);
                }
                catch (PlantAmbiguousSelectorException ex)
                {
                    return BaseCommand.WritePlantAmbiguous(output, ex.Selector, ex.Matches);
                }
                catch (GitServiceException ex)
                {
                    return BaseCommand.WriteGitFailed(output, ex);
                }
            }
        );

        planterCommand.Subcommands.Add(showCommand);
        planterCommand.Subcommands.Add(plantCommand);
        planterCommand.Subcommands.Add(growCommand);
        return planterCommand;
    }
}
