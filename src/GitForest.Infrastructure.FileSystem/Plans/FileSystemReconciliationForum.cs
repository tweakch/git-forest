using System.Text;
using GitForest.Core.Services;
using GitForest.Infrastructure.FileSystem.Serialization;

namespace GitForest.Infrastructure.FileSystem.Plans;

/// <summary>
/// Filesystem-backed reconciliation forum.
/// Today this preserves deterministic "plan.yaml plant_templates seeding" behavior
/// while allowing the application-layer reconciler to work via the forum port.
/// </summary>
public sealed class FileSystemReconciliationForum : IReconciliationForum
{
    private readonly FileSystemForestPaths _paths;

    public FileSystemReconciliationForum(string forestDir)
    {
        _paths = new FileSystemForestPaths(forestDir);
    }

    public Task<ReconciliationStrategy> RunAsync(
        ReconcileContext context,
        CancellationToken cancellationToken = default
    )
    {
        _ = cancellationToken;
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(context.PlanId))
        {
            return Task.FromResult(new ReconciliationStrategy(Array.Empty<DesiredPlant>()));
        }

        var planId = context.PlanId.Trim();
        var planYamlPath = _paths.PlanYamlPath(planId);
        if (!File.Exists(planYamlPath))
        {
            return Task.FromResult(new ReconciliationStrategy(Array.Empty<DesiredPlant>()));
        }

        var planYaml = File.ReadAllText(planYamlPath, Encoding.UTF8);
        var parsed = PlanYamlLite.Parse(planYaml);

        var templates =
            parsed.PlantTemplateNames.Count > 0
                ? parsed.PlantTemplateNames
                : new List<string> { "default-plant" };
        var planners =
            parsed.Planners.Count > 0 ? parsed.Planners : new List<string> { "default-planner" };
        var planters = parsed.Planters.Count > 0 ? parsed.Planters : new List<string>();

        var desired = new List<DesiredPlant>(templates.Count);
        for (var i = 0; i < templates.Count; i++)
        {
            var slug = NormalizeSlug(templates[i]);
            var key = $"{planId}:{slug}";

            var plannerId = planners[i % planners.Count];
            var assignedPlanters =
                planters.Count > 0 ? new[] { planters[i % planters.Count] } : Array.Empty<string>();

            var title = $"{parsed.Name}".Trim() == string.Empty ? slug : $"{parsed.Name}: {slug}";
            desired.Add(
                new DesiredPlant(
                    Key: key,
                    Slug: slug,
                    Title: title,
                    Description: string.Empty,
                    PlannerId: plannerId,
                    AssignedPlanters: assignedPlanters
                )
            );
        }

        return Task.FromResult(new ReconciliationStrategy(desired));
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

