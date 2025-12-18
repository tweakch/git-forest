using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlanterRepository : AbstractPlanterRepository
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

    public override Task<Planter?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _repo.GetByIdAsync(id, cancellationToken);

    public override Task AddAsync(Planter entity, CancellationToken cancellationToken = default)
        => _repo.AddAsync(entity, cancellationToken);

    public override Task UpdateAsync(Planter entity, CancellationToken cancellationToken = default)
        => _repo.UpdateAsync(entity, cancellationToken);

    public override Task DeleteAsync(Planter entity, CancellationToken cancellationToken = default)
        => _repo.DeleteAsync(entity, cancellationToken);

    protected override List<Planter> LoadAll()
    {
        return _repo.Snapshot();
    }
}

