using GitForest.Core;
using Orleans.Runtime;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain implementation for Planter entity
/// </summary>
public class PlanterGrain : Grain, IPlanterGrain
{
    private readonly IPersistentState<Planter?> _state;

    public PlanterGrain([PersistentState("planter")] IPersistentState<Planter?> state)
    {
        _state = state;
    }

    public Task<Planter?> GetAsync()
    {
        return Task.FromResult(_state.State);
    }

    public async Task SetAsync(Planter planter)
    {
        _state.State = planter;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }
}
