using GitForest.Application.Features.Plans;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plans;
using GitForest.Core.Specifications.Plants;
using GitForest.Mediator;

namespace GitForest.Application.Features.Planters;

public sealed record AssignDefaultPlantersCommand(
    string? PlanId,
    bool Single,
    bool Reset,
    bool OnlyUnassigned,
    bool DryRun
) : IRequest<AssignDefaultPlantersResult>;

public sealed record AssignDefaultPlantersResult(
    string? PlanId,
    int PlantsConsidered,
    int PlantsUpdated,
    bool DryRun
);

internal sealed class AssignDefaultPlantersHandler
    : IRequestHandler<AssignDefaultPlantersCommand, AssignDefaultPlantersResult>
{
    private readonly IPlanRepository _plans;
    private readonly IPlantRepository _plants;

    public AssignDefaultPlantersHandler(IPlanRepository plans, IPlantRepository plants)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<AssignDefaultPlantersResult> Handle(
        AssignDefaultPlantersCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var planId = Normalize(request.PlanId);
        var allPlans = await _plans.ListAsync(new AllPlansSpec(), cancellationToken);
        var plans = allPlans
            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
            .ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        if (planId is not null && !plans.ContainsKey(planId))
        {
            throw new PlanNotInstalledException(planId);
        }

        var plantList = planId is null
            ? await _plants.ListAsync(new AllPlantsSpec(), cancellationToken)
            : await _plants.ListAsync(new PlantsByPlanIdSpec(planId), cancellationToken);

        var considered = 0;
        var updatedCount = 0;

        foreach (var plant in plantList)
        {
            if (plant is null || string.IsNullOrWhiteSpace(plant.Key))
            {
                continue;
            }

            var plantPlanId = (plant.PlanId ?? string.Empty).Trim();
            if (plantPlanId.Length == 0)
            {
                continue;
            }

            if (!plans.TryGetValue(plantPlanId, out var plan))
            {
                continue;
            }

            var existingPlanters = NormalizePlanters(plant.AssignedPlanters);
            if (request.OnlyUnassigned && existingPlanters.Count > 0)
            {
                continue;
            }

            var defaultPlanters = NormalizePlanters(plan.Planters);
            if (defaultPlanters.Count == 0)
            {
                if (!request.Reset)
                {
                    continue;
                }
            }

            var targetPlanters = request.Single
                ? SelectSinglePlanter(defaultPlanters)
                : defaultPlanters;

            if (targetPlanters.Count == 0 && !request.Reset)
            {
                continue;
            }

            var nextPlanters = request.Reset
                ? targetPlanters
                : MergePlanters(existingPlanters, targetPlanters);

            considered++;
            if (ListEquals(existingPlanters, nextPlanters))
            {
                continue;
            }

            updatedCount++;

            if (!request.DryRun)
            {
                var updated = Clone(plant);
                updated.AssignedPlanters = nextPlanters.ToList();
                if (
                    string.Equals(updated.Status, "planned", StringComparison.OrdinalIgnoreCase)
                    && updated.AssignedPlanters.Count > 0
                )
                {
                    updated.Status = "planted";
                }

                updated.LastActivityDate = DateTime.UtcNow;
                await _plants.UpdateAsync(updated, cancellationToken);
            }
        }

        return new AssignDefaultPlantersResult(
            PlanId: planId,
            PlantsConsidered: considered,
            PlantsUpdated: updatedCount,
            DryRun: request.DryRun
        );
    }

    private static List<string> NormalizePlanters(IEnumerable<string>? planters)
    {
        var list = new List<string>();
        if (planters is null)
        {
            return list;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in planters)
        {
            var value = (raw ?? string.Empty).Trim();
            if (value.Length == 0 || !seen.Add(value))
            {
                continue;
            }

            list.Add(value);
        }

        return list;
    }

    private static List<string> SelectSinglePlanter(IReadOnlyList<string> planters)
    {
        if (planters.Count == 0)
        {
            return new List<string>();
        }

        return new List<string> { planters[0] };
    }

    private static List<string> MergePlanters(
        IReadOnlyList<string> existing,
        IReadOnlyList<string> target
    )
    {
        var merged = new List<string>(existing.Count + target.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in existing)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0 || !seen.Add(trimmed))
            {
                continue;
            }

            merged.Add(trimmed);
        }

        foreach (var value in target)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0 || !seen.Add(trimmed))
            {
                continue;
            }

            merged.Add(trimmed);
        }

        return merged;
    }

    private static bool ListEquals(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string? Normalize(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static Plant Clone(Plant source)
    {
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
