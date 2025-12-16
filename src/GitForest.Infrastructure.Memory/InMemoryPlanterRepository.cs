using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlanterRepository : IPlanterRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Planter> _planters;

    public InMemoryPlanterRepository(IEnumerable<Planter>? seed = null, IEqualityComparer<string>? idComparer = null)
    {
        _planters = new Dictionary<string, Planter>(idComparer ?? StringComparer.OrdinalIgnoreCase);

        if (seed is not null)
        {
            foreach (var p in seed)
            {
                if (p is null) continue;
                var id = (p.Id ?? string.Empty).Trim();
                if (id.Length == 0) continue;
                _planters[id] = p;
            }
        }
    }

    public Task<Planter?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Planter?>(null);

        lock (_gate)
        {
            _planters.TryGetValue(id.Trim(), out var found);
            return Task.FromResult(found);
        }
    }

    public Task<Planter?> GetBySpecAsync(ISpecification<Planter> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Planter>> ListAsync(ISpecification<Planter> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task AddAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planter.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        lock (_gate)
        {
            if (_planters.ContainsKey(id))
            {
                throw new InvalidOperationException($"Planter '{id}' already exists.");
            }

            _planters[id] = entity;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planter.Id must be provided.", nameof(entity));

        var id = entity.Id.Trim();
        lock (_gate)
        {
            _planters[id] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        lock (_gate)
        {
            _planters.Remove(entity.Id.Trim());
        }

        return Task.CompletedTask;
    }

    private Task<T?> GetBySpecInternalAsync<T>(ISpecification<Planter, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Planter> all;
        lock (_gate)
        {
            all = _planters.Values.ToList();
        }

        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Planter?> GetBySpecInternalAsync(ISpecification<Planter> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Planter> all;
        lock (_gate)
        {
            all = _planters.Values.ToList();
        }

        return Task.FromResult(InMemorySpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<T>> ListBySpecInternalAsync<T>(ISpecification<Planter, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Planter> all;
        lock (_gate)
        {
            all = _planters.Values.ToList();
        }

        return Task.FromResult((IReadOnlyList<T>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Planter>> ListBySpecInternalAsync(ISpecification<Planter> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Planter> all;
        lock (_gate)
        {
            all = _planters.Values.ToList();
        }

        return Task.FromResult((IReadOnlyList<Planter>)InMemorySpecificationEvaluator.Apply(all, specification).ToList());
    }
}

