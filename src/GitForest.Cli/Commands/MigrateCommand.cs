using System.CommandLine;
using GitForest.Cli.Orleans;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plants;
using GitForest.Infrastructure.FileSystem.Repositories;
using GitForest.Infrastructure.Memory;

namespace GitForest.Cli.Commands;

public static class MigrateCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var command = new Command("migrate", "Migrate forest state between persistence providers");

        var fromOption = new Option<string>("--from")
        {
            Description = "Source provider (orleans|file|memory)",
        };
        var toOption = new Option<string>("--to")
        {
            Description = "Destination provider (orleans|file|memory)",
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be migrated without writing",
        };

        command.Options.Add(fromOption);
        command.Options.Add(toOption);
        command.Options.Add(dryRunOption);

        command.SetAction(
            async (parseResult, token) =>
            {
                var output = parseResult.GetOutput(cliOptions);
                var from = NormalizeProvider(parseResult.GetValue(fromOption));
                var to = NormalizeProvider(parseResult.GetValue(toOption));
                var dryRun = parseResult.GetValue(dryRunOption);

                if (from is null || to is null)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(
                            code: "invalid_arguments",
                            message: "Invalid provider; expected orleans|file|memory"
                        );
                    }
                    else
                    {
                        output.WriteErrorLine(
                            "Error: invalid provider; expected orleans|file|memory"
                        );
                    }

                    return ExitCodes.InvalidArguments;
                }

                if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                {
                    if (output.Json)
                    {
                        output.WriteJson(
                            new
                            {
                                status = "no_op",
                                from,
                                to,
                                dryRun,
                                created = 0,
                                updated = 0,
                            }
                        );
                    }
                    else
                    {
                        output.WriteLine("No-op: --from and --to are the same.");
                    }

                    return ExitCodes.Success;
                }

                var forestDir = ForestStore.GetDefaultForestDir();
                ForestStore.EnsureInitialized(forestDir);

                var effective = ForestConfigReader.ReadEffective(forestDir);

                await using var sourceAccessor =
                    from == "orleans" ? new OrleansClientAccessor(effective) : null;
                await using var destAccessor =
                    to == "orleans" ? new OrleansClientAccessor(effective) : null;

                var source = CreatePlantRepository(from, forestDir, sourceAccessor);
                var dest = CreatePlantRepository(to, forestDir, destAccessor);

                var all = await source.ListAsync(new AllPlantsSpec(), token);

                var created = 0;
                var updated = 0;

                foreach (var plant in all)
                {
                    if (plant is null || string.IsNullOrWhiteSpace(plant.Key))
                    {
                        continue;
                    }

                    var key = plant.Key.Trim();
                    var existing = await dest.GetByIdAsync(key, token);
                    if (existing is null)
                    {
                        created++;
                        if (!dryRun)
                        {
                            await dest.AddAsync(Clone(plant), token);
                        }
                    }
                    else
                    {
                        updated++;
                        if (!dryRun)
                        {
                            await dest.UpdateAsync(Clone(plant), token);
                        }
                    }
                }

                if (output.Json)
                {
                    output.WriteJson(
                        new
                        {
                            status = "migrated",
                            from,
                            to,
                            dryRun,
                            plants = new { created, updated },
                        }
                    );
                }
                else
                {
                    output.WriteLine($"Migrated plants: +{created} ~{updated} (dry-run={dryRun})");
                }

                return ExitCodes.Success;
            }
        );

        return command;
    }

    private static string? NormalizeProvider(string? raw)
    {
        var p = (raw ?? string.Empty).Trim().ToLowerInvariant();
        return p is "orleans" or "file" or "memory" ? p : null;
    }

    private static IPlantRepository CreatePlantRepository(
        string provider,
        string forestDir,
        OrleansClientAccessor? orleans
    )
    {
        return provider switch
        {
            "file" => new FileSystemPlantRepository(forestDir),
            "memory" => new InMemoryPlantRepository(),
            "orleans" => orleans is null
                ? throw new InvalidOperationException("Orleans client was not provided.")
                : new ConnectingOrleansPlantRepository(orleans),
            _ => throw new InvalidOperationException($"Unsupported provider: {provider}"),
        };
    }

    private static Plant Clone(Plant source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return new Plant
        {
            Key = source.Key,
            Slug = source.Slug,
            PlanId = source.PlanId,
            PlannerId = source.PlannerId,
            Status = source.Status,
            Title = source.Title,
            Description = source.Description,
            AssignedPlanters = source.AssignedPlanters is null
                ? new List<string>()
                : new List<string>(source.AssignedPlanters),
            Branches = source.Branches is null
                ? new List<string>()
                : new List<string>(source.Branches),
            SelectedBranch = source.SelectedBranch,
            CreatedDate = source.CreatedDate,
            LastActivityDate = source.LastActivityDate,
        };
    }
}
