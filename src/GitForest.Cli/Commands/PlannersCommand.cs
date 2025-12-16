using System.CommandLine;
using System.CommandLine.Invocation;
using GitForest.Cli;

namespace GitForest.Cli.Commands;

public static class PlannersCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var plannersCommand = new Command("planners", "Manage planners");

        var listCommand = new Command("list", "List planners");
        var planFilterOption = new Option<string?>("--plan", "Filter by plan ID");
        listCommand.AddOption(planFilterOption);

        listCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var plan = context.ParseResult.GetValueForOption(planFilterOption);
            var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

            try
            {
                var plans = ForestStore.ListPlans(forestDir);
                if (!string.IsNullOrWhiteSpace(plan))
                {
                    var planId = plan.Trim();
                    plans = plans.Where(p => string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase)).ToArray();
                }

                // Aggregate unique planners across installed plans, also tracking which plan(s) reference each planner.
                var planners = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var installed in plans)
                {
                    if (string.IsNullOrWhiteSpace(installed.Id))
                    {
                        continue;
                    }

                    var planYamlPath = Path.Combine(forestDir, "plans", installed.Id.Trim(), "plan.yaml");
                    if (!File.Exists(planYamlPath))
                    {
                        continue;
                    }

                    try
                    {
                        var yaml = File.ReadAllText(planYamlPath);
                        var parsed = PlanYamlLite.Parse(yaml);
                        foreach (var rawPlannerId in parsed.Planners ?? Array.Empty<string>())
                        {
                            if (string.IsNullOrWhiteSpace(rawPlannerId))
                            {
                                continue;
                            }

                            var plannerId = rawPlannerId.Trim();
                            if (!planners.TryGetValue(plannerId, out var referencedByPlans))
                            {
                                referencedByPlans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                planners[plannerId] = referencedByPlans;
                            }

                            referencedByPlans.Add(installed.Id.Trim());
                        }
                    }
                    catch
                    {
                        // best-effort: ignore invalid plan YAML
                    }
                }

                var rows = planners
                    .Select(kvp => new PlannerRow(
                        Id: kvp.Key,
                        Plans: kvp.Value.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()))
                    .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

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

    private sealed record PlannerRow(string Id, string[] Plans);

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


