using System.CommandLine;
using System.CommandLine.Invocation;
using GitForest.Cli;

namespace GitForest.Cli.Commands;

public static class PlantersCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var plantersCommand = new Command("planters", "Manage planters");

        var listCommand = new Command("list", "List planters");
        var builtinOption = new Option<bool>("--builtin", "Show only built-in planters");
        var customOption = new Option<bool>("--custom", "Show only custom planters");
        listCommand.AddOption(builtinOption);
        listCommand.AddOption(customOption);

        listCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var builtin = context.ParseResult.GetValueForOption(builtinOption);
            var custom = context.ParseResult.GetValueForOption(customOption);
            var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

            try
            {
                // By default (no flags), show both.
                var includeBuiltin = !builtin && !custom || builtin;
                var includeCustom = !builtin && !custom || custom;

                var rows = new List<PlanterRow>();

                if (includeBuiltin)
                {
                    var plans = ForestStore.ListPlans(forestDir);

                    // Aggregate unique planters across installed plans, also tracking which plan(s) reference each planter.
                    var planters = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var installed in plans)
                    {
                        if (string.IsNullOrWhiteSpace(installed.Id))
                        {
                            continue;
                        }

                        var planId = installed.Id.Trim();
                        var planYamlPath = Path.Combine(forestDir, "plans", planId, "plan.yaml");
                        if (!File.Exists(planYamlPath))
                        {
                            continue;
                        }

                        try
                        {
                            var yaml = File.ReadAllText(planYamlPath);
                            var parsed = PlanYamlLite.Parse(yaml);
                            foreach (var rawPlanterId in parsed.Planters ?? Array.Empty<string>())
                            {
                                if (string.IsNullOrWhiteSpace(rawPlanterId))
                                {
                                    continue;
                                }

                                var planterId = rawPlanterId.Trim();
                                if (!planters.TryGetValue(planterId, out var referencedByPlans))
                                {
                                    referencedByPlans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                    planters[planterId] = referencedByPlans;
                                }

                                referencedByPlans.Add(planId);
                            }
                        }
                        catch
                        {
                            // best-effort: ignore invalid plan YAML
                        }
                    }

                    rows.AddRange(planters
                        .Select(kvp => new PlanterRow(
                            Id: kvp.Key,
                            Kind: "builtin",
                            Plans: kvp.Value.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()))
                        .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase));
                }

                if (includeCustom)
                {
                    // Best-effort: any directory names under .git-forest/planters are treated as custom planters.
                    var plantersDir = Path.Combine(forestDir, "planters");
                    if (Directory.Exists(plantersDir))
                    {
                        foreach (var dir in Directory.GetDirectories(plantersDir))
                        {
                            var id = Path.GetFileName(dir);
                            if (!string.IsNullOrWhiteSpace(id))
                            {
                                rows.Add(new PlanterRow(Id: id.Trim(), Kind: "custom", Plans: Array.Empty<string>()));
                            }
                        }
                    }
                }

                // De-duplicate: if a custom planter shares the same id as a builtin, keep builtin.
                var merged = rows
                    .GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(g =>
                    {
                        var builtinRow = g.FirstOrDefault(x => string.Equals(x.Kind, "builtin", StringComparison.OrdinalIgnoreCase));
                        if (builtinRow is not null)
                        {
                            return builtinRow;
                        }

                        return g.First();
                    })
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (output.Json)
                {
                    output.WriteJson(new
                    {
                        planters = merged.Select(r => new { id = r.Id, kind = r.Kind, plans = r.Plans }).ToArray()
                    });
                }
                else
                {
                    if (merged.Length == 0)
                    {
                        output.WriteLine("No planters configured");
                    }
                    else
                    {
                        output.WriteLine($"{PadRight("Id", 30)} {PadRight("Kind", 8)} {PadRight("Plans", 30)}");
                        foreach (var row in merged)
                        {
                            var plansText = row.Plans.Length == 0 ? "-" : string.Join(",", row.Plans);
                            output.WriteLine($"{PadRight(row.Id, 30)} {PadRight(row.Kind, 8)} {PadRight(Truncate(plansText, 30), 30)}");
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

        plantersCommand.AddCommand(listCommand);
        return plantersCommand;
    }

    private sealed record PlanterRow(string Id, string Kind, string[] Plans);

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


