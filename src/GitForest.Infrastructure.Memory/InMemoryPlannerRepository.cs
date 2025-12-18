using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlannerRepository : IPlannerRepository
{
    private readonly InMemoryRepositoryBase<Planner> _repo;

    public InMemoryPlannerRepository(IEnumerable<Planner>? seed = null, IEqualityComparer<string>? idComparer = null)
    {
        _repo = new InMemoryRepositoryBase<Planner>(
            seed,
            idComparer ?? StringComparer.OrdinalIgnoreCase,
            static p => p.Id,
            ValidateEntity,
            "Planner");
    }

    public Task<Planner?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _repo.GetByIdAsync(id, cancellationToken);

    public Task AddAsync(Planner entity, CancellationToken cancellationToken = default)
        => _repo.AddAsync(entity, cancellationToken);

    public Task UpdateAsync(Planner entity, CancellationToken cancellationToken = default)
        => _repo.UpdateAsync(entity, cancellationToken);

    public Task DeleteAsync(Planner entity, CancellationToken cancellationToken = default)
        => _repo.DeleteAsync(entity, cancellationToken);

    public Task<Planner?> GetBySpecAsync(ISpecification<Planner> specification, CancellationToken cancellationToken = default)
        => _repo.GetBySpecAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken = default)
        => _repo.GetBySpecAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Planner>> ListAsync(ISpecification<Planner> specification, CancellationToken cancellationToken = default)
        => _repo.ListAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Planner, TResult> specification, CancellationToken cancellationToken = default)
        => _repo.ListAsync(specification, cancellationToken);

    private static void ValidateEntity(Planner entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Planner.Id must be provided.", nameof(entity));
    }
}

