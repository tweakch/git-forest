using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlanRepository : IPlanRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Plan> _plans;

    public InMemoryPlanRepository(IEnumerable<Plan>? seed = null, IEqualityComparer<string>? idComparer = null)
    {
        _plans = new Dictionary<string, Plan>(idComparer ?? StringComparer.OrdinalIgnoreCase);

        if (seed is not null)
        {
            foreach (var p in seed)
            {
                if (p is null) continue;
                var id = (p.Id ?? string.Empty).Trim();
                if (id.Length == 0) continue;
                _plans[id] = p;
            }
        }
    }

    public Task<Plan?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Plan?>(null);

        lock (_gate)
        {
            _plans.TryGetValue(id.Trim(), out var found);
            return Task.FromResult(found);
        }
    }

    public Task<Plan?> GetBySpecAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Plan>> ListAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task AddAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Plan.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        lock (_gate)
        {
            if (_plans.ContainsKey(id))
            {
                throw new InvalidOperationException($"Plan '{id}' already exists.");
            }

            _plans[id] = entity;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Plan.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        lock (_gate)
        {
            _plans[id] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        lock (_gate)
        {
            _plans.Remove(entity.Id.Trim());
        }

        return Task.CompletedTask;
    }

    private Task<T?> GetBySpecInternalAsync<T>(ISpecification<Plan, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Plan> all;
        lock (_gate)
        {
            all = _plans.Values.ToList();
        }

        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Plan?> GetBySpecInternalAsync(ISpecification<Plan> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Plan> all;
        lock (_gate)
        {
            all = _plans.Values.ToList();
        }

        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<T>> ListBySpecInternalAsync<T>(ISpecification<Plan, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Plan> all;
        lock (_gate)
        {
            all = _plans.Values.ToList();
        }

        return Task.FromResult((IReadOnlyList<T>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Plan>> ListBySpecInternalAsync(ISpecification<Plan> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Plan> all;
        lock (_gate)
        {
            all = _plans.Values.ToList();
        }

        return Task.FromResult((IReadOnlyList<Plan>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }
}

