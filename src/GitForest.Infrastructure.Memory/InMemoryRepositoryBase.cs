using Ardalis.Specification;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

internal sealed class InMemoryRepositoryBase<TEntity>
    where TEntity : class
{
    private readonly object _gate = new();
    private readonly Dictionary<string, TEntity> _items;
    private readonly Func<TEntity, string?> _keySelector;
    private readonly Action<TEntity> _validateEntity;
    private readonly string _duplicateEntityLabel;

    public InMemoryRepositoryBase(
        IEnumerable<TEntity?>? seed,
        IEqualityComparer<string> keyComparer,
        Func<TEntity, string?> keySelector,
        Action<TEntity> validateEntity,
        string duplicateEntityLabel)
    {
        if (keyComparer is null) throw new ArgumentNullException(nameof(keyComparer));
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        _validateEntity = validateEntity ?? throw new ArgumentNullException(nameof(validateEntity));
        _duplicateEntityLabel = duplicateEntityLabel ?? throw new ArgumentNullException(nameof(duplicateEntityLabel));

        _items = new Dictionary<string, TEntity>(keyComparer);

        if (seed is not null)
        {
            foreach (var e in seed)
            {
                if (e is null) continue;
                var key = (keySelector(e) ?? string.Empty).Trim();
                if (key.Length == 0) continue;
                _items[key] = e;
            }
        }
    }

    public Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<TEntity?>(null);

        lock (_gate)
        {
            _items.TryGetValue(id.Trim(), out var found);
            return Task.FromResult(found);
        }
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _validateEntity(entity);

        var key = GetTrimmedKey(entity);
        lock (_gate)
        {
            if (_items.ContainsKey(key))
            {
                throw new InvalidOperationException($"{_duplicateEntityLabel} '{key}' already exists.");
            }

            _items[key] = entity;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _validateEntity(entity);

        var key = GetTrimmedKey(entity);
        lock (_gate)
        {
            _items[key] = entity;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        var keyRaw = _keySelector(entity);
        if (string.IsNullOrWhiteSpace(keyRaw)) return Task.CompletedTask;

        lock (_gate)
        {
            _items.Remove(keyRaw.Trim());
        }

        return Task.CompletedTask;
    }

    public Task<TEntity?> GetBySpecAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    private string GetTrimmedKey(TEntity entity)
    {
        var key = _keySelector(entity);
        // _validateEntity is expected to ensure this is non-null/non-whitespace for write operations.
        return (key ?? string.Empty).Trim();
    }

    private List<TEntity> Snapshot()
    {
        lock (_gate)
        {
            return _items.Values.ToList();
        }
    }

    private Task<TResult?> GetBySpecInternalAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = Snapshot();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<TEntity?> GetBySpecInternalAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = Snapshot();
        return Task.FromResult(SpecificationEvaluator.Apply(all, specification).FirstOrDefault());
    }

    private Task<IReadOnlyList<TResult>> ListBySpecInternalAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = Snapshot();
        return Task.FromResult((IReadOnlyList<TResult>)SpecificationEvaluator.Apply(all, specification).ToList());
    }

    private Task<IReadOnlyList<TEntity>> ListBySpecInternalAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var all = Snapshot();
        return Task.FromResult((IReadOnlyList<TEntity>)SpecificationEvaluator.Apply(all, specification).ToList());
    }
}

