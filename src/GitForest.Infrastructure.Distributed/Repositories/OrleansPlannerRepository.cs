using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.Distributed.Grains;

namespace GitForest.Infrastructure.Distributed.Repositories;

/// <summary>
/// Orleans-based repository implementation for Planner entities
/// </summary>
public sealed class OrleansPlannerRepository : AbstractRepositoryWithSpecs<Planner, string>, IPlannerRepository
{
    private readonly IGrainFactory _grainFactory;

    public OrleansPlannerRepository(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
    }

    public override async Task<Planner?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var grain = _grainFactory.GetGrain<IPlannerGrain>(id.Trim());
        return await grain.GetAsync();
    }

    public override async Task AddAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planner.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        var grain = _grainFactory.GetGrain<IPlannerGrain>(id);
        
        var existing = await grain.GetAsync();
        if (existing is not null)
        {
            throw new InvalidOperationException($"Planner '{id}' already exists.");
        }

        await grain.SetAsync(entity);

        var indexGrain = _grainFactory.GetGrain<IPlannerIndexGrain>(0);
        await indexGrain.AddIdAsync(id);
    }

    public override async Task UpdateAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planner.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        var grain = _grainFactory.GetGrain<IPlannerGrain>(id);
        await grain.SetAsync(entity);
    }

    public override async Task DeleteAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return;

        var id = entity.Id.Trim();
        var grain = _grainFactory.GetGrain<IPlannerGrain>(id);
        await grain.DeleteAsync();

        var indexGrain = _grainFactory.GetGrain<IPlannerIndexGrain>(0);
        await indexGrain.RemoveIdAsync(id);
    }

    protected override Task<IReadOnlyList<Planner>> LoadAllAsync(CancellationToken cancellationToken = default)
        => GetAllAsync(cancellationToken);

    private async Task<IReadOnlyList<Planner>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var indexGrain = _grainFactory.GetGrain<IPlannerIndexGrain>(0);
        var ids = await indexGrain.GetAllIdsAsync();

        var tasks = ids.Select(id => _grainFactory.GetGrain<IPlannerGrain>(id).GetAsync());
        var results = await Task.WhenAll(tasks);

        return results.Where(p => p is not null).Cast<Planner>().ToList();
    }
}
