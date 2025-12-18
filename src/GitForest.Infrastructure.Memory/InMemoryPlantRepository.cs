using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlantRepository : AbstractPlantRepository
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

    public override Task<Plant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _repo.GetByIdAsync(id, cancellationToken);

    public override Task AddAsync(Plant entity, CancellationToken cancellationToken = default)
        => _repo.AddAsync(entity, cancellationToken);

    public override Task UpdateAsync(Plant entity, CancellationToken cancellationToken = default)
        => _repo.UpdateAsync(entity, cancellationToken);

    public override Task DeleteAsync(Plant entity, CancellationToken cancellationToken = default)
        => _repo.DeleteAsync(entity, cancellationToken);

    protected override List<Plant> LoadAll()
    {
        return _repo.Snapshot();
    }
}

