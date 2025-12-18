using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlanRepository : AbstractPlanRepository
{
    private readonly InMemoryRepositoryBase<Plan> _repo;

    public InMemoryPlanRepository(
        IEnumerable<Plan>? seed = null,
        IEqualityComparer<string>? idComparer = null
    )
    {
        _repo = new InMemoryRepositoryBase<Plan>(
            seed,
            idComparer ?? StringComparer.OrdinalIgnoreCase,
            static p => p.Id,
            ValidateEntity,
            "Plan"
        );
    }

    public override Task<Plan?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default
    ) => _repo.GetByIdAsync(id, cancellationToken);

    public override Task AddAsync(Plan entity, CancellationToken cancellationToken = default) =>
        _repo.AddAsync(entity, cancellationToken);

    public override Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default) =>
        _repo.UpdateAsync(entity, cancellationToken);

    public override Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default) =>
        _repo.DeleteAsync(entity, cancellationToken);

    protected override List<Plan> LoadAllPlans()
    {
        return _repo.Snapshot();
    }
}
