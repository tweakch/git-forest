using Ardalis.Specification;
using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlanRepository : IPlanRepository
{
    private readonly InMemoryRepositoryBase<Plan> _repo;

    public InMemoryPlanRepository(IEnumerable<Plan>? seed = null, IEqualityComparer<string>? idComparer = null)
    {
        _repo = new InMemoryRepositoryBase<Plan>(
            seed,
            idComparer ?? StringComparer.OrdinalIgnoreCase,
            static p => p.Id,
            ValidateEntity,
            "Plan");
    }

    public Task<Plan?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _repo.GetByIdAsync(id, cancellationToken);

    public Task AddAsync(Plan entity, CancellationToken cancellationToken = default)
        => _repo.AddAsync(entity, cancellationToken);

    public Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default)
        => _repo.UpdateAsync(entity, cancellationToken);

    public Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default)
        => _repo.DeleteAsync(entity, cancellationToken);

    public Task<Plan?> GetBySpecAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
        => _repo.GetBySpecAsync(specification, cancellationToken);

    public Task<TResult?> GetBySpecAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
        => _repo.GetBySpecAsync(specification, cancellationToken);

    public Task<IReadOnlyList<Plan>> ListAsync(ISpecification<Plan> specification, CancellationToken cancellationToken = default)
        => _repo.ListAsync(specification, cancellationToken);

    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<Plan, TResult> specification, CancellationToken cancellationToken = default)
        => _repo.ListAsync(specification, cancellationToken);

    private static void ValidateEntity(Plan entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) throw new ArgumentException("Plan.Id must be provided.", nameof(entity));
    }
}

