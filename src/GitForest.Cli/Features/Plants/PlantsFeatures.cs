using MediatR;

namespace GitForest.Cli.Features.Plants;

public sealed record ListPlantsQuery(string? StatusFilter, string? PlanFilter)
    : IRequest<IReadOnlyList<ForestStore.PlantRecord>>;

internal sealed class ListPlantsHandler
    : IRequestHandler<ListPlantsQuery, IReadOnlyList<ForestStore.PlantRecord>>
{
    public Task<IReadOnlyList<ForestStore.PlantRecord>> Handle(
        ListPlantsQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
        return Task.FromResult(
            (IReadOnlyList<ForestStore.PlantRecord>)
                ForestStore.ListPlants(forestDir, request.StatusFilter, request.PlanFilter)
        );
    }
}

public sealed record GetPlantQuery(string Selector) : IRequest<ForestStore.PlantRecord>;

internal sealed class GetPlantHandler : IRequestHandler<GetPlantQuery, ForestStore.PlantRecord>
{
    public Task<ForestStore.PlantRecord> Handle(
        GetPlantQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
        var plant = ForestStore.ResolvePlant(forestDir, request.Selector);
        return Task.FromResult(plant);
    }
}

public sealed record AssignPlanterToPlantCommand(string Selector, string PlanterId, bool DryRun)
    : IRequest<ForestStore.PlantRecord>;

internal sealed class AssignPlanterToPlantHandler
    : IRequestHandler<AssignPlanterToPlantCommand, ForestStore.PlantRecord>
{
    public Task<ForestStore.PlantRecord> Handle(
        AssignPlanterToPlantCommand request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
        var updated = ForestStore.UpdatePlant(
            forestDir,
            request.Selector,
            plant =>
            {
                var normalizedPlanterId = (request.PlanterId ?? string.Empty).Trim();
                if (normalizedPlanterId.Length == 0)
                {
                    return plant;
                }

                var planters = (plant.AssignedPlanters ?? Array.Empty<string>()).ToList();
                if (
                    !planters.Any(p =>
                        string.Equals(p, normalizedPlanterId, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    planters.Add(normalizedPlanterId);
                }

                var status = plant.Status;
                if (string.Equals(status, "planned", StringComparison.OrdinalIgnoreCase))
                {
                    status = "planted";
                }

                return plant with
                {
                    AssignedPlanters = planters,
                    Status = status,
                };
            },
            request.DryRun
        );

        return Task.FromResult(updated);
    }
}

public sealed record UnassignPlanterFromPlantCommand(string Selector, string PlanterId, bool DryRun)
    : IRequest<ForestStore.PlantRecord>;

internal sealed class UnassignPlanterFromPlantHandler
    : IRequestHandler<UnassignPlanterFromPlantCommand, ForestStore.PlantRecord>
{
    public Task<ForestStore.PlantRecord> Handle(
        UnassignPlanterFromPlantCommand request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
        var updated = ForestStore.UpdatePlant(
            forestDir,
            request.Selector,
            plant =>
            {
                var normalizedPlanterId = (request.PlanterId ?? string.Empty).Trim();
                var planters = (plant.AssignedPlanters ?? Array.Empty<string>())
                    .Where(p =>
                        !string.Equals(p, normalizedPlanterId, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
                return plant with { AssignedPlanters = planters };
            },
            request.DryRun
        );

        return Task.FromResult(updated);
    }
}

public sealed record ListPlantBranchesQuery(string Selector) : IRequest<PlantBranchesResult>;

public sealed record PlantBranchesResult(string PlantKey, string[] Branches);

internal sealed class ListPlantBranchesHandler
    : IRequestHandler<ListPlantBranchesQuery, PlantBranchesResult>
{
    public Task<PlantBranchesResult> Handle(
        ListPlantBranchesQuery request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
        var plant = ForestStore.ResolvePlant(forestDir, request.Selector);
        var branches = (plant.Branches ?? Array.Empty<string>()).ToArray();
        return Task.FromResult(new PlantBranchesResult(PlantKey: plant.Key, Branches: branches));
    }
}

public sealed record HarvestPlantCommand(string Selector, bool Force, bool DryRun)
    : IRequest<ForestStore.PlantRecord>;

internal sealed class HarvestPlantHandler
    : IRequestHandler<HarvestPlantCommand, ForestStore.PlantRecord>
{
    public Task<ForestStore.PlantRecord> Handle(
        HarvestPlantCommand request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
        var updated = ForestStore.UpdatePlant(
            forestDir,
            request.Selector,
            plant =>
            {
                if (
                    !request.Force
                    && !string.Equals(
                        plant.Status,
                        "harvestable",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    throw new InvalidOperationException(
                        $"Plant is not harvestable (status={plant.Status})."
                    );
                }

                return plant with
                {
                    Status = "harvested",
                };
            },
            request.DryRun
        );

        return Task.FromResult(updated);
    }
}

public sealed record ArchivePlantCommand(string Selector, bool Force, bool DryRun)
    : IRequest<ForestStore.PlantRecord>;

internal sealed class ArchivePlantHandler
    : IRequestHandler<ArchivePlantCommand, ForestStore.PlantRecord>
{
    public Task<ForestStore.PlantRecord> Handle(
        ArchivePlantCommand request,
        CancellationToken cancellationToken
    )
    {
        _ = cancellationToken;

        var forestDir = ForestStore.GetForestDir(ForestStore.DefaultForestDirName);
        var updated = ForestStore.UpdatePlant(
            forestDir,
            request.Selector,
            plant =>
            {
                if (
                    !request.Force
                    && !string.Equals(plant.Status, "harvested", StringComparison.OrdinalIgnoreCase)
                )
                {
                    throw new InvalidOperationException(
                        $"Plant is not harvested (status={plant.Status})."
                    );
                }

                return plant with
                {
                    Status = "archived",
                };
            },
            request.DryRun
        );

        return Task.FromResult(updated);
    }
}

