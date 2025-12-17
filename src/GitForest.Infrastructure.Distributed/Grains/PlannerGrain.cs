using GitForest.Core;
using Orleans.Runtime;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain implementation for Planner entity
/// </summary>
public class PlannerGrain : Grain, IPlannerGrain
{
    private readonly IPersistentState<Planner?> _state;

    public PlannerGrain([PersistentState("planner")] IPersistentState<Planner?> state)
    {
        _state = state;
    }

    public Task<Planner?> GetAsync()
    {
        return Task.FromResult(_state.State);
    }

    public async Task SetAsync(Planner planner)
    {
        _state.State = planner;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }
}
