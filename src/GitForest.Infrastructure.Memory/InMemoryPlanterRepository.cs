using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlanterRepository : IPlanterRepository
{
    private readonly InMemoryRepositoryBase<Planter> _repo;

    public InMemoryPlanterRepository(IEnumerable<Planter>? seed = null, IEqualityComparer<string>? idComparer = null)
    {
        _repo = new InMemoryRepositoryBase<Planter>(
            seed,
            idComparer ?? StringComparer.OrdinalIgnoreCase,
            static p => p.Id,
            ValidateEntity,
            "Planter");
    }

    public Task<Planter?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _repo.GetByIdAsync(id, cancellationToken);

    public Task AddAsync(Planter entity, CancellationToken cancellationToken = default)
        => _repo.AddAsync(entity, cancellationToken);

    public Task UpdateAsync(Planter entity, CancellationToken cancellationToken = default)
        => _repo.UpdateAsync(entity, cancellationToken);

    public Task DeleteAsync(Planter entity, CancellationToken cancellationToken = default)
        => _repo.DeleteAsync(entity, cancellationToken);

    public Task<Planter?> GetBySpecAsync(ISpecification<Planter> specification, CancellationToken cancellationToken = default)
        => _repo.GetBySpecAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken = default)
        => _repo.GetBySpecAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Planter>> ListAsync(ISpecification<Planter> specification, CancellationToken cancellationToken = default)
        => _repo.ListAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Planter, TResult> specification, CancellationToken cancellationToken = default)
        => _repo.ListAsync(specification, cancellationToken);

    private static void ValidateEntity(Planter entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planter.Id must be provided.", nameof(entity));
    }
}

