using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlantRepository : IPlantRepository
{
    private readonly InMemoryRepositoryBase<Plant> _repo;

    public InMemoryPlantRepository(IEnumerable<Plant>? seed = null, IEqualityComparer<string>? keyComparer = null)
    {
        _repo = new InMemoryRepositoryBase<Plant>(
            seed,
            keyComparer ?? StringComparer.OrdinalIgnoreCase,
            static p => p.Key,
            ValidateEntity,
            "Plant");
    }

    public Task<Plant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _repo.GetByIdAsync(id, cancellationToken);

    public Task AddAsync(Plant entity, CancellationToken cancellationToken = default)
        => _repo.AddAsync(entity, cancellationToken);

    public Task UpdateAsync(Plant entity, CancellationToken cancellationToken = default)
        => _repo.UpdateAsync(entity, cancellationToken);

    public Task DeleteAsync(Plant entity, CancellationToken cancellationToken = default)
        => _repo.DeleteAsync(entity, cancellationToken);

    public Task<Plant?> GetBySpecAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
        => _repo.GetBySpecAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
        => _repo.GetBySpecAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Plant>> ListAsync(ISpecification<Plant> specification, CancellationToken cancellationToken = default)
        => _repo.ListAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plant, TResult> specification, CancellationToken cancellationToken = default)
        => _repo.ListAsync(specification, cancellationToken);

    private static void ValidateEntity(Plant entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) throw new ArgumentException("Plant.Key must be provided.", nameof(entity));
    }
}

