using System.CommandLine;
using System.CommandLine.Invocation;
using GitForest.Cli;
using GitForest.Application.Features.Planners;
using MediatR;

namespace GitForest.Cli.Commands;

public static class PlannersCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plannersCommand = new Command("planners", "Manage planners");

        var listCommand = new Command("list", "List planners");
        var planFilterOption = new Option<string?>("--plan", "Filter by plan ID");
        listCommand.AddOption(planFilterOption);

        listCommand.SetHandler(async (InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var plan = context.ParseResult.GetValueForOption(planFilterOption);

            try
            {
                var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                if (!ForestStore.IsInitialized(forestDir))
                {
                    throw new ForestStore.ForestNotInitializedException(forestDir);
                }

                var rows = (await mediator.Send(new ListPlannersQuery(PlanFilter: plan))).ToArray();

                if (output.Json)
                {
                    output.WriteJson(new
                    {
                        planners = rows.Select(r => new { id = r.Id, plans = r.Plans }).ToArray()
                    });
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
                            var plansText = row.Plans.Length == 0 ? "-" : string.Join(",", row.Plans);
                            output.WriteLine($"{PadRight(row.Id, 30)} {PadRight(Truncate(plansText, 30), 30)}");
                        }
                    }
                }

                context.ExitCode = ExitCodes.Success;
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
        });

        plannersCommand.AddCommand(listCommand);
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


