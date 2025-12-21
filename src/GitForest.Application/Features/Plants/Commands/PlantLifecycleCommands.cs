using GitForest.Application.Features.Plants;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Core.Specifications.Plants;
using GitForest.Mediator;

namespace GitForest.Application.Features.Plants.Commands;

public sealed record AssignPlanterToPlantCommand(string Selector, string PlanterId, bool DryRun)
    : IRequest<Plant>;

public sealed class PlantNotFoundException : Exception
{
    public string Selector { get; }

    public PlantNotFoundException(string selector)
        : base($"Plant not found: '{selector}'.")
    {
        Selector = selector;
    }
}

public sealed class PlantAmbiguousSelectorException : Exception
{
    public string Selector { get; }
    public string[] Matches { get; }

    public PlantAmbiguousSelectorException(string selector, string[] matches)
        : base($"Plant selector is ambiguous: '{selector}'.")
    {
        Selector = selector;
        Matches = matches ?? Array.Empty<string>();
    }
}

internal sealed class AssignPlanterToPlantHandler
    : IRequestHandler<AssignPlanterToPlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public AssignPlanterToPlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(
        AssignPlanterToPlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await PlantSelector.ResolveAsync(_plants, request.Selector, cancellationToken);
        var updated = Clone(resolved);

        var normalizedPlanterId = (request.PlanterId ?? string.Empty).Trim();
        if (normalizedPlanterId.Length == 0)
        {
            return resolved;
        }

        var planters = (updated.AssignedPlanters ?? new List<string>()).ToList();
        if (
            !planters.Any(p =>
                string.Equals(p, normalizedPlanterId, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            planters.Add(normalizedPlanterId);
        }

        updated.AssignedPlanters = planters;

        if (string.Equals(updated.Status, "planned", StringComparison.OrdinalIgnoreCase))
        {
            updated.Status = "planted";
        }

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return updated;
    }

    internal static Plant Clone(Plant source)
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
            CreatedDate = source.CreatedDate,
            LastActivityDate = source.LastActivityDate,
        };
    }
}

public sealed record UnassignPlanterFromPlantCommand(string Selector, string PlanterId, bool DryRun)
    : IRequest<Plant>;

internal sealed class UnassignPlanterFromPlantHandler
    : IRequestHandler<UnassignPlanterFromPlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public UnassignPlanterFromPlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(
        UnassignPlanterFromPlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await PlantSelector.ResolveAsync(_plants, request.Selector, cancellationToken);

        var updated = AssignPlanterToPlantHandler.Clone(resolved);

        var normalizedPlanterId = (request.PlanterId ?? string.Empty).Trim();
        var planters = (updated.AssignedPlanters ?? new List<string>())
            .Where(p =>
                !string.Equals(
                    (p ?? string.Empty).Trim(),
                    normalizedPlanterId,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        updated.AssignedPlanters = planters;

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return updated;
    }
}

public sealed record HarvestPlantCommand(string Selector, bool Force, bool DryRun)
    : IRequest<Plant>;

internal sealed class HarvestPlantHandler : IRequestHandler<HarvestPlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public HarvestPlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(
        HarvestPlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await PlantSelector.ResolveAsync(_plants, request.Selector, cancellationToken);
        var updated = AssignPlanterToPlantHandler.Clone(resolved);

        if (
            !request.Force
            && !string.Equals(updated.Status, "harvestable", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                $"Plant is not harvestable (status={updated.Status})."
            );
        }

        updated.Status = "harvested";

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return updated;
    }
}

public sealed record ArchivePlantCommand(string Selector, bool Force, bool DryRun)
    : IRequest<Plant>;

internal sealed class ArchivePlantHandler : IRequestHandler<ArchivePlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public ArchivePlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(
        ArchivePlantCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await PlantSelector.ResolveAsync(_plants, request.Selector, cancellationToken);
        var updated = AssignPlanterToPlantHandler.Clone(resolved);

        if (
            !request.Force
            && !string.Equals(updated.Status, "harvested", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                $"Plant is not harvested (status={updated.Status})."
            );
        }

        updated.Status = "archived";

        if (!request.DryRun)
        {
            updated.LastActivityDate = DateTime.UtcNow;
            await _plants.UpdateAsync(updated, cancellationToken);
        }

        return updated;
    }
}

public sealed record RemovePlantCommand(string Selector, bool Force, bool DryRun) : IRequest<Plant>;

internal sealed class RemovePlantHandler : IRequestHandler<RemovePlantCommand, Plant>
{
    private readonly IPlantRepository _plants;

    public RemovePlantHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<Plant> Handle(RemovePlantCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resolved = await PlantSelector.ResolveAsync(_plants, request.Selector, cancellationToken);

        if (
            !request.Force
            && !string.Equals(resolved.Status, "archived", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                $"Plant must be archived before removal (status={resolved.Status}). Use --force to override."
            );
        }

        var snapshot = AssignPlanterToPlantHandler.Clone(resolved);

        if (!request.DryRun)
        {
            await _plants.DeleteAsync(resolved, cancellationToken);
        }

        return snapshot;
    }
}

public sealed record RemovePlantsByPlanCommand(string PlanId, bool Force, bool DryRun)
    : IRequest<RemovePlantsByPlanResult>;

public sealed record RemovePlantsByPlanResult(string PlanId, bool DryRun, bool Force, string[] PlantKeys);

internal sealed class RemovePlantsByPlanHandler
    : IRequestHandler<RemovePlantsByPlanCommand, RemovePlantsByPlanResult>
{
    private readonly IPlantRepository _plants;

    public RemovePlantsByPlanHandler(IPlantRepository plants)
    {
        _plants = plants ?? throw new ArgumentNullException(nameof(plants));
    }

    public async Task<RemovePlantsByPlanResult> Handle(
        RemovePlantsByPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var planId = (request.PlanId ?? string.Empty).Trim();
        if (planId.Length == 0)
        {
            throw new InvalidOperationException("Plan ID is required.");
        }

        var plants = await _plants.ListAsync(new PlantsByPlanIdSpec(planId), cancellationToken);
        var ordered = plants
            .OrderBy(p => p.Key ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var plant in ordered)
        {
            if (plant is null)
            {
                continue;
            }

            if (
                !request.Force
                && !string.Equals(plant.Status, "archived", StringComparison.OrdinalIgnoreCase)
            )
            {
                throw new InvalidOperationException(
                    $"Plant '{plant.Key}' must be archived before removal (status={plant.Status}). Use --force to override."
                );
            }
        }

        if (!request.DryRun)
        {
            foreach (var plant in ordered)
            {
                if (plant is null)
                {
                    continue;
                }

                await _plants.DeleteAsync(plant, cancellationToken);
            }
        }

        return new RemovePlantsByPlanResult(
            PlanId: planId,
            DryRun: request.DryRun,
            Force: request.Force,
            PlantKeys: ordered
                .Select(p => p.Key)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim())
                .ToArray()
        );
    }
}
