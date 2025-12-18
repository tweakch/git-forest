using System.Globalization;
using System.Text;
using GitForest.Core.Services;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Plans;

public sealed class FileSystemPlanReconciler : IPlanReconciler
{
    private readonly string _forestDir;

    public FileSystemPlanReconciler(string forestDir)
    {
        _forestDir = forestDir ?? string.Empty;
    }

    public Task<(string planId, int plantsCreated, int plantsUpdated)> ReconcileAsync(
        string planId,
        bool dryRun,
        string? forum = null,
        CancellationToken cancellationToken = default
    )
    {
        _ = forum;
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new ArgumentException("Plan ID must be provided.", nameof(planId));
        }

        var id = planId.Trim();
        var planDir = Path.Combine(_forestDir.Trim(), "plans", id);
        var planYamlPath = Path.Combine(planDir, "plan.yaml");
        if (!File.Exists(planYamlPath))
        {
            throw new DirectoryNotFoundException($"Plan not installed: {id}");
        }

        var planYaml = File.ReadAllText(planYamlPath, Encoding.UTF8);
        var plan = PlanYamlLite.Parse(planYaml);

        var plantsDir = Path.Combine(_forestDir.Trim(), "plants");
        Directory.CreateDirectory(plantsDir);

        var templates =
            plan.PlantTemplateNames.Count > 0
                ? plan.PlantTemplateNames
                : new List<string> { "default-plant" };
        var planners =
            plan.Planners.Count > 0 ? plan.Planners : new List<string> { "default-planner" };
        var planters = plan.Planters.Count > 0 ? plan.Planters : new List<string>();

        var created = 0;
        var updated = 0;

        for (var i = 0; i < templates.Count; i++)
        {
            var slug = NormalizeSlug(templates[i]);
            var key = $"{id}:{slug}";
            var dirName = $"{id}__{slug}";
            var plantDir = Path.Combine(plantsDir, dirName);
            var plantYamlPathOut = Path.Combine(plantDir, "plant.yaml");

            var plannerId = planners[i % planners.Count];
            var assignedPlanters =
                planters.Count > 0 ? new[] { planters[i % planters.Count] } : Array.Empty<string>();

            var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var plant = new PlantFileModel(
                Key: key,
                Status: "planned",
                Title: $"{plan.Name}".Trim() == string.Empty ? slug : $"{plan.Name}: {slug}",
                PlanId: id,
                PlannerId: plannerId,
                AssignedPlanters: assignedPlanters,
                Branches: Array.Empty<string>(),
                CreatedAt: now,
                UpdatedAt: null,
                Description: null
            );

            if (!Directory.Exists(plantDir) || !File.Exists(plantYamlPathOut))
            {
                if (!dryRun)
                {
                    Directory.CreateDirectory(plantDir);
                    File.WriteAllText(
                        plantYamlPathOut,
                        PlantYamlLite.Serialize(plant),
                        Encoding.UTF8
                    );
                }

                created++;
            }
            else
            {
                // For now, keep reconcile minimal: treat existing as up-to-date.
                updated++;
            }
        }

        return Task.FromResult((planId: id, plantsCreated: created, plantsUpdated: updated));
    }

    private static string NormalizeSlug(string input)
    {
        var trimmed = (input ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return "untitled";
        }

        // Keep it deterministic and file-system safe.
        var sb = new StringBuilder(trimmed.Length);
        var lastWasDash = false;
        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastWasDash = false;
                continue;
            }

            if (ch is '-' or '_' or ' ' or '.')
            {
                if (!lastWasDash)
                {
                    sb.Append('-');
                    lastWasDash = true;
                }
            }
        }

        var slug = sb.ToString().Trim('-');
        return slug.Length == 0 ? "untitled" : slug;
    }
}
