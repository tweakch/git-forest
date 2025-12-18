using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlannerRepository : AbstractPlannerRepository
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

    public override Task<Planner?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _repo.GetByIdAsync(id, cancellationToken);

    public override Task AddAsync(Planner entity, CancellationToken cancellationToken = default)
        => _repo.AddAsync(entity, cancellationToken);

    public override Task UpdateAsync(Planner entity, CancellationToken cancellationToken = default)
        => _repo.UpdateAsync(entity, cancellationToken);

    public override Task DeleteAsync(Planner entity, CancellationToken cancellationToken = default)
        => _repo.DeleteAsync(entity, cancellationToken);

    protected override List<Planner> LoadAll()
    {
        return _repo.Snapshot();
    }
}

