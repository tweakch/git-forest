using System.CommandLine;
using GitForest.Cli;
using GitForest.Application.Features.Plants;
using MediatR;

namespace GitForest.Cli.Commands;

public static class PlantsCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plantsCommand = new Command("plants", "Manage plants");

        var listCommand = new Command("list", "List plants");
        var statusFilterOption = new Option<string?>("--status")
        {
            Description = "Filter by status (planned|planted|growing|harvestable|harvested|archived)"
        };
        var planFilterOption = new Option<string?>("--plan")
        {
            Description = "Filter by plan ID"
        };
        listCommand.Options.Add(statusFilterOption);
        listCommand.Options.Add(planFilterOption);

        listCommand.SetAction(async (parseResult, token) =>
        {
            var output = parseResult.GetOutput(cliOptions);
            var status = parseResult.GetValue(statusFilterOption);
            var plan = parseResult.GetValue(planFilterOption);

            try
            {
                var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                if (!ForestStore.IsInitialized(forestDir))
                {
                    throw new ForestStore.ForestNotInitializedException(forestDir);
                }

                var plants = await mediator.Send(new ListPlantsQuery(Status: status, PlanId: plan), token);

                if (output.Json)
                {
                    output.WriteJson(new
                    {
                        plants = plants.Select(p => new
                        {
                            key = p.Key,
                            status = p.Status,
                            title = p.Title,
                            planId = p.PlanId,
                            plannerId = string.IsNullOrWhiteSpace(p.PlannerId) ? null : p.PlannerId,
                            planters = (p.AssignedPlanters ?? new List<string>()).ToArray()
                        }).ToArray()
                    });
                }
                else
                {
                    output.WriteLine("Key                             Status   Title                         Plan   Planter");
                    if (plants.Count == 0)
                    {
                        output.WriteLine("No plants found");
                    }
                    else
                    {
                        foreach (var p in plants)
                        {
                            var planter = p.AssignedPlanters.Count > 0 ? p.AssignedPlanters[0] : "-";
                            output.WriteLine($"{PadRight(p.Key, 31)} {PadRight(p.Status, 8)} {PadRight(Truncate(p.Title, 28), 28)} {PadRight(p.PlanId, 6)} {planter}");
                        }
                    }
                }

                return ExitCodes.Success;
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

                return ExitCodes.ForestNotInitialized;
            }
        });

        plantsCommand.Subcommands.Add(listCommand);
        return plantsCommand;
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


