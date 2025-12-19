using System.Globalization;
using System.Text;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Services;
using GitForest.Core.Specifications.Plans;
using GitForest.Core.Specifications.Plants;

namespace GitForest.Application.Features.Plans;

/// <summary>
/// Application-layer plan reconciler that delegates desired plant generation to a reconciliation forum,
/// then applies the resulting strategy to persisted plants idempotently via repositories.
/// </summary>
public sealed class ForumPlanReconciler : IPlanReconciler
{
    private readonly IPlanRepository _plans;
    private readonly IPlantRepository _plants;
    private readonly IReconciliationForumRouter _forums;

    public ForumPlanReconciler(
        IPlanRepository plans,
        IPlantRepository plants,
        IReconciliationForumRouter forums
    )
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
        _forums = forums ?? throw new ArgumentNullException(nameof(forums));
    }

    public async Task<(string planId, int plantsCreated, int plantsUpdated)> ReconcileAsync(
        string planId,
        bool dryRun,
        string? forum = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new ArgumentException("Plan ID must be provided.", nameof(planId));
        }

        var id = planId.Trim();

        var plan = await _plans.GetBySpecAsync(new PlanByIdSpec(id), cancellationToken);
        if (plan is null)
        {
            // Keep CLI behavior consistent: handler maps DirectoryNotFoundException => PlanNotInstalledException.
            throw new DirectoryNotFoundException($"Plan not installed: {id}");
        }

        var existingPlants = await _plants.ListAsync(new PlantsByPlanIdSpec(id), cancellationToken);
        var context = new ReconcileContext(
            PlanId: id,
            Plan: plan,
            RawPlanYaml: null,
            ExistingPlants: existingPlants,
            Repository: string.IsNullOrWhiteSpace(plan.Repository) ? null : plan.Repository
        );

        var strategy = await _forums.RunAsync(context, forum, cancellationToken);
        var desired = NormalizeDesiredPlants(id, plan, strategy?.DesiredPlants);

        var existingByKey = existingPlants
            .Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .ToDictionary(p => p.Key.Trim(), p => p, StringComparer.Ordinal);

        var created = 0;
        var updated = 0;
        var now = DateTime.UtcNow;

        foreach (var d in desired)
        {
            if (!existingByKey.TryGetValue(d.Key, out var existing))
            {
                created++;
                if (!dryRun)
                {
                    var plant = new Plant
                    {
                        Key = d.Key,
                        Slug = d.Slug,
                        PlanId = id,
                        PlannerId = d.PlannerId ?? string.Empty,
                        Status = "planned",
                        Title = d.Title ?? string.Empty,
                        Description = d.Description ?? string.Empty,
                        AssignedPlanters = (d.AssignedPlanters ?? Array.Empty<string>()).ToList(),
                        Branches = new List<string>(),
                        CreatedDate = now,
                        LastActivityDate = null,
                    };

                    await _plants.AddAsync(plant, cancellationToken);
                }

                continue;
            }

            // Update only plan-owned fields idempotently. Preserve lifecycle fields (status, branches, created, etc.).
            var normalizedPlannerId = d.PlannerId ?? string.Empty;
            var normalizedTitle = d.Title ?? string.Empty;
            var normalizedDescription = d.Description ?? string.Empty;
            var normalizedAssignedPlanters = (d.AssignedPlanters ?? Array.Empty<string>()).ToArray();
            var changed = false;

            if (!string.Equals(existing.PlanId ?? string.Empty, id, StringComparison.Ordinal))
            {
                existing.PlanId = id;
                changed = true;
            }

            if (!string.Equals(existing.Slug ?? string.Empty, d.Slug, StringComparison.Ordinal))
            {
                existing.Slug = d.Slug;
                changed = true;
            }

            if (
                !string.Equals(
                    existing.PlannerId ?? string.Empty,
                    normalizedPlannerId,
                    StringComparison.Ordinal
                )
            )
            {
                existing.PlannerId = normalizedPlannerId;
                changed = true;
            }

            if (
                !string.Equals(
                    existing.Title ?? string.Empty,
                    normalizedTitle,
                    StringComparison.Ordinal
                )
            )
            {
                existing.Title = normalizedTitle;
                changed = true;
            }

            if (
                !string.Equals(
                    existing.Description ?? string.Empty,
                    normalizedDescription,
                    StringComparison.Ordinal
                )
            )
            {
                existing.Description = normalizedDescription;
                changed = true;
            }

            if (!SameList(existing.AssignedPlanters, normalizedAssignedPlanters))
            {
                existing.AssignedPlanters = normalizedAssignedPlanters.ToList();
                changed = true;
            }

            if (changed)
            {
                updated++;
                if (!dryRun)
                {
                    await _plants.UpdateAsync(existing, cancellationToken);
                }
            }
        }

        return (planId: id, plantsCreated: created, plantsUpdated: updated);
    }

    private static IReadOnlyList<NormalizedDesiredPlant> NormalizeDesiredPlants(
        string planId,
        Plan plan,
        IReadOnlyList<DesiredPlant>? desiredPlants
    )
    {
        var id = (planId ?? string.Empty).Trim();
        if (id.Length == 0)
        {
            return Array.Empty<NormalizedDesiredPlant>();
        }

        var input = desiredPlants ?? Array.Empty<DesiredPlant>();

        var planters = NormalizeIdsPreserveOrder(plan.Planters);

        // Normalize fields first, then sort to ensure stable application order.
        var normalized = new List<NormalizedDesiredPlant>(input.Count);
        foreach (var d in input)
        {
            if (d is null)
            {
                continue;
            }

            var key = (d.Key ?? string.Empty).Trim();
            var slugFromKey = string.Empty;
            if (key.Length > 0)
            {
                var idx = key.IndexOf(':', StringComparison.Ordinal);
                if (idx > 0 && idx < key.Length - 1)
                {
                    var keyPlan = key[..idx].Trim();
                    slugFromKey = key[(idx + 1)..].Trim();
                    if (!string.Equals(keyPlan, id, StringComparison.Ordinal))
                    {
                        // If an upstream forum produced a key for a different plan, force it into this plan.
                        key = string.Empty;
                    }
                }
                else
                {
                    key = string.Empty;
                }
            }

            var rawSlug = !string.IsNullOrWhiteSpace(d.Slug) ? d.Slug : slugFromKey;
            if (string.IsNullOrWhiteSpace(rawSlug))
            {
                rawSlug = !string.IsNullOrWhiteSpace(d.Title) ? d.Title : "untitled";
            }

            var slug = NormalizeSlug(rawSlug);
            if (key.Length == 0)
            {
                key = $"{id}:{slug}";
            }

            var title = (d.Title ?? string.Empty).Trim();
            if (title.Length == 0)
            {
                title = slug;
            }

            var description = (d.Description ?? string.Empty).Trim();
            var plannerId = (d.PlannerId ?? string.Empty).Trim();

            var assigned = NormalizeIdsPreserveOrder(d.AssignedPlanters);

            normalized.Add(
                new NormalizedDesiredPlant(
                    Key: key,
                    Slug: slug,
                    Title: title,
                    Description: description,
                    PlannerId: plannerId,
                    AssignedPlanters: assigned
                )
            );
        }

        normalized.Sort(
            static (a, b) =>
            {
                var c = string.CompareOrdinal(a.Slug, b.Slug);
                if (c != 0)
                    return c;
                c = string.CompareOrdinal(a.Title, b.Title);
                if (c != 0)
                    return c;
                c = string.CompareOrdinal(a.PlannerId, b.PlannerId);
                if (c != 0)
                    return c;
                return string.CompareOrdinal(a.Key, b.Key);
            }
        );

        // Resolve slug collisions deterministically (affects Key).
        var usedSlugs = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < normalized.Count; i++)
        {
            var item = normalized[i];
            var baseSlug = item.Slug;
            var finalSlug = baseSlug;

            if (usedSlugs.Contains(finalSlug))
            {
                var suffix = 1;
                while (true)
                {
                    finalSlug = $"{baseSlug}-{suffix.ToString("00", CultureInfo.InvariantCulture)}";
                    if (!usedSlugs.Contains(finalSlug))
                    {
                        break;
                    }

                    suffix++;
                }
            }

            usedSlugs.Add(finalSlug);
            if (!string.Equals(finalSlug, item.Slug, StringComparison.Ordinal))
            {
                normalized[i] = item with { Slug = finalSlug, Key = $"{id}:{finalSlug}" };
            }
            else
            {
                // Ensure key matches the possibly normalized slug.
                normalized[i] = item with
                {
                    Key = $"{id}:{finalSlug}",
                };
            }
        }

        // Deterministic fallback planter assignment:
        // If a forum did not assign planters, assign a single planter from the plan's configured planters (round-robin).
        if (planters.Length > 0)
        {
            for (var i = 0; i < normalized.Count; i++)
            {
                var item = normalized[i];
                if (item.AssignedPlanters.Count > 0)
                {
                    continue;
                }

                var assigned = new[] { planters[i % planters.Length] };
                normalized[i] = item with { AssignedPlanters = assigned };
            }
        }

        return normalized;
    }

    private static string[] NormalizeIdsPreserveOrder(IReadOnlyList<string>? ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return Array.Empty<string>();
        }

        var results = new List<string>(ids.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in ids)
        {
            var value = (raw ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                continue;
            }

            if (seen.Add(value))
            {
                results.Add(value);
            }
        }

        return results.Count == 0 ? Array.Empty<string>() : results.ToArray();
    }

    private static bool SameList(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        a ??= Array.Empty<string>();
        b ??= Array.Empty<string>();

        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            var left = (a[i] ?? string.Empty).Trim();
            var right = (b[i] ?? string.Empty).Trim();
            if (!string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
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

    private sealed record NormalizedDesiredPlant(
        string Key,
        string Slug,
        string Title,
        string Description,
        string PlannerId,
        IReadOnlyList<string> AssignedPlanters
    );
}
