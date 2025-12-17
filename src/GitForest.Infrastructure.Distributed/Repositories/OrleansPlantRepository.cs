using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.Distributed.Grains;

namespace GitForest.Infrastructure.Distributed.Repositories;

/// <summary>
/// Orleans-based repository implementation for Plant entities
/// </summary>
public sealed class OrleansPlantRepository : IPlantRepository
{
    private readonly IGrainFactory _grainFactory;
    private readonly ISpecificationEvaluator _specificationEvaluator;

    public OrleansPlantRepository(IGrainFactory grainFactory, ISpecificationEvaluator specificationEvaluator)
    {
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        _specificationEvaluator = specificationEvaluator ?? throw new ArgumentNullException(nameof(specificationEvaluator));
    }

    public async Task<Plant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var grain = _grainFactory.GetGrain<IPlantGrain>(id.Trim());
        return await grain.GetAsync();
    }

    public async Task<Plant?> GetBySpecAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).FirstOrDefault();
    }

    public async Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Plant>> ListAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).ToList();
    }

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).ToList();
    }

    public async Task AddAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) throw new ArgumentException("Plant.Key must be provided.", nameof(entity));

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

    public async Task UpdateAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) throw new ArgumentException("Plant.Key must be provided.", nameof(entity));

        var id = entity.Key.Trim();
        var grain = _grainFactory.GetGrain<IPlantGrain>(id);
        await grain.SetAsync(entity);
    }

    public async Task DeleteAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) return;

        var id = entity.Key.Trim();
        var grain = _grainFactory.GetGrain<IPlantGrain>(id);
        await grain.DeleteAsync();

        var indexGrain = _grainFactory.GetGrain<IPlantIndexGrain>(0);
        await indexGrain.RemoveIdAsync(id);
    }

    private async Task<IReadOnlyList<Plant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var indexGrain = _grainFactory.GetGrain<IPlantIndexGrain>(0);
        var ids = await indexGrain.GetAllIdsAsync();

        var tasks = ids.Select(id => _grainFactory.GetGrain<IPlantGrain>(id).GetAsync());
        var results = await Task.WhenAll(tasks);

        return results.Where(p => p is not null).Cast<Plant>().ToList();
    }
}
