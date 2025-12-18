using System.CommandLine;
using GitForest.Application.Features.Planters;
using GitForest.Cli;
using MediatR;

namespace GitForest.Cli.Commands;

public static class PlantersCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var plantersCommand = new Command("planters", "Manage planters");

        var listCommand = new Command("list", "List planters");
        var builtinOption = new Option<bool>("--builtin")
        {
            Description = "Show only built-in planters",
        };
        var customOption = new Option<bool>("--custom")
        {
            Description = "Show only custom planters",
        };
        listCommand.Options.Add(builtinOption);
        listCommand.Options.Add(customOption);

        listCommand.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var builtin = parseResult.GetValue(builtinOption);
                var custom = parseResult.GetValue(customOption);

                try
                {
                    var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                    if (!ForestStore.IsInitialized(forestDir))
                    {
                        throw new ForestStore.ForestNotInitializedException(forestDir);
                    }

                    // By default (no flags), show both.
                    var includeBuiltin = !builtin && !custom || builtin;
                    var includeCustom = !builtin && !custom || custom;
                    var merged = (
                        await mediator.Send(
                            new ListPlantersQuery(
                                IncludeBuiltin: includeBuiltin,
                                IncludeCustom: includeCustom
                            ),
                            token
                        )
                    ).ToArray();

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                planters = merged
                                    .Select(r => new
                                    {
                                        id = r.Id,
                                        kind = r.Kind,
                                        plans = r.Plans,
                                    })
                                    .ToArray(),
                            }
                        );
                    }
                    else
                    {
                        if (merged.Length == 0)
                        {
                            output.WriteLine("No planters configured");
                        }
                        else
                        {
                            output.WriteLine(
                                $"{PadRight("Id", 30)} {PadRight("Kind", 8)} {PadRight("Plans", 30)}"
                            );
                            foreach (var row in merged)
                            {
                                var plansText =
                                    row.Plans.Length == 0 ? "-" : string.Join(",", row.Plans);
                                output.WriteLine(
                                    $"{PadRight(row.Id, 30)} {PadRight(row.Kind, 8)} {PadRight(Truncate(plansText, 30), 30)}"
                                );
                            }
                        }
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
            }
        );

        plantersCommand.Subcommands.Add(listCommand);
        return plantersCommand;
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
