using Ardalis.Specification;

namespace GitForest.Core.Persistence;

/// <summary>
/// Generic repository base that implements Ardalis specification queries by materializing
/// a collection via <see cref="LoadAllAsync"/> and evaluating it with <see cref="SpecificationEvaluator"/>.
/// </summary>
public abstract class AbstractRepositoryWithSpecs<T, TId> : IRepository<T, TId>
    where T : class
{
    public abstract Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    public abstract Task AddAsync(T entity, CancellationToken cancellationToken = default);
    public abstract Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    public abstract Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    public Task<T?> GetBySpecAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        => GetBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        => ListBySpecInternalAsync(specification, cancellationToken);

    /// <summary>
    /// Materializes all entities for in-memory specification evaluation.
    /// Implementations should return a stable snapshot.
    /// </summary>
    protected abstract Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default);

    private async Task<TResult?> GetBySpecInternalAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken)
    {
        if (specification is null) throw new ArgumentNullException(nameof(specification));
        var all = await LoadAllAsync(cancellationToken);
        return SpecificationEvaluator.Apply(all, specification).FirstOrDefault();
    }

    private async Task<T?> GetBySpecInternalAsync(ISpecification<T> specification, CancellationToken cancellationToken)
    {
        if (specification is null) throw new ArgumentNullException(nameof(specification));
        var all = await LoadAllAsync(cancellationToken);
        return SpecificationEvaluator.Apply(all, specification).FirstOrDefault();
    }

    private async Task<IReadOnlyList<TResult>> ListBySpecInternalAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken)
    {
        if (specification is null) throw new ArgumentNullException(nameof(specification));
        var all = await LoadAllAsync(cancellationToken);
        return (IReadOnlyList<TResult>)SpecificationEvaluator.Apply(all, specification).ToList();
    }

    private async Task<IReadOnlyList<T>> ListBySpecInternalAsync(ISpecification<T> specification, CancellationToken cancellationToken)
    {
        if (specification is null) throw new ArgumentNullException(nameof(specification));
        var all = await LoadAllAsync(cancellationToken);
        return (IReadOnlyList<T>)SpecificationEvaluator.Apply(all, specification).ToList();
    }
}

