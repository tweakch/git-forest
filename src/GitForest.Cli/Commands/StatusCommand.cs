using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class StatusCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var command = new Command("status", "Show forest status");

        command.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);

            try
            {
                var plans = ForestStore.ListPlans(forestDir);
                var plants = ForestStore.ListPlants(forestDir, statusFilter: null, planFilter: null);

                var plantsByStatus = plants
                    .GroupBy(p => (p.Status ?? string.Empty).Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                var planned = GetCount(plantsByStatus, "planned");
                var planted = GetCount(plantsByStatus, "planted");
                var growing = GetCount(plantsByStatus, "growing");
                var harvestable = GetCount(plantsByStatus, "harvestable");
                var harvested = GetCount(plantsByStatus, "harvested");
                var archived = GetCount(plantsByStatus, "archived");

                var (plantersAvailable, plannersAvailable) = LoadAvailablePlantersAndPlanners(forestDir, plans.Select(p => p.Id));

                var nonArchivedPlants = plants.Where(p => !string.Equals(p.Status, "archived", StringComparison.OrdinalIgnoreCase)).ToArray();

                var plantersActive = nonArchivedPlants
                    .SelectMany(p => p.AssignedPlanters ?? Array.Empty<string>())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var plannersActive = nonArchivedPlants
                    .Select(p => p.PlannerId)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var lockPath = Path.Combine(forestDir, "lock");
                var lockStatus = "free";
                try
                {
                    if (File.Exists(lockPath))
                    {
                        var lockText = File.ReadAllText(lockPath).Trim();
                        if (!string.IsNullOrWhiteSpace(lockText))
                        {
                            lockStatus = "held";
                        }
                    }
                }
                catch
                {
                    // best-effort lock status
                }

                if (output.Json)
                {
                    output.WriteJson(new
                    {
                        forest = "initialized",
                        repo = "origin/main",
                        plans = plans.Count,
                        plants = plants.Count,
                        planters = plantersAvailable.Length,
                        planners = plannersAvailable.Length,
                        @lock = lockStatus,
                        plantsByStatus = new
                        {
                            planned,
                            planted,
                            growing,
                            harvestable,
                            harvested,
                            archived
                        },
                        plantersAvailable,
                        plantersActive,
                        plannersAvailable,
                        plannersActive
                    });
                }
                else
                {
                    output.WriteLine("Forest: initialized  Repo: origin/main");
                    output.WriteLine($"Plans: {plans.Count} installed");
                    output.WriteLine($"Plants: planned {planned} | planted {planted} | growing {growing} | harvestable {harvestable} | harvested {harvested} | archived {archived}");
                    output.WriteLine($"Planters: {plantersAvailable.Length} available | {plantersActive.Length} active");
                    output.WriteLine($"Planners: {plannersAvailable.Length} available | {plannersActive.Length} active");
                    output.WriteLine($"Lock: {lockStatus}");
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

    private static (string[] plantersAvailable, string[] plannersAvailable) LoadAvailablePlantersAndPlanners(string forestDir, IEnumerable<string> planIds)
    {
        var planters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var planners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var planId in planIds)
        {
            if (string.IsNullOrWhiteSpace(planId))
            {
                continue;
            }

            var planYamlPath = Path.Combine(forestDir, "plans", planId.Trim(), "plan.yaml");
            if (!File.Exists(planYamlPath))
            {
                continue;
            }

            try
            {
                var yaml = File.ReadAllText(planYamlPath);
                var parsed = PlanYamlLite.Parse(yaml);

                foreach (var p in parsed.Planters ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        planters.Add(p.Trim());
                    }
                }

                foreach (var p in parsed.Planners ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        planners.Add(p.Trim());
                    }
                }
            }
            catch
            {
                // best-effort: ignore invalid plan YAML
            }
        }

        return (
            planters.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
            planners.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
        );
    }
}


