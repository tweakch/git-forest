using Orleans.Runtime;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Index grain implementation for tracking Planner IDs
/// </summary>
public class PlannerIndexGrain : Grain, IPlannerIndexGrain
{
    private readonly IPersistentState<HashSet<string>> _state;

    public PlannerIndexGrain([PersistentState("plannerIndex")] IPersistentState<HashSet<string>> state)
    {
        _state = state;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _state.State ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return base.OnActivateAsync(cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetAllIdsAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>(_state.State.ToList());
    }

    public async Task AddIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        
        _state.State.Add(id.Trim());
        await _state.WriteStateAsync();
    }

    public async Task RemoveIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        
        _state.State.Remove(id.Trim());
        await _state.WriteStateAsync();
    }
}
