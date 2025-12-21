using System.CommandLine;
using GitForest.Application.Features.Connection;
using GitForest.Mediator;
using AppForest = GitForest.Application.Features.Forest;

namespace GitForest.Cli.Commands;

public static class StatusCommand
{
    public static Command Build(CliOptions cliOptions, IMediator mediator)
    {
        var command = new Command("status", "Show forest status");

        command.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);

                try
                {
                    var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
                    if (!ForestStore.IsInitialized(forestDir))
                    {
                        throw new ForestStore.ForestNotInitializedException(forestDir);
                    }

                    var connection = await mediator.Send(
                        new GetForestConnectionStatusQuery(),
                        token
                    );

                    if (connection.Type == "orleans" && !connection.Available)
                    {
                        var lite = await mediator.Send(
                            new AppForest.GetForestStatusLiteQuery(),
                            token
                        );

                        if (output.Json)
                        {
                            output.WriteJson(
                                new
                                {
                                    forest = "initialized",
                                    repo = "origin/main",
                                    connection = new
                                    {
                                        type = connection.Type,
                                        available = connection.Available,
                                        details = connection.Details,
                                        error = connection.Error,
                                    },
                                    plans = lite.PlansCount,
                                    plants = (int?)null,
                                    planters = lite.PlantersAvailable.Length,
                                    planners = lite.PlannersAvailable.Length,
                                    @lock = lite.LockStatus,
                                    plantsByStatus = (object?)null,
                                    plantersAvailable = lite.PlantersAvailable,
                                    plantersActive = Array.Empty<string>(),
                                    plannersAvailable = lite.PlannersAvailable,
                                    plannersActive = Array.Empty<string>(),
                                }
                            );
                        }
                        else
                        {
                            output.WriteLine("Forest: initialized  Repo: origin/main");
                            output.WriteLine(
                                $"Connection: {connection.Type} ({connection.Details}) unavailable"
                            );
                            output.WriteLine($"Plans: {lite.PlansCount} installed");
                            output.WriteLine(
                                "Plants: unavailable (backend not reachable; run `aspire run`)"
                            );
                            output.WriteLine(
                                $"Planters: {lite.PlantersAvailable.Length} available | (active unknown)"
                            );
                            output.WriteLine(
                                $"Planners: {lite.PlannersAvailable.Length} available | (active unknown)"
                            );
                            output.WriteLine($"Lock: {lite.LockStatus}");
                        }

                        return ExitCodes.OrleansNotAvailable;
                    }

                    var status = await mediator.Send(new AppForest.GetForestStatusQuery(), token);

                    var planned = GetCount(status.PlantsByStatus, "planned");
                    var planted = GetCount(status.PlantsByStatus, "planted");
                    var growing = GetCount(status.PlantsByStatus, "growing");
                    var harvestable = GetCount(status.PlantsByStatus, "harvestable");
                    var harvested = GetCount(status.PlantsByStatus, "harvested");
                    var archived = GetCount(status.PlantsByStatus, "archived");

                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                forest = "initialized",
                                repo = "origin/main",
                                connection = new
                                {
                                    type = connection.Type,
                                    available = connection.Available,
                                    details = connection.Details,
                                    error = connection.Error,
                                },
                                plans = status.PlansCount,
                                plants = status.PlantsCount,
                                planters = status.PlantersAvailable.Length,
                                planners = status.PlannersAvailable.Length,
                                @lock = status.LockStatus,
                                plantsByStatus = new
                                {
                                    planned,
                                    planted,
                                    growing,
                                    harvestable,
                                    harvested,
                                    archived,
                                },
                                plantersAvailable = status.PlantersAvailable,
                                plantersActive = status.PlantersActive,
                                plannersAvailable = status.PlannersAvailable,
                                plannersActive = status.PlannersActive,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine("Forest: initialized  Repo: origin/main");
                        if (connection.Type == "orleans")
                        {
                            output.WriteLine(
                                $"Connection: {connection.Type} ({connection.Details})"
                            );
                        }
                        else
                        {
                            output.WriteLine($"Connection: {connection.Type}");
                        }
                        output.WriteLine($"Plans: {status.PlansCount} installed");
                        output.WriteLine(
                            $"Plants: planned {planned} | planted {planted} | growing {growing} | harvestable {harvestable} | harvested {harvested} | archived {archived}"
                        );
                        output.WriteLine(
                            $"Planters: {status.PlantersAvailable.Length} available | {status.PlantersActive.Length} active"
                        );
                        output.WriteLine(
                            $"Planners: {status.PlannersAvailable.Length} available | {status.PlannersActive.Length} active"
                        );
                        output.WriteLine($"Lock: {status.LockStatus}");
                    }

                    return ExitCodes.Success;
                }
                catch (ForestStore.ForestNotInitializedException)
                {
                    return BaseCommand.WriteForestNotInitialized(output);
                }
            }
        );

        return command;
    }

    private static int GetCount(IReadOnlyDictionary<string, int> counts, string status)
    {
        if (counts.TryGetValue(status, out var value))
        {
            return value;
        }

        return 0;
    }
}
