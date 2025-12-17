using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlantRepository : IPlantRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Plant> _plants;

    public InMemoryPlantRepository(IEnumerable<Plant>? seed = null, IEqualityComparer<string>? keyComparer = null)
    {
        _plants = new Dictionary<string, Plant>(keyComparer ?? StringComparer.OrdinalIgnoreCase);

        if (seed is not null)
        {
            foreach (var p in seed)
            {
                if (p is null) continue;
                var key = (p.Key ?? string.Empty).Trim();
                if (key.Length == 0) continue;
                _plants[key] = p;
            }
        }
    }

    public Task<Plant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Plant?>(null);

        lock (_gate)
        {
            _plants.TryGetValue(id.Trim(), out var found);
            return Task.FromResult(found);
        }
    }

    public Task<Plant?> GetBySpecAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Plant>> ListAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task AddAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) throw new ArgumentException("Plant.Key must be provided.", nameof(entity));

        var key = entity.Key.Trim();
        lock (_gate)
        {
            if (_plants.ContainsKey(key))
            {
                throw new InvalidOperationException($"Plant '{key}' already exists.");
            }

            _plants[key] = entity;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) throw new ArgumentException("Plant.Key must be provided.", nameof(entity));

        var key = entity.Key.Trim();
        lock (_gate)
        {
            _plants[key] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) return Task.CompletedTask;

        lock (_gate)
        {
            _plants.Remove(entity.Key.Trim());
        }

        return Task.CompletedTask;
    }

    private Task<T?> GetBySpecInternalAsync<T>(ISpecification<Plant, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Plant> all;
        lock (_gate)
        {
            all = _plants.Values.ToList();
        }

        return Task.FromResult(GitForest.Core.Persistence.SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<Plant?> GetBySpecInternalAsync(ISpecification<Plant> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Plant> all;
        lock (_gate)
        {
            all = _plants.Values.ToList();
        }

        return Task.FromResult(GitForest.Core.Persistence.SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<T>> ListBySpecInternalAsync<T>(ISpecification<Plant, T> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Plant> all;
        lock (_gate)
        {
            all = _plants.Values.ToList();
        }

        return Task.FromResult((IReadOnlyList<T>)GitForest.Core.Persistence.SpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<Plant>> ListBySpecInternalAsync(ISpecification<Plant> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        List<Plant> all;
        lock (_gate)
        {
            all = _plants.Values.ToList();
        }

        return Task.FromResult((IReadOnlyList<Plant>)GitForest.Core.Persistence.SpecificationEvaluator.Apply(all, specification).ToList());
    }
}

