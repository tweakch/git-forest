using GitForest.Core;
using Orleans.Runtime;

namespace GitForest.Infrastructure.Distributed.Grains;

/// <summary>
/// Orleans grain implementation for Plan entity
/// </summary>
public class PlanGrain : Grain, IPlanGrain
{
    private readonly IPersistentState<Plan?> _state;

    public PlanGrain([PersistentState("plan")] IPersistentState<Plan?> state)
    {
        _state = state;
    }

    public Task<Plan?> GetAsync()
    {
        return Task.FromResult(_state.State);
    }

    public async Task SetAsync(Plan plan)
    {
        _state.State = plan;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }
}
