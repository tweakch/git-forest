using GitForest.Application.Features.Plants;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plants;
using GitForest.Mediator;

namespace GitForest.Application.Features.Reconcile;

public sealed record ReconcilePlantCommand(
    string Selector,
    string? SelectedBranch,
    string? Status,
    bool Prune,
    bool Force,
    bool DryRun
) : IRequest<ReconcilePlantResult>;

public sealed record ReconcilePlantResult(
    string PlantKey,
    string Status,
    string? SelectedBranch,
    bool Pruned,
    bool DryRun
);

public sealed record ReconcileForestCommand(string? PlanId, bool DryRun)
    : IRequest<ReconcileForestResult>;

public sealed record ReconcileForestResult(
    string? PlanId,
    int PlantsUpdated,
    int NeedsSelection,
    bool DryRun
);

internal sealed class ReconcilePlantHandler
    : IRequestHandler<ReconcilePlantCommand, ReconcilePlantResult>
{
    private readonly IPlantRepository _plants;

    public ReconcilePlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<ReconcilePlantResult> Handle(
        ReconcilePlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await PlantSelector.ResolveAsync(
            _plants,
            request.Selector,
            cancellationToken
        );
        var updated = Clone(resolved);

        var selected = NormalizeBranch(request.SelectedBranch);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            var known = (updated.Branches ?? new List<string>()).Any(b =>
                string.Equals(b, selected, StringComparison.OrdinalIgnoreCase)
            );
            if (!known)
            {
                throw new InvalidOperationException(
                    $"Branch '{selected}' is not recorded for plant '{updated.Key}'."
                );
            }

            updated.SelectedBranch = selected;
        }

        if (request.Prune && !string.IsNullOrWhiteSpace(updated.SelectedBranch))
        {
            updated.Branches = (updated.Branches ?? new List<string>())
                .Where(b =>
                    string.Equals(b, updated.SelectedBranch, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
        }

        var targetStatus = NormalizeStatus(request.Status);
        if (targetStatus is null && ShouldAutoHarvestable(updated))
        {
            targetStatus = "harvestable";
        }

        if (!string.IsNullOrWhiteSpace(targetStatus))
        {
            var current = updated.Status ?? "planned";
            if (!request.Force && !CanTransition(current, targetStatus))
            {
                throw new InvalidOperationException(
                    $"Cannot transition plant from '{current}' to '{targetStatus}'."
                );
            }

            updated.Status = targetStatus;
        }

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return new ReconcilePlantResult(
            PlantKey: updated.Key,
            Status: updated.Status,
            SelectedBranch: updated.SelectedBranch,
            Pruned: request.Prune,
            DryRun: request.DryRun
        );
    }

    private static bool ShouldAutoHarvestable(Plant plant)
    {
        var status = plant.Status ?? string.Empty;
        if (!string.Equals(status, "growing", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return (plant.Branches ?? new List<string>()).Count > 0;
    }

    private static bool CanTransition(string current, string target)
    {
        if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return target switch
        {
            "harvestable" => current is "planned" or "planted" or "growing",
            "harvested" => current is "harvestable",
            "archived" => current is "harvested",
            _ => false,
        };
    }

    private static string? NormalizeStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return null;
        }

        return normalized switch
        {
            "harvestable" => "harvestable",
            "harvested" => "harvested",
            "archived" => "archived",
            _ => throw new InvalidOperationException(
                "Invalid --status. Expected: harvestable|harvested|archived"
            ),
        };
    }

    private static string? NormalizeBranch(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
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

internal sealed class ReconcileForestHandler
    : IRequestHandler<ReconcileForestCommand, ReconcileForestResult>
{
    private readonly IPlantRepository _plants;

    public ReconcileForestHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<ReconcileForestResult> Handle(
        ReconcileForestCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var planId = NormalizePlanId(request.PlanId);
        var all = await _plants.ListAsync(new AllPlantsSpec(), cancellationToken);

        var updated = 0;
        var needsSelection = 0;

        foreach (var plant in all)
        {
            if (plant is null || string.IsNullOrWhiteSpace(plant.Key))
            {
                continue;
            }

            if (
                planId is not null
                && !string.Equals(plant.PlanId, planId, StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            var status = plant.Status ?? string.Empty;
            if (!string.Equals(status, "growing", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var branches = (plant.Branches ?? new List<string>())
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Select(b => b.Trim())
                .ToList();

            if (branches.Count == 0)
            {
                continue;
            }

            var updatedPlant = Clone(plant);
            if (string.IsNullOrWhiteSpace(updatedPlant.SelectedBranch))
            {
                if (branches.Count == 1)
                {
                    updatedPlant.SelectedBranch = branches[0];
                }
                else
                {
                    needsSelection++;
                }
            }

            updatedPlant.Status = "harvestable";
            updated++;

            if (!request.DryRun)
            {
                updatedPlant.LastActivityDate = DateTime.UtcNow;
                await _plants.UpdateAsync(updatedPlant, cancellationToken);
            }
        }

        return new ReconcileForestResult(
            PlanId: planId,
            PlantsUpdated: updated,
            NeedsSelection: needsSelection,
            DryRun: request.DryRun
        );
    }

    private static string? NormalizePlanId(string? planId)
    {
        var trimmed = (planId ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
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
