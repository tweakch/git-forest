using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;
using GitForest.Infrastructure.Distributed.Grains;

namespace GitForest.Infrastructure.Distributed.Repositories;

/// <summary>
/// Orleans-based repository implementation for Plan entities
/// </summary>
public sealed class OrleansPlansRepository : IPlanRepository
{
    private readonly IGrainFactory _grainFactory;
    private readonly ISpecificationEvaluator _specificationEvaluator;

    public OrleansPlansRepository(IGrainFactory grainFactory, ISpecificationEvaluator specificationEvaluator)
    {
        _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
        _specificationEvaluator = specificationEvaluator ?? throw new ArgumentNullException(nameof(specificationEvaluator));
    }

    public async Task<Plan?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var grain = _grainFactory.GetGrain<IPlanGrain>(id.Trim());
        return await grain.GetAsync();
    }

    public async Task<Plan?> GetBySpecAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).FirstOrDefault();
    }

    public async Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Plan>> ListAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).ToList();
    }

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return _specificationEvaluator.Evaluate(all, specification).ToList();
    }

    public async Task AddAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Plan.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        var grain = _grainFactory.GetGrain<IPlanGrain>(id);
        
        var existing = await grain.GetAsync();
        if (existing is not null)
        {
            throw new InvalidOperationException($"Plan '{id}' already exists.");
        }

        await grain.SetAsync(entity);

        var indexGrain = _grainFactory.GetGrain<IPlanIndexGrain>(0);
        await indexGrain.AddIdAsync(id);
    }

    public async Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Plan.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        var grain = _grainFactory.GetGrain<IPlanGrain>(id);
        await grain.SetAsync(entity);
    }

    public async Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return;

        var id = entity.Id.Trim();
        var grain = _grainFactory.GetGrain<IPlanGrain>(id);
        await grain.DeleteAsync();

        var indexGrain = _grainFactory.GetGrain<IPlanIndexGrain>(0);
        await indexGrain.RemoveIdAsync(id);
    }

    private async Task<IReadOnlyList<Plan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var indexGrain = _grainFactory.GetGrain<IPlanIndexGrain>(0);
        var ids = await indexGrain.GetAllIdsAsync();

        var tasks = ids.Select(id => _grainFactory.GetGrain<IPlanGrain>(id).GetAsync());
        var results = await Task.WhenAll(tasks);

        return results.Where(p => p is not null).Cast<Plan>().ToList();
    }
}
