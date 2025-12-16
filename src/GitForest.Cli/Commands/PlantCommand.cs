using System.CommandLine;
using System.CommandLine.Invocation;

namespace GitForest.Cli.Commands;

public static class PlantCommand
{
    public static Command Build(CliOptions cliOptions)
    {
        var plantCommand = new Command("plant", "Manage a specific plant");
        var selectorArg = new Argument<string>("selector", "Plant selector (key, slug, or P01)");
        plantCommand.AddArgument(selectorArg);

        var showCommand = new Command("show", "Show plant details");
        showCommand.SetHandler((InvocationContext context) =>
        {
            var output = context.GetOutput(cliOptions);
            var selector = context.ParseResult.GetValueForArgument(selectorArg);

            var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
            try
            {
                var plants = ForestStore.ListPlants(forestDir, statusFilter: null, planFilter: null);
                var matches = FindMatches(plants, selector);

                if (matches.Count == 1)
                {
                    var plant = matches[0];
                    if (output.Json)
                    {
                        output.WriteJson(new
                        {
                            plant = new
                            {
                                key = plant.Key,
                                status = plant.Status,
                                title = plant.Title,
                                planId = plant.PlanId,
                                plannerId = plant.PlannerId,
                                planters = plant.AssignedPlanters.ToArray()
                            }
                        });
                    }
                    else
                    {
                        var plantersText = plant.AssignedPlanters.Count == 0 ? "-" : string.Join(", ", plant.AssignedPlanters);
                        output.WriteLine($"Key: {plant.Key}");
                        output.WriteLine($"Status: {plant.Status}");
                        output.WriteLine($"Title: {plant.Title}");
                        output.WriteLine($"Plan: {plant.PlanId}");
                        output.WriteLine($"Planner: {plant.PlannerId ?? "-"}");
                        output.WriteLine($"Planters: {plantersText}");
                    }

                    context.ExitCode = ExitCodes.Success;
                    return;
                }

                if (matches.Count == 0)
                {
                    if (output.Json)
                    {
                        output.WriteJsonError(code: "plant_not_found", message: "Plant not found", details: new { selector });
                    }
                    else
                    {
                        output.WriteErrorLine($"Plant '{selector}': not found");
                    }

                    context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
                    return;
                }

                // Ambiguous selector.
                if (output.Json)
                {
                    output.WriteJsonError(
                        code: "plant_ambiguous",
                        message: "Plant selector is ambiguous",
                        details: new { selector, matches = matches.Select(p => p.Key).ToArray() });
                }
                else
                {
                    output.WriteErrorLine($"Plant '{selector}': ambiguous; matched {matches.Count} plants:");
                    foreach (var key in matches.Select(p => p.Key).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    {
                        output.WriteErrorLine($"- {key}");
                    }
                }

                context.ExitCode = ExitCodes.PlantNotFoundOrAmbiguous;
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

        plantCommand.AddCommand(showCommand);
        return plantCommand;
    }

    private static IReadOnlyList<ForestStore.PlantRecord> FindMatches(IReadOnlyList<ForestStore.PlantRecord> plants, string selector)
    {
        var sel = (selector ?? string.Empty).Trim();
        if (sel.Length == 0 || plants.Count == 0)
        {
            return Array.Empty<ForestStore.PlantRecord>();
        }

        // 1) Exact key match: <plan-id>:<slug>
        var exact = plants.Where(p => string.Equals(p.Key, sel, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (exact.Length > 0)
        {
            return exact;
        }

        // 2) Pxx style stable index into ordered list (best-effort; deterministic ordering).
        if (TryParsePIndex(sel, out var index))
        {
            var ordered = plants.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToArray();
            if (index >= 0 && index < ordered.Length)
            {
                return new[] { ordered[index] };
            }

            return Array.Empty<ForestStore.PlantRecord>();
        }

        // 3) Slug match: match any plant whose key right-side equals selector.
        var slugMatches = plants.Where(p =>
        {
            var key = p.Key ?? string.Empty;
            var idx = key.IndexOf(':', StringComparison.Ordinal);
            if (idx < 0 || idx == key.Length - 1)
            {
                return false;
            }

            var slug = key[(idx + 1)..];
            return string.Equals(slug, sel, StringComparison.OrdinalIgnoreCase);
        }).ToArray();

        return slugMatches;
    }

    private static bool TryParsePIndex(string selector, out int index)
    {
        // Accept P01, p1, P0003 → 1-based ordinal; convert to 0-based index.
        index = -1;
        if (selector.Length < 2)
        {
            return false;
        }

        if (selector[0] != 'p' && selector[0] != 'P')
        {
            return false;
        }

        var digits = selector[1..].Trim();
        if (digits.Length == 0)
        {
            return false;
        }

        foreach (var ch in digits)
        {
            if (!char.IsDigit(ch))
            {
                return false;
            }
        }

        if (!int.TryParse(digits, out var oneBased) || oneBased <= 0)
        {
            return false;
        }

        index = oneBased - 1;
        return true;
    }
}


