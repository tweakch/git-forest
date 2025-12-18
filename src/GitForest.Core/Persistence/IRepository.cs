namespace GitForest.Core.Persistence;

/// <summary>
/// Read/write repository port for an aggregate root.
/// </summary>
public interface IRepository<T, in TId> : IReadRepository<T, TId>
    where T : class
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}

