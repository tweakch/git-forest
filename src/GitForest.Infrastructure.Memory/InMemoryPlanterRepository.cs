using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlanterRepository : AbstractPlanterRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Planter> _planters;

    public InMemoryPlanterRepository(IEnumerable<Planter>? seed = null, IEqualityComparer<string>? idComparer = null)
    {
        _planters = new Dictionary<string, Planter>(idComparer ?? StringComparer.OrdinalIgnoreCase);

        if (seed is not null)
        {
            foreach (var p in seed)
            {
                if (p is null) continue;
                var id = (p.Id ?? string.Empty).Trim();
                if (id.Length == 0) continue;
                _planters[id] = p;
            }
        }
    }

    public override Task<Planter?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Planter?>(null);

        lock (_gate)
        {
            _planters.TryGetValue(id.Trim(), out var found);
            return Task.FromResult(found);
        }
    }

    public override Task AddAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var id = GetTrimmedId(entity);
        lock (_gate)
        {
            if (_planters.ContainsKey(id))
            {
                throw new InvalidOperationException($"Planter '{id}' already exists.");
            }

            _planters[id] = entity;
        }

        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var id = GetTrimmedId(entity);
        lock (_gate)
        {
            _planters[id] = entity;
        }

        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Planter entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        lock (_gate)
        {
            _planters.Remove(entity.Id.Trim());
        }

        return Task.CompletedTask;
    }

    protected override List<Planter> LoadAll()
    {
        lock (_gate)
        {
            return _planters.Values.ToList();
        }
    }
}

