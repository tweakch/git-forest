using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlannerRepository : AbstractPlannerRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Planner> _planners;

    public InMemoryPlannerRepository(IEnumerable<Planner>? seed = null, IEqualityComparer<string>? idComparer = null)
    {
        _planners = new Dictionary<string, Planner>(idComparer ?? StringComparer.OrdinalIgnoreCase);

        if (seed is not null)
        {
            foreach (var p in seed)
            {
                if (p is null) continue;
                var id = (p.Id ?? string.Empty).Trim();
                if (id.Length == 0) continue;
                _planners[id] = p;
            }
        }
    }

    public override Task<Planner?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Planner?>(null);

        lock (_gate)
        {
            _planners.TryGetValue(id.Trim(), out var found);
            return Task.FromResult(found);
        }
    }

    public override Task AddAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var id = GetTrimmedId(entity);
        lock (_gate)
        {
            if (_planners.ContainsKey(id))
            {
                throw new InvalidOperationException($"Planner '{id}' already exists.");
            }

            _planners[id] = entity;
        }

        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var id = GetTrimmedId(entity);
        lock (_gate)
        {
            _planners[id] = entity;
        }

        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Planner entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        lock (_gate)
        {
            _planners.Remove(entity.Id.Trim());
        }

        return Task.CompletedTask;
    }

    protected override List<Planner> LoadAll()
    {
        lock (_gate)
        {
            return _planners.Values.ToList();
        }
    }
}

