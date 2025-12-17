using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.Distributed.Grains;

namespace GitForest.Infrastructure.Distributed.Repositories;

/// <summary>
/// Orleans-based repository implementation for Planter entities
/// </summary>
public sealed class OrleansPlanterRepository : IPlanterRepository
{
    private readonly IGrainFactory _grainFactory;
    private readonly ISpecificationEvaluator _specificationEvaluator;

    public OrleansPlanterRepository(IGrainFactory grainFactory, ISpecificationEvaluator specificationEvaluator)
    {
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        _specificationEvaluator = specificationEvaluator ?? throw new ArgumentNullException(nameof(specificationEvaluator));
    }

    public async Task<Planter?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var grain = _grainFactory.GetGrain<IPlanterGrain>(id.Trim());
        return await grain.GetAsync();
    }

    public async Task<Planter?> GetBySpecAsync(ISpecification<Planter> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).FirstOrDefault();
    }

    public async Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Planter>> ListAsync(ISpecification<Planter> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).ToList();
    }

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).ToList();
    }

    public async Task AddAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planter.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        var grain = _grainFactory.GetGrain<IPlanterGrain>(id);
        
        var existing = await grain.GetAsync();
        if (existing is not null)
        {
            throw new InvalidOperationException($"Planter '{id}' already exists.");
        }

        await grain.SetAsync(entity);

        var indexGrain = _grainFactory.GetGrain<IPlanterIndexGrain>(0);
        await indexGrain.AddIdAsync(id);
    }

    public async Task UpdateAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planter.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        var grain = _grainFactory.GetGrain<IPlanterGrain>(id);
        await grain.SetAsync(entity);
    }

    public async Task DeleteAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return;

        var id = entity.Id.Trim();
        var grain = _grainFactory.GetGrain<IPlanterGrain>(id);
        await grain.DeleteAsync();

        var indexGrain = _grainFactory.GetGrain<IPlanterIndexGrain>(0);
        await indexGrain.RemoveIdAsync(id);
    }

    private async Task<IReadOnlyList<Planter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var indexGrain = _grainFactory.GetGrain<IPlanterIndexGrain>(0);
        var ids = await indexGrain.GetAllIdsAsync();

        var tasks = ids.Select(id => _grainFactory.GetGrain<IPlanterGrain>(id).GetAsync());
        var results = await Task.WhenAll(tasks);

        return results.Where(p => p is not null).Cast<Planter>().ToList();
    }
}
