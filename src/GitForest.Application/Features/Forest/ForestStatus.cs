using GitForest.Core.Persistence;
using GitForest.Core.Services;
using GitForest.Core.Specifications.Plans;
using GitForest.Core.Specifications.Plants;
using MediatR;

namespace GitForest.Application.Features.Forest;

public sealed record GetForestStatusQuery() : IRequest<ForestStatusResult>;

public sealed record ForestStatusResult(
    int PlansCount,
    int PlantsCount,
    IReadOnlyDictionary<string, int> PlantsByStatus,
    string[] PlantersAvailable,
    string[] PlantersActive,
    string[] PlannersAvailable,
    string[] PlannersActive,
    string LockStatus);

internal sealed class GetForestStatusHandler : IRequestHandler<GetForestStatusQuery, ForestStatusResult>
{
    private readonly IPlanRepository _plans;
    private readonly IPlantRepository _plants;
    private readonly ILockStatusProvider _lockStatus;

    public GetForestStatusHandler(IPlanRepository plans, IPlantRepository plants, ILockStatusProvider lockStatus)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
        _lockStatus = lockStatus ?? throw new ArgumentNullException(nameof(lockStatus));
    }

    public async Task<ForestStatusResult> Handle(GetForestStatusQuery request, CancellationToken cancellationToken)
    {
        _ = request;

        var plans = await _plans.ListAsync(new AllPlansSpec(), cancellationToken);
        var plants = await _plants.ListAsync(new AllPlantsSpec(), cancellationToken);

        var plantsByStatus = plants
            .GroupBy(p => (p.Status ?? string.Empty).Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var plantersAvailable = plans
            .SelectMany(p => p.Planters ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var plannersAvailable = plans
            .SelectMany(p => p.Planners ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var nonArchivedPlants = plants
            .Where(p => !string.Equals(p.Status, "archived", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var plantersActive = nonArchivedPlants
            .SelectMany(p => p.AssignedPlanters ?? new List<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var plannersActive = nonArchivedPlants
            .Select(p => p.PlannerId)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var lockStatus = _lockStatus.GetLockStatus();

        return new ForestStatusResult(
            PlansCount: plans.Count,
            PlantsCount: plants.Count,
            PlantsByStatus: plantsByStatus,
            PlantersAvailable: plantersAvailable,
            PlantersActive: plantersActive,
            PlannersAvailable: plannersAvailable,
            PlannersActive: plannersActive,
            LockStatus: lockStatus);
    }
}

