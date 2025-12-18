using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.Distributed.Grains;

namespace GitForest.Infrastructure.Distributed.Repositories;

/// <summary>
/// Orleans-based repository implementation for Plant entities
/// </summary>
public sealed class OrleansPlantRepository
    : AbstractRepositoryWithSpecs<Plant, string>,
        IPlantRepository
{
    private readonly IGrainFactory _grainFactory;

    public OrleansPlantRepository(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
    }

    public override async Task<Plant?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var grain = _grainFactory.GetGrain<IPlantGrain>(id.Trim());
        return await grain.GetAsync();
    }

    public override async Task AddAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key))
            throw new ArgumentException("Plant.Key must be provided.", nameof(entity));

        var id = entity.Key.Trim();
        var grain = _grainFactory.GetGrain<IPlantGrain>(id);

        var existing = await grain.GetAsync();
        if (existing is not null)
        {
            throw new InvalidOperationException($"Plant '{id}' already exists.");
        }

        await grain.SetAsync(entity);

        var indexGrain = _grainFactory.GetGrain<IPlantIndexGrain>(0);
        await indexGrain.AddIdAsync(id);
    }

    public override async Task UpdateAsync(
        Plant entity,
        CancellationToken cancellationToken = default
    )
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key))
            throw new ArgumentException("Plant.Key must be provided.", nameof(entity));

        var id = entity.Key.Trim();
        var grain = _grainFactory.GetGrain<IPlantGrain>(id);
        await grain.SetAsync(entity);
    }

    public override async Task DeleteAsync(
        Plant entity,
        CancellationToken cancellationToken = default
    )
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key))
            return;

        var id = entity.Key.Trim();
        var grain = _grainFactory.GetGrain<IPlantGrain>(id);
        await grain.DeleteAsync();

        var indexGrain = _grainFactory.GetGrain<IPlantIndexGrain>(0);
        await indexGrain.RemoveIdAsync(id);
    }

    protected override Task<IReadOnlyList<Plant>> LoadAllAsync(
        CancellationToken cancellationToken = default
    ) => GetAllAsync(cancellationToken);

    private async Task<IReadOnlyList<Plant>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        var indexGrain = _grainFactory.GetGrain<IPlantIndexGrain>(0);
        var ids = await indexGrain.GetAllIdsAsync();

        var tasks = ids.Select(id => _grainFactory.GetGrain<IPlantGrain>(id).GetAsync());
        var results = await Task.WhenAll(tasks);

        return results.Where(p => p is not null).Cast<Plant>().ToList();
    }
}
