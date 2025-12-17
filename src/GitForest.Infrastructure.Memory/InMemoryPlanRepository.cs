using GitForest.Core;
using GitForest.Core.Persistence;

namespace GitForest.Infrastructure.Memory;

public sealed class InMemoryPlanRepository : AbstractPlanRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Plan> _plans;

    public InMemoryPlanRepository(IEnumerable<Plan>? seed = null, IEqualityComparer<string>? idComparer = null)
    {
        _plans = new Dictionary<string, Plan>(idComparer ?? StringComparer.OrdinalIgnoreCase);

        if (seed is not null)
        {
            foreach (var p in seed)
            {
                if (p is null) continue;
                var id = (p.Id ?? string.Empty).Trim();
                if (id.Length == 0) continue;
                _plans[id] = p;
            }
        }
    }

    public override Task<Plan?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Plan?>(null);

        lock (_gate)
        {
            _plans.TryGetValue(id.Trim(), out var found);
            return Task.FromResult(found);
        }
    }

    public override Task AddAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var id = GetTrimmedId(entity);
        lock (_gate)
        {
            if (_plans.ContainsKey(id))
            {
                throw new InvalidOperationException($"Plan '{id}' already exists.");
            }

            _plans[id] = entity;
        }

        return Task.CompletedTask;
    }

    public override Task UpdateAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        ValidateEntity(entity);

        var id = GetTrimmedId(entity);
        lock (_gate)
        {
            _plans[id] = entity;
        }

        return Task.CompletedTask;
    }

    public override Task DeleteAsync(Plan entity, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        if (string.IsNullOrWhiteSpace(entity.Id)) return Task.CompletedTask;

        lock (_gate)
        {
            _plans.Remove(entity.Id.Trim());
        }

        return Task.CompletedTask;
    }

    protected override List<Plan> LoadAllPlans()
    {
        lock (_gate)
        {
            return _plans.Values.ToList();
        }
    }
}

