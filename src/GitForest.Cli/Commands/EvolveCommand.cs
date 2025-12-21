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
                    return BaseCommand.WriteForestNotInitialized(output);
                }
                catch (AppPlans.PlanNotInstalledException)
                {
                    return BaseCommand.WritePlanNotFound(output, planId ?? string.Empty);
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
                catch (ArgumentException ex)
                {
                    return BaseCommand.WriteInvalidArguments(
                        output,
                        ex.Message,
                        new { all, planId, plant = plantSelector }
                    );
                }
            }
        );
        return command;
    }
}
