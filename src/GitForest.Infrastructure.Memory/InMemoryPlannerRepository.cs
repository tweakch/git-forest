using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlannerRepository : IPlannerRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Planner> _planners;

    public InMemoryPlannerRepository(IEnumerable<Planner>? seed = null, IEqualityComparer<string>? idComparer = null)
    {
        _planners = new Dictionary<string, Planner>(idComparer ?? StringComparer.OrdinalIgnoreCase);

        if (seed is not null)
        {
            foreach (var p in seed)
            {
                if (p is null) continue;
                var id = (p.Id ?? string.Empty).Trim();
                if (id.Length == 0) continue;
                _planners[id] = p;
            }
        }
    }

    public Task<Planner?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Planner?>(null);

        lock (_gate)
        {
            _planners.TryGetValue(id.Trim(), out var found);
            return Task.FromResult(found);
        }
    }

    public Task<Planner?> GetBySpecAsync(ISpecification<Planner> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Planner>> ListAsync(ISpecification<Planner> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task AddAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planner.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        lock (_gate)
        {
            if (_planners.ContainsKey(id))
            {
                throw new InvalidOperationException($"Planner '{id}' already exists.");
            }

            _planners[id] = entity;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planner.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        lock (_gate)
        {
            _planners[id] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        lock (_gate)
        {
            _planners.Remove(entity.Id.Trim());
        }

        return Task.CompletedTask;
    }

    private Task<T?> GetBySpecInternalAsync<T>(ISpecification<Planner, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Planner> all;
        lock (_gate)
        {
            all = _planners.Values.ToList();
        }

        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Planner?> GetBySpecInternalAsync(ISpecification<Planner> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Planner> all;
        lock (_gate)
        {
            all = _planners.Values.ToList();
        }

        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<T>> ListBySpecInternalAsync<T>(ISpecification<Planner, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Planner> all;
        lock (_gate)
        {
            all = _planners.Values.ToList();
        }

        return Task.FromResult((IReadOnlyList<T>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Planner>> ListBySpecInternalAsync(ISpecification<Planner> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Planner> all;
        lock (_gate)
        {
            all = _planners.Values.ToList();
        }

        return Task.FromResult((IReadOnlyList<Planner>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }
}

