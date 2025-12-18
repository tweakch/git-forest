using Ardalis.Specification;

namespace GitForest.Core.Persistence;

/// <summary>
/// Read-only repository port for an aggregate root, with query intent expressed via Ardalis specifications.
/// </summary>
public interface IReadRepository<T, in TId>
    where T : class
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<T?> GetBySpecAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    Task<TResult?> GetBySpecAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default);
}


