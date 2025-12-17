using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlantRepository : AbstractPlantRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Plant> _plants;

    public InMemoryPlantRepository(IEnumerable<Plant>? seed = null, IEqualityComparer<string>? keyComparer = null)
    {
        _plants = new Dictionary<string, Plant>(keyComparer ?? StringComparer.OrdinalIgnoreCase);

        if (seed is not null)
        {
            foreach (var p in seed)
            {
                if (p is null) continue;
                var key = (p.Key ?? string.Empty).Trim();
                if (key.Length == 0) continue;
                _plants[key] = p;
            }
        }
    }

    public override Task<Plant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Plant?>(null);

        lock (_gate)
        {
            _plants.TryGetValue(id.Trim(), out var found);
            return Task.FromResult(found);
        }
    }

    public override Task AddAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var key = GetTrimmedId(entity);
        lock (_gate)
        {
            if (_plants.ContainsKey(key))
            {
                throw new InvalidOperationException($"Plant '{key}' already exists.");
            }

            _plants[key] = entity;
        }

        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var key = GetTrimmedId(entity);
        lock (_gate)
        {
            _plants[key] = entity;
        }

        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Plant entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Key)) return Task.CompletedTask;

        lock (_gate)
        {
            _plants.Remove(entity.Key.Trim());
        }

        return Task.CompletedTask;
    }

    protected override List<Plant> LoadAll()
    {
        lock (_gate)
        {
            return _plants.Values.ToList();
        }
    }
}

